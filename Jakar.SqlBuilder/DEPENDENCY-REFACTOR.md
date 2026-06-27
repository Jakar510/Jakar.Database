# Moving `ITableRecord` into Jakar.SqlBuilder — Impact & Recommended Design

> Companion to `SPECIFICATION.md`. Answers: what does pulling `ITableRecord` into Jakar.SqlBuilder affect, and what is the cleanest way to get a dialect-correct typed builder shared by both libraries, including the PostgreSQL lowercase-vs-quote behavior and the Identity / EF / DI surface.

---

## 1. TL;DR

Do **not** move `ITableRecord<TSelf>` into Jakar.SqlBuilder as written. As written it transitively forces Jakar.SqlBuilder to reference **Npgsql, Microsoft.Data.SqlClient, Microsoft.AspNetCore.Identity, and Dapper** — the exact heavyweight, non-AOT, non-trim dependencies the builder spec exists to avoid. That inverts the layering: a string-building library would depend on a web/identity/driver stack.

Instead, **invert the dependency**. Define in Jakar.SqlBuilder only the small SQL-shape contract the builder actually needs (table name + lean column metadata, no driver types), and have Jakar.Database's `ITableRecord<TSelf>` **extend** that contract while keeping all persistence/identity/driver members where they already live. Jakar.Database then references Jakar.SqlBuilder with no cycle, and the typed overloads from `SPECIFICATION.md` §9 constrain against the lean contract — which `TableRecord<TSelf>` already satisfies. This supersedes the "bridge assembly" idea in §9.8: the constraint moves into core and Jakar.Database simply implements it.

Separately, identifier casing (the `"Name"` → `name` requirement) must become a **per-dialect rendering policy**, not a global behavior baked into `SqlName` — because only PostgreSQL should fold to `snake_case`/lower; SQL Server and SQLite must preserve case and bracket/quote.

---

## 2. What `ITableRecord` actually drags in

Auditing the interface member by member against the assemblies each pulls in:

| Member | Type referenced | Assembly pulled into SqlBuilder | Verdict |
|---|---|---|---|
| `ITableName.TableName` | `SqlName` | Jakar.Database (lightweight value) | **Move** (lean) |
| `IJsonModel<TSelf>` | Jakar.Extensions | Jakar.Extensions *(already referenced)* | OK / drop from contract |
| `IEqualComparable<TSelf>` | Jakar.Extensions | Jakar.Extensions *(already referenced)* | OK / drop from contract |
| `ClassProperties` | `ImmutableArray<PropertyInfo>` | System.Collections.Immutable + reflection | Replace with lean column list |
| `Hasher` | `PasswordHasher<TSelf>` | **Microsoft.AspNetCore.Identity** | **Keep in Jakar.Database** |
| `MetaData` | `TableMetaData<TSelf>` | drags `ColumnMetaData` → **Npgsql + Microsoft.Data.SqlClient** | **Keep**, expose lean view |
| `PropertyCount` | `int` | — | Move (lean) |
| `Create(DbDataReader)` | `System.Data.Common` | System.Data | **Keep in Jakar.Database** |
| `ToDynamicParameters()` | `CommandParameters` | Jakar.Database (Dapper wrapper) | **Keep** |
| `GetHash()` | `UInt128` | — | Keep (not builder concern) |
| `Modified()` | `TSelf` | — | Keep |
| `Import(NpgsqlBatchCommand, …)` | `NpgsqlBatchCommand` | **Npgsql** | **Keep in Jakar.Database** |
| `Import(NpgsqlBinaryImporter, …)` | `NpgsqlBinaryImporter` | **Npgsql** | **Keep in Jakar.Database** |
| `Import(DataRow, …)` | `System.Data` | System.Data | **Keep in Jakar.Database** |

The interface mixes three unrelated responsibilities: **SQL shape** (table name, columns — the only part the builder needs), **persistence/hydration** (`Create(DbDataReader)`, the three `Import` overloads, `ToDynamicParameters`), and **identity/domain** (`Hasher`, `GetHash`, `Modified`, JSON/equality). Only the first belongs in a SQL-text library.

`TableMetaData`/`ColumnMetaData` are themselves un-movable as-is: `ColumnMetaData` exposes `NpgsqlDbType PostgresDbType`, `SqlDbType SqlDbType`, and constructs `Microsoft.Data.SqlClient.SqlParameter`. Moving them would drag both drivers into the builder.

---

## 3. Recommended design — interface segregation + dependency inversion

### 3.1 Lean contracts owned by Jakar.SqlBuilder

Jakar.SqlBuilder defines the minimal surface it needs. No driver types, no Identity, no Dapper:

```csharp
namespace Jakar.SqlBuilder;

public interface ISqlTableName
{
    static abstract ref readonly SqlName TableName { get; }   // SqlName also moves here
}

// One column's SQL-relevant shape — driver-agnostic.
public interface ISqlColumn
{
    string  PropertyName { get; }   // canonical CLR name (the nameof target)
    string  ColumnName   { get; }   // raw, un-cased, un-quoted name
    Type    ClrType      { get; }
    bool    IsNullable   { get; }
    bool    IsPrimaryKey { get; }
    bool    IsIdentity   { get; }
    // Per-dialect type name as a plain string, keyed by the builder's own dialect enum:
    string  TypeName( SqlDialectKind dialect );
}

// The static-abstract shape the typed overloads constrain against.
public interface ISqlTable<TSelf> : ISqlTableName
    where TSelf : ISqlTable<TSelf>
{
    static abstract IReadOnlyList<ISqlColumn> SqlColumns { get; }   // frozen, cached
    static abstract bool TryGetColumn( string propertyName, out ISqlColumn column );
}
```

`SqlDialectKind` is the builder's own three-value (extensible) enum; the Jakar.Database `DatabaseType` maps onto it (§5.3). `SqlName` and the casing helper move into Jakar.SqlBuilder too, but the casing is applied **at dialect render time**, not eagerly (§4).

### 3.2 Jakar.Database extends, doesn't relocate

`ITableRecord<TSelf>` keeps every existing member and simply gains the lean contract as a base:

```csharp
namespace Jakar.Database;

public interface ITableRecord<TSelf> : ISqlTable<TSelf>,           // ← new base from SqlBuilder
                                       IJsonModel<TSelf>, IEqualComparable<TSelf>
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    static abstract ref readonly ImmutableArray<PropertyInfo> ClassProperties { get; }
    static abstract PasswordHasher<TSelf> Hasher   { get; }
    static abstract TableMetaData<TSelf>  MetaData { get; }
    static abstract int                   PropertyCount { get; }

    static abstract TSelf       Create( DbDataReader reader );
    CommandParameters ToDynamicParameters();
    UInt128           GetHash();
    TSelf             Modified();
    ValueTask Import( NpgsqlBatchCommand   batch,    CancellationToken token );
    ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token );
    ValueTask Import( DataRow              row,      CancellationToken token );
}
```

`TableMetaData<TSelf>` implements the lean view by exposing its `ColumnMetaData` set as `ISqlColumn` (a thin adapter: `ColumnName`, `PropertyType` → `ClrType`, `IsNullable`, `IsPrimaryKey`, identity flags, and `TypeName(kind)` delegating to its existing `this[propertyName, DatabaseType]` indexer). The driver enums (`NpgsqlDbType`, `SqlDbType`) stay on `ColumnMetaData` and never enter the contract. `TableRecord<TSelf>` satisfies `ISqlTable<TSelf>` by forwarding `SqlColumns`/`TryGetColumn` to `MetaData`.

### 3.3 Resulting reference graph

```
Jakar.SqlBuilder            (lean: Jakar.Extensions only — no Npgsql/Identity/drivers)
        ▲
        │ references
        │
Jakar.Database              (Npgsql, SqlClient, Sqlite, Identity, Dapper, …)
```

No cycle. The typed overloads (`Select<TRecord>`, `Where<TRecord>`, `On<TRecord>`, `EqualToColumn<TRecord>`, `CreateTable<TRecord>`, …) live in **core** Jakar.SqlBuilder, constrained `where TRecord : ISqlTable<TRecord>`. `TableRecord<TSelf>` qualifies, so they light up automatically in Jakar.Database with zero per-call allocation (frozen lookups, static-abstract reads, no boxing).

---

## 4. Identifier casing as a dialect policy (the lowercase requirement)

Today `SqlName`'s constructor eagerly calls `.SqlName()` → `Strings.ToSnakeCase` (it lives in `PostgresParams.cs`). That is **PostgreSQL-correct but globally wrong**: applied under SQL Server it would silently rename your columns. For accuracy across three dialects, identifier rendering must move to the dialect:

- **PostgreSqlDialect.WriteIdentifier** → `ToSnakeCase` + lower, emit **bare** (`date_created`), no quotes. Quote *only* when the folded name is a reserved word or contains unsafe characters; in that rare case quote and preserve the folded form. This preserves the existing `name`-not-`"Name"` behavior you want.
- **SqlServerDialect.WriteIdentifier** → preserve case, emit `[Name]` (bracket-quoted), always safe.
- **SqliteDialect.WriteIdentifier** → preserve case, emit `"Name"` (double-quoted) or bare when safe.

The canonical stored name therefore must be the **un-transformed** column/property name (so each dialect can fold or quote it independently). Where `ColumnMetaData.ColumnName` is already snake_cased for Postgres, either (a) store the raw name and let the PG dialect fold, or (b) have `ISqlColumn.ColumnName` return the raw name and keep the snake_cased form as a Postgres-only projection. Option (a) is cleaner and is the recommended change. Net effect: one source of truth for the name, three dialect renderings, no accidental cross-dialect renames — "constancy and accuracy" satisfied by construction.

This also generalizes to the future dialects you want (MySQL backticks + lowercase, Oracle uppercase-folding, Firebird): each is just another `WriteIdentifier` policy.

---

## 5. Impact on Jakar.Database

### 5.1 Compile-time / structural

Adding `: ISqlTable<TSelf>` to `ITableRecord<TSelf>` requires `TableRecord<TSelf>` to implement `SqlColumns`/`TryGetColumn` — a few forwarding members to `MetaData`, no behavioral change. `SqlName` moves namespace (Jakar.SqlBuilder); a `global using` alias or type-forward keeps existing call sites compiling. Removing the eager snake_case from `SqlName` (§4) is the one behavioral change and must be done together with adding the PG dialect folding, or Postgres SQL would regress to PascalCase.

### 5.2 SQL generation

Jakar.Database currently builds SQL through `SqlCommand` helpers + `SqlInterpolatedStringHandler` + `TableMetaData.CreateTableSql(DatabaseType)`, with the dialect resolved at runtime via the ambient `Database.Current.DatabaseType`. Once the validated builder is available, those helpers can be reimplemented on top of it (`SqlCommand.GetAll<TSelf>()` → `SqlBuilder.For(dialect).Select<TSelf>(…).From<TSelf>()…`). This is the payoff: every store/table query becomes dialect-correct, parameterized, and structurally validated instead of hand-assembled. It can be done incrementally — the builder and the legacy `SqlCommand` strings can coexist during migration.

### 5.3 What does *not* change

`ColumnMetaData`'s driver members, the migration pipeline, Dapper mapping, `CommandParameters`, and the record hydration (`Create(DbDataReader)`, `Import`) are untouched — they stay in Jakar.Database. The builder consumes the lean view and never sees a driver type.

---

## 6. Identity, EF-style features, and DI

### 6.1 Identity stores are unaffected in shape

`UserStore : IUserStore<UserRecord>, IUserLoginStore<…>, IUserClaimStore<…>, …`, `RoleStore`, `UserManager`, `RoleManager`, token providers — all continue to implement the standard ASP.NET Core Identity interfaces against the fixed `UserRecord`/`RoleRecord`. None of those interfaces touch SQL text, so making the builder a dependency does not alter the Identity surface. What changes is *internal*: the SQL each store emits is generated by the typed builder, so `UserStore` CRUD, token lookup, and role queries become dialect-correct automatically. Because `UserRecord`/`RoleRecord` are `TableRecord`s, they satisfy `ISqlTable<TSelf>` and work with `Select<UserRecord>(nameof(UserRecord.UserName))` etc.

### 6.2 The dialect must become a first-class, DI-bound choice

The accuracy requirement argues against the ambient `Database.Current.DatabaseType` lookup for SQL generation. Bind the dialect explicitly so stores/tables resolve it deterministically:

- Add to the `Database` base an abstract/static dialect accessor, e.g. `public abstract SqlDialectKind Dialect { get; }` (each concrete `PostgresDatabase`/`SqlServerDatabase`/`SqliteDatabase` returns its kind — it already overrides `DatabaseType`, so this is a one-line projection).
- `DbTable<TSelf>` and the Identity stores obtain the builder via the injected `Database` (`db.Dialect`) rather than `Database.Current`, removing the ambient dependency from the hot path.

### 6.3 `DbServices.AddDatabase<TDatabase, …>` — changes

`AddDatabase` already fixes the dialect through `TDatabase : Database`, so no new type parameter is strictly required. The additions are registration-only:

```csharp
self.Services.AddSingleton(options);
// NEW: expose the dialect + builder options resolved from the chosen TDatabase
self.Services.AddSingleton<SqlDialectKind>(static sp => sp.GetRequiredService<TDatabase>().Dialect);
self.Services.AddSingleton(options.SqlBuilder);          // NEW: SqlBuilderOptions (see 6.5)
// …existing telemetry / serilog / fusioncache / TDatabase / health / identity …
self.Services.AddIdentityServices<…>(options);
```

If you prefer the dialect known at the type level (for `SqlBuilder.For<TDialect>()` monomorphization inside stores), add an optional overload `AddDatabase<TDatabase, TDialect, …>` where `TDialect : struct, ISqlDialect` and register `TDialect` as the strategy — but the runtime-`SqlDialectKind` form is sufficient and keeps the existing 1- and 14-parameter overloads intact.

### 6.4 `AddIdentityServices<…>` — changes

Signature unchanged. The 13 type parameters, the `AddIdentity<UserRecord, RoleRecord>()` chain, validators, and token providers all stay. The only consideration: ensure the stores can reach the dialect/builder (they already take `Database`/services via DI, so `db.Dialect` is available). No new registrations are required here beyond what `AddDatabase` adds in 6.3. `AddDataProtection`, `AddPasswordValidator`, `AddInMemoryTokenCaches`, authentication and authorization wiring are untouched.

### 6.5 `DbOptions` — add a builder options block

Surface the builder's knobs so they're configurable per application:

```csharp
public sealed class DbOptions
{
    // …existing…
    public SqlBuilderOptions SqlBuilder { get; set; } = SqlBuilderOptions.Default;
}

public sealed record SqlBuilderOptions
{
    public bool StrictTypes       { get; init; } = false; // validate CLR value vs column ClrType
    public bool AliasProjections  { get; init; } = false; // backpatch SELECT-list aliases (spec §9.5)
    public int  InitialBufferSize { get; init; } = 1024;  // ArrayPool rent size
    public static SqlBuilderOptions Default { get; } = new();
}
```

---

## 7. Rollout order (low-risk, incremental)

1. **Extract lean contracts** (`ISqlTableName`, `ISqlColumn`, `ISqlTable<TSelf>`, `SqlName`, `SqlDialectKind`) into Jakar.SqlBuilder; add `Jakar.Database → Jakar.SqlBuilder` reference. No behavior change yet.
2. **Adapt metadata**: implement `ISqlColumn` on `ColumnMetaData`, `ISqlTable<TSelf>` on `TableRecord<TSelf>` (forward to `MetaData`). Add the lean base to `ITableRecord<TSelf>`.
3. **Move casing to the dialect**: stop eager snake_case in `SqlName`; add PG fold-and-bare / SqlServer bracket / SQLite quote identifier policies. Golden-test all three against the current Postgres output to prove no regression.
4. **Bind the dialect**: add `Database.Dialect`; register it in `AddDatabase`; add `DbOptions.SqlBuilder`.
5. **Reimplement SQL helpers on the builder** behind the existing `SqlCommand` API, one statement family at a time (Select → Insert → Update → Delete → DDL), with round-trip tests on SQLite/SqlServer/Postgres.
6. **Switch the Identity stores** to builder-generated SQL; verify against the Identity conformance tests.

Each step compiles and passes tests independently; the builder and legacy SQL coexist until step 6 completes.

---

## 8. Risks & notes

The single behavioral risk is the casing move (step 3): if `SqlName`'s eager snake_case is removed without the PG dialect folding in place, Postgres identifiers regress to PascalCase — do them together and gate on golden tests. Keeping Jakar.SqlBuilder dependency-light also keeps it AOT/trim-friendly even though Jakar.Database is not (`IsAotCompatible=False`), which is worth preserving as a property of the core. Finally, resist the temptation to expose `TableMetaData`/`ColumnMetaData` directly through the contract for convenience — that is precisely what reintroduces the driver dependencies; always go through the lean `ISqlColumn` view.
