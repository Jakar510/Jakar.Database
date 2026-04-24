# Jakar.Database

`Jakar.Database` is a .NET ORM and schema-management library built around three ideas:

1. Stream records with `IAsyncEnumerable<T>` instead of materializing everything eagerly.
2. Keep the common table model explicit so PostgreSQL and Microsoft SQL Server can share most of the shape.
3. Push repetitive record boilerplate into code generation instead of hand-maintaining `Create(DbDataReader)` and `ToDynamicParameters()` everywhere.

## Current Focus

- High-throughput table access over `DbDataReader` and batched commands.
- Shared record metadata for common tables.
- Reversible migrations, especially for tables, enums, and generated indexes.
- Incremental source generation for `ITableRecord<TSelf>` plumbing.
- Hybrid host authentication for WebAPI, Blazor, and other .NET clients.

## Repository Layout

- `Jakar.Database/`
  The main library, common records, migration metadata, database abstractions, and package README.
- `Jakar.Database.Generators/`
  Incremental source generator that emits `Create(DbDataReader)` and `ToDynamicParameters()` for opt-in partial table records.
- `Jakar.Database.Tests/`
  Integration tests plus lightweight unit checks for generated record behavior.
- `SampleApi/`
  Minimal sample host.
- `Experiments/`
  Benchmarks and exploratory performance work.
- `Jakar.SqlBuilder/`
  SQL builder utilities used alongside the ORM layer.

## Design Notes

### Record Model

The library uses a small inheritance stack:

- `TableRecord<TSelf>`
  Base JSON model and shared metadata access.
- `LastModifiedRecord<TSelf>`
  Adds `LastModified`.
- `PairRecord<TSelf>`
  Adds `ID` and shared parameter helpers.
- `OwnedTableRecord<TSelf>`
  Adds `UserID`.
- `Mapping<TSelf, TKey, TValue>`
  Link-table pattern for many-to-many relationships.

The generated metadata still comes from reflection over the public record shape. That keeps the table model obvious in code, while the generator removes only the mechanical translation code.

### Source Generator

The incremental generator is now active through `Jakar.Database.Generators`.

If a table record:

- is `partial`
- implements `ITableRecord<TSelf>`
- has a `DbDataReader` constructor
- does not already declare `Create(DbDataReader)`
- does not already override `ToDynamicParameters()`

the generator emits those members automatically.

That keeps the manual code focused on actual behavior:

- validation
- custom factory overloads
- custom binary import/export when the generated path is not enough
- comparison and equality logic
- table-specific domain helpers

The generator now also fills in the common abstract `TableRecord<TSelf>` members when they are missing:

- `Export(NpgsqlBinaryExporter, CancellationToken)`
- `Import(NpgsqlBatchCommand, CancellationToken)`
- `Import(NpgsqlBinaryImporter, string, NpgsqlDbType, CancellationToken)`

Records still keep manual implementations when they need non-standard behavior.

### Authentication

`DbOptions.AddAuthentication(...)` now registers a hybrid default scheme intended for mixed hosts:

- bearer tokens are used automatically for requests with an `Authorization: Bearer ...` header
- bearer tokens are also preferred for configured API path prefixes such as `/api`
- cookies are used for interactive requests such as Blazor or MVC-style navigation

Relevant `DbOptions` members:

- `AuthenticationScheme`
  The default hybrid scheme used by authorization.
- `BearerAuthenticationScheme`
  The named JWT bearer scheme.
- `CookieAuthenticationScheme`
  The primary cookie scheme for browser-based sign-in.
- `BearerPathPrefixes`
  Paths that should challenge/authenticate as APIs even without a bearer header.
- `ForwardDefaultSelector`
  Optional override for custom scheme selection.

Minimal host order:

```csharp
builder.Services.AddAuthorization();
options.AddAuthentication(builder);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

Focused validation now covers:

- bearer-authenticated API requests
- cookie-authenticated interactive requests
- unauthenticated API requests returning `401`

### Identity Services

`DbServices.AddIdentityServices(...)` is the ASP.NET Identity registration entry point for the library's custom stores and managers.

It now validates cleanly for the common `Identity` surface instead of relying on hidden follow-up registrations:

- `IUserStore<UserRecord>` and the related login, claim, password, email, lockout, 2FA, authenticator-key, recovery-code, and phone-number store interfaces
- `IRoleStore<RoleRecord>`
- `UserManager<UserRecord>`, `RoleManager<RoleRecord>`, and `SignInManager<UserRecord>`
- default ASP.NET Identity token flows for password reset, email confirmation, and authenticator tokens

The default `IdentityOptions` token-provider mapping now aligns with the providers that `AddIdentityServices(...)` actually registers:

- password reset uses `TokenOptions.DefaultProvider`
- email confirmation and change-email use `TokenOptions.DefaultEmailProvider`
- change-phone-number uses `TokenOptions.DefaultPhoneProvider`
- authenticator tokens use `TokenOptions.DefaultAuthenticatorProvider`

The generic overload also keeps a named custom provider registration for both:

- `nameof(TTokenProvider)`
- `options.AppInformation.AppName`

### Migrations

`MigrationRecord` now treats rollback as a first-class concern:

- `SetLastModified` defines `DownSQL`
- enum migrations define `DownSQL`
- generated index migrations define `DownSQL`
- `MigrationManager.RevertMigrations(...)` actually executes rollback SQL in reverse order and removes applied migration rows

If a migration cannot be rolled back safely, it should not be registered without an explicit decision. Silent “best effort” down migrations are how schema drift starts.

## Current Provider Status

The target remains PostgreSQL plus Microsoft SQL Server, but the implementation is not fully symmetric yet.

What is in better shape now:

- shared table metadata
- generated record parameterization
- reversible migration definitions
- provider-aware index create/drop SQL

What still needs more work:

- some query helpers still emit PostgreSQL-first SQL such as `LIMIT`, `RETURNING`, or `RANDOM()`
- enum handling is PostgreSQL-native today
- broader SQL Server query/dialect coverage still needs a deliberate pass

That means the table model is trending toward cross-provider, but parts of the query surface are still PostgreSQL-biased.

## Building

```powershell
dotnet build Jakar.Database.slnx
```

## Testing

Unit-style generator checks:

```powershell
dotnet test Jakar.Database.Tests\Jakar.Database.Tests.csproj --filter GeneratedRecordTests --no-restore
```

Identity service checks:

```powershell
dotnet test Jakar.Database.Tests\Jakar.Database.Tests.csproj --filter IdentityServicesRegistrationTests --no-restore
```

Integration tests require Docker because the existing fixture uses Testcontainers PostgreSQL:

```powershell
dotnet test Jakar.Database.Tests\Jakar.Database.Tests.csproj
```

## Immediate Improvement Areas

If you want to keep pushing in the current direction, these are the next high-value steps:

1. Move more provider-specific SQL generation behind `DatabaseType` instead of embedding PostgreSQL syntax in static query helpers.
2. Expand generator coverage to more import/export helpers where the pattern is stable enough.
3. Separate Docker-backed integration tests from pure unit tests so local verification is cheaper.
4. Audit all public get-only properties on table records and mark non-persisted ones with `[DbIgnore]` consistently.
5. Decide how the analyzer should be shipped through NuGet so consumers get the generator automatically, not only solution builds.

## Related Docs

- [Package README](Jakar.Database/README.md)
- [Database Type Notes](Jakar.Database/Features.md)
