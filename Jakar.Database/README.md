# Jakar.Database

`Jakar.Database` is a record-oriented ORM layer for .NET with:

- `IAsyncEnumerable<T>`-first query surfaces
- shared table metadata for common records
- migration helpers with explicit rollback SQL
- an incremental source generator that removes common `ITableRecord<TSelf>` boilerplate

## Install

```powershell
dotnet add package Jakar.Database
```

## What It Provides

- `DbTable<TSelf>` wrappers for common CRUD and streaming access
- base records such as `TableRecord<TSelf>`, `PairRecord<TSelf>`, and `OwnedTableRecord<TSelf>`
- migration metadata and table creation helpers
- Dapper-friendly command parameter generation
- optional source-generated implementations of `Create(DbDataReader)` and `ToDynamicParameters()`

## Source Generator Contract

The generator emits `Create(DbDataReader)` and `ToDynamicParameters()` when your record:

- is `partial`
- implements `ITableRecord<TSelf>`
- has a `DbDataReader` constructor
- does not already declare those members manually

That lets you keep only the meaningful code in the record itself.

Example:

```csharp
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

In that shape, the generator supplies:

- `public static ExampleRecord Create(DbDataReader reader)`
- `public override CommandParameters ToDynamicParameters()`

## Migration Guidance

Down migrations matter. Every migration that changes schema state should have an explicit rollback path.

Current rollback support covers:

- shared function setup such as `SetLastModified`
- enum migrations
- table creation migrations
- generated index migrations

`MigrationManager.RevertMigrations(...)` now executes registered `DownSQL` in reverse order and removes the corresponding applied migration rows.

## Provider Notes

The long-term target is PostgreSQL plus Microsoft SQL Server. Today, the shared table metadata is moving in that direction, but parts of the query layer are still PostgreSQL-first. Treat SQL Server support as in progress unless a specific surface has been validated.

## Build

```powershell
dotnet build ..\Jakar.Database.slnx
```

## Test

Fast generator-focused checks:

```powershell
dotnet test ..\Jakar.Database.Tests\Jakar.Database.Tests.csproj --filter GeneratedRecordTests --no-restore
```

Full integration tests require Docker because the existing suite uses PostgreSQL Testcontainers.

## License

MIT. See [LICENSE.txt](./LICENSE.txt).
