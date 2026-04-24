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
- binary import/export
- comparison and equality logic
- table-specific domain helpers

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

Integration tests require Docker because the existing fixture uses Testcontainers PostgreSQL:

```powershell
dotnet test Jakar.Database.Tests\Jakar.Database.Tests.csproj
```

## Immediate Improvement Areas

If you want to keep pushing in the current direction, these are the next high-value steps:

1. Move more provider-specific SQL generation behind `DatabaseType` instead of embedding PostgreSQL syntax in static query helpers.
2. Expand generator coverage to reader-based import/export helpers where the pattern is stable enough.
3. Separate Docker-backed integration tests from pure unit tests so local verification is cheaper.
4. Audit all public get-only properties on table records and mark non-persisted ones with `[DbIgnore]` consistently.
5. Decide how the analyzer should be shipped through NuGet so consumers get the generator automatically, not only solution builds.

## Related Docs

- [Package README](Jakar.Database/README.md)
- [Database Type Notes](Jakar.Database/Features.md)
