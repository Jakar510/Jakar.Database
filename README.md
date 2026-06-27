# Jakar.Database

`Jakar.Database` is a .NET ORM, schema-management, authentication, and host-wiring library built around explicit table records instead of Entity Framework entities.

The library focuses on:

- streaming table access with `IAsyncEnumerable<T>`
- shared record metadata for PostgreSQL and Microsoft SQL Server targets
- Dapper-friendly command parameter generation
- reversible schema migrations
- source-generated `ITableRecord<TSelf>` boilerplate
- ASP.NET Core authentication and Identity wiring without Entity Framework stores

## Prerequisites

- .NET 10 SDK
- PostgreSQL for the current integration-test fixture and sample host
- Docker when running the full integration test suite

## Repository Layout

- `Jakar.Database/`
  Main library, common records, database abstractions, migration metadata, authentication, Identity stores, and the package README.
- `Jakar.Database.Generators/`
  Incremental source generator for common `ITableRecord<TSelf>` and `TableRecord<TSelf>` members.
- `Jakar.Database.Tests/`
  Integration tests plus focused unit checks for generated records, authentication routing, migrations, and Identity service registration.
- `SampleApi/`
  Minimal ASP.NET Core host showing database registration, hybrid auth, migrations, and test endpoints.
- `Experiments/`
  Benchmarks and exploratory performance work.
- `Jakar.SqlBuilder/`
  Lean, dependency-free, dialect-aware fluent SQL builder (`ref struct` stages, validated output) for SQLite / SQL Server / PostgreSQL. Jakar.Database references it; see [SQL Builder](#sql-builder).

## Install

From a consuming project:

```powershell
dotnet add package Jakar.Database
```

The package targets `net10.0`. Source-generated record members are described below.

## Quick Start

Create a `Database` subclass for the provider you want to connect to. The current validated sample path uses PostgreSQL:

```csharp
using System.Data.Common;
using Jakar.Database;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Npgsql;

public sealed class AppDatabase(IConfiguration configuration, IOptions<DbOptions> options, IFusionCache cache)
    : Database(configuration, options, cache), IAppID
{
    public static Guid AppID { get; } = Guid.NewGuid();
    public static string AppName => nameof(AppDatabase);
    public static AppVersion AppVersion { get; } = new(1, 0, 0, 1);

    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;

    protected override DbConnection CreateConnection(in ConnectionString secure) => new NpgsqlConnection(secure);
}
```

Register it in an ASP.NET Core host:

```csharp
using Jakar.Database;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

SecuredStringResolverOptions connectionString =
    "User ID=dev;Password=dev;Host=localhost;Port=5432;Database=my_app";

DbOptions options = new()
{
    TelemetrySource = new TelemetrySource(AppVersion.Default, Guid.NewGuid(), "MyApp", typeof(Program).Assembly.FullName),
    ConnectionStringResolver = connectionString,
    CommandTimeout = 30,
    TokenIssuer = AppDatabase.AppName,
    TokenAudience = AppDatabase.AppName,
    LoggerOptions = new AppLoggerOptions(),
    ConfigureCookieAuth = cookie =>
    {
        cookie.Cookie.Name = "my_app_auth";
        cookie.LoginPath = "/auth/login";
        cookie.AccessDeniedPath = "/auth/denied";
    }
};

builder.AddDatabase<AppDatabase>(options);

await using WebApplication app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

await app.RunWithMigrationsAsync(["localhost:8181", "0.0.0.0:8181"]);
```

`AddDatabase<TDatabase>(...)` registers the database singleton, default logging, OpenTelemetry, FusionCache, ASP.NET Core Identity stores, data protection, authentication, authorization, and the multi-factor policy. It also registers the `SqlBuilderOptions` from `DbOptions.SqlBuilder` and points the PostgreSQL identifier folder at the curated `PostgresParams.SqlName` so the SQL builder reproduces existing snake_case column names; see [SQL Builder](#sql-builder).

## Table Records

Table records are explicit .NET records. The inheritance stack captures the common columns:

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

Record metadata is still derived from the public record shape. That keeps table definitions visible in code while letting the generator remove the repetitive conversion members.

Example record shape:

```csharp
using System.Data.Common;
using Jakar.Database;

[Table(TABLE_NAME)]
public sealed partial record ExampleRecord : PairRecord<ExampleRecord>, ITableRecord<ExampleRecord>
{
    public const string TABLE_NAME = "examples";

    private static readonly SqlName __tableName = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __tableName;

    public string Name { get; init; } = string.Empty;

    internal ExampleRecord(DbDataReader reader) : base(reader)
    {
        Name = reader.GetFieldValue<ExampleRecord, string>(nameof(Name));
    }
}
```

The generated metadata ignores properties marked with `[DbIgnore]`. Public get-only properties that are not persisted should be marked with `[DbIgnore]` so the table shape remains intentional.

## Source Generator

`Jakar.Database.Generators` emits common record plumbing when a record:

- is `partial`
- implements `ITableRecord<TSelf>`
- has a `DbDataReader` constructor
- does not already declare `Create(DbDataReader)`
- does not already override `ToDynamicParameters()`
- does not already implement the common import/export members

Generated members can include:

- `public static TSelf Create(DbDataReader reader)`
- `public override CommandParameters ToDynamicParameters()`
- `public override ValueTask Export(NpgsqlBinaryExporter exporter, CancellationToken token)`
- `public override ValueTask Import(NpgsqlBatchCommand batch, CancellationToken token)`
- `protected override ValueTask Import(NpgsqlBinaryImporter importer, string propertyName, NpgsqlDbType postgresDbType, CancellationToken token)`

Keep manual implementations for validation, custom factory overloads, non-standard binary import/export behavior, comparison/equality logic, and table-specific domain helpers.

## Migrations

`MigrationManager` registers built-in migrations for shared functions, enum types, common tables, and generated indexes. Hosts can apply migrations during startup with:

```csharp
await app.ApplyMigrations();
```

or use the full startup helper:

```csharp
await app.RunWithMigrationsAsync(["localhost:8181", "0.0.0.0:8181"]);
```

Development hosts can expose the applied-migrations page at `/_migrations`:

```csharp
app.TryUseMigrationsEndPoint();
```

Rollback is first-class. `MigrationRecord` carries both `UpSQL` and `DownSQL`, and `MigrationManager.RevertMigrations(...)` executes rollback SQL in reverse order before removing applied migration rows:

```csharp
await app.RevertMigrations(migrateDownToInclusive: 12);
```

Current rollback support covers:

- `SetLastModified` function setup
- enum migrations
- table creation migrations
- generated index migrations

If a migration cannot be rolled back safely, do not register it without making that decision explicit. Silent best-effort rollback logic is how schema drift starts.

## Authentication

`DbOptions.AddAuthentication(...)` registers a hybrid ASP.NET Core authentication setup for mixed hosts:

- bearer tokens for requests with an `Authorization: Bearer ...` header
- bearer tokens for configured API path prefixes such as `/api`
- cookies for interactive browser requests such as Blazor, MVC, or Razor navigation

Relevant `DbOptions` members:

- `AuthenticationScheme`
- `BearerAuthenticationScheme`
- `CookieAuthenticationScheme`
- `BearerPathPrefixes`
- `ForwardDefaultSelector`
- `ConfigureCookieAuth`
- `ConfigureApplicationCookie`
- `ConfigureExternalCookie`
- `ConfigureMicrosoftAccount`
- `ConfigureGoogle`
- `ConfigureOpenIdConnect`

Minimal middleware order:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Focused tests validate bearer-authenticated API requests, cookie-authenticated interactive requests, and unauthenticated API requests returning `401`.

## Identity Services

`DbServices.AddIdentityServices(...)` wires the library's custom stores and managers into ASP.NET Core Identity without Entity Framework.

The validated registration surface includes:

- `IUserStore<UserRecord>`
- login, claim, password, security-stamp, 2FA, email, lockout, authenticator-key, recovery-code, and phone-number user-store interfaces
- `IRoleStore<RoleRecord>`
- `UserManager<UserRecord>`
- `RoleManager<RoleRecord>`
- `SignInManager<UserRecord>`
- personal-data protection through `IProtectedUserStore<UserRecord>`
- default ASP.NET Identity token flows for password reset, email confirmation, change-email, change-phone-number, and authenticator tokens

Default token-provider mapping:

- password reset uses `TokenOptions.DefaultProvider`
- email confirmation and change-email use `TokenOptions.DefaultEmailProvider`
- change-phone-number uses `TokenOptions.DefaultPhoneProvider`
- authenticator tokens use `TokenOptions.DefaultAuthenticatorProvider`

The generic overload also registers the app-specific token provider under:

- `options.AppInformation.AppName`
- `typeof(TTokenProvider).Name`

That lets custom token workflows opt in explicitly while keeping the stock Identity methods aligned with the default provider names.

## SQL Builder

`Jakar.SqlBuilder` is a separate, dependency-free project (referenced by `Jakar.Database`) that builds validated, dialect-correct SQL from a fluent `ref struct` chain. It targets SQLite, Microsoft SQL Server, and PostgreSQL, chosen either by name or at runtime:

```csharp
using Jakar.SqlBuilder;

// Named dialect
SqlResult a = SqlBuilder.PostgreSQL.Select("id", "name").From("users")
                        .Where("active").EqualTo(true).Build();

// Runtime dialect from the live Database (db.Dialect maps DatabaseType -> SqlDialectKind)
SqlResult b = SqlBuilder.For(db.Dialect).Select("*").From("users").Build();
```

`SqlResult` carries `.Sql` and a `.Parameters` set. PostgreSQL folds identifiers to bare `snake_case`/lower (`name`); SQL Server and SQLite quote (`[name]` / `"name"`).

The integration with the ORM is contracts-based and additive — it does **not** replace the legacy `SqlCommand` / `SqlInterpolatedStringHandler` paths:

- `ITableRecord<TSelf>` now also implements `Jakar.SqlBuilder.ISqlTable<TSelf>`, and `ColumnMetaData` implements `ISqlColumn`, so any `TableRecord` works with the strongly-typed `<T>` overloads (`Select<User>(nameof(User.Name))`, `Where<User>(...)`, etc.), which validate the property is a mapped column and translate it to the real column name.
- `Database.Dialect` exposes the builder dialect; `DbOptions.SqlBuilder` carries `SqlBuilderOptions`.

Core `Jakar.SqlBuilder` takes **no** dependency on Jakar.Database (no Npgsql / SqlClient / Identity). See [SQL Builder README](Jakar.SqlBuilder/README.md), the [specification](Jakar.SqlBuilder/SPECIFICATION.md), and the [dependency rationale](Jakar.SqlBuilder/DEPENDENCY-REFACTOR.md).

## Provider Status

The target remains PostgreSQL plus Microsoft SQL Server, 