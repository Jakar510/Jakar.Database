# Jakar.SqlBuilder — validated fluent builder (Phase 1 foundation)

A low-allocation, dialect-aware SQL builder built on `ref struct` stages. This is the Phase-1 vertical from `SPECIFICATION.md`: the contracts, the dialect-aware writer, results, and a working SELECT / INSERT / UPDATE / DELETE surface. It is **purely additive** — the legacy `SqlCommand` and `SqlInterpolatedStringHandler` SQL paths are untouched and continue to work side by side. (The earlier `EasySqlBuilder` prototype has been removed; this builder replaces its root role.) It compiles clean against `net10.0` / C# 14.

## Entry points (both styles supported)

```csharp
// Named dialect (preferred)
SqlResult a = SqlBuilder.PostgreSQL.Select("id", "name").From("users").Build();

// Runtime-chosen dialect
SqlDialectKind kind = db.Dialect;                 // from Jakar.Database
SqlResult b = SqlBuilder.For(kind).Select("*").From("users").Build();

// With options (strict types, alias projections, buffer size)
SqlResult c = SqlBuilder.PostgreSQLWith(options).Select("*").From("users").Build();
```

`SqlResult` carries `.Sql` and `.Parameters` (a `SqlParameterSet`); it implicitly converts to `string`.

## Dialects

`SqlDialectKind` = `Sqlite | SqlServer | PostgreSql` (+ reserved `MySql/Oracle/Firebird`). Per-dialect behavior lives in `SqlDialects`:

- **Identifiers** — PostgreSQL folds to bare lower/snake_case (`name`, not `"Name"`); SQL Server quotes `[name]`; SQLite quotes `"name"`. The PostgreSQL folder is overridable (`SqlDialects.PostgresIdentifierFolder`); Jakar.Database sets it to the curated `PostgresParams.SqlName` so existing column names stay exact.
- **Parameters** — `$1` (PG), `@p0` (SQL Server / SQLite).
- **Paging** — `LIMIT/OFFSET` (PG/SQLite) vs `OFFSET … ROWS FETCH NEXT … ROWS ONLY` + `TOP` (SQL Server).
- **RETURNING vs OUTPUT** — gated per dialect; calling the wrong one throws `SqlBuildException`.

## Values: parameters vs inline

Per value, the caller chooses:

```csharp
.Where("status").EqualTo(SqlValue.Param("active"))   // -> $1 ; bound
.Where("age").GreaterThan(18)                          // -> > 18 ; inline number
.Where("active").EqualTo(true)                         // -> = TRUE / = 1 (dialect)
.Where("name").Like("A%")                              // -> LIKE 'A%' (escaped inline)
```

`SqlValue.Raw(...)` is the only un-escaped path — the audited injection boundary.

## Strongly-typed columns (`nameof`)

Any `ISqlTable<T>` (every Jakar.Database `TableRecord`) works with the `<T>` overloads, which validate the property is a mapped column and translate it to the real column name:

```csharp
SqlResult r = SqlBuilder.SqlServer
    .Select<User>(nameof(User.Name)).Count<Order>(nameof(Order.ID), "orders")
    .From<User>("u")
    .LeftJoin<Order>("o").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID))
    .Where<User>(nameof(User.Active)).EqualTo(true)
    .GroupBy<User>(nameof(User.Name))
    .Having().Count<Order>(nameof(Order.Name)).GreaterThan(5)
    .OrderByDesc("orders")
    .Offset(0).FetchNext(20)
    .Build();
```

## INSERT / UPDATE / DELETE

```csharp
// INSERT (PostgreSQL) — typed columns, all values bound, RETURNING
SqlResult ins = SqlBuilder.PostgreSQL
    .Insert().Into<User>(nameof(User.Name), nameof(User.Email))
    .ValuesParams("Ada", "ada@x.io")          // -> VALUES ($1,$2)
    .Returning(nameof(User.ID))
    .Build();
// INSERT INTO users (name, email) VALUES ($1,$2) RETURNING id;

// INSERT (raw columns, mixed value kinds, multi-row)
SqlResult ins2 = SqlBuilder.Sqlite
    .Insert().Into("users", "name", "email")
    .Values(SqlValue.Param("Ada"), SqlValue.Inline("ada@x.io"))
    .Values(SqlValue.Param("Bob"), SqlValue.Null)
    .Build();

// UPDATE (typed) — Set / Where, then optional RETURNING (PG/SQLite) or OUTPUT (SQL Server)
SqlResult upd = SqlBuilder.PostgreSQL
    .Update<User>()
    .Set<User>(nameof(User.Name), SqlValue.Param("Ada"))
    .Where<User>(nameof(User.ID)).EqualTo(42)
    .Build();
// UPDATE users SET name = $1 WHERE id = 42;

// DELETE (SQL Server) — bracket quoting, bool -> 0/1
SqlResult del = SqlBuilder.SqlServer
    .Delete<User>()
    .Where<User>(nameof(User.Active)).EqualTo(false)
    .Build();
// DELETE FROM [users] WHERE [active] = 0;
```

`Returning(...)` throws on SQL Server (use `Output(...)`, which emits `OUTPUT INSERTED.* / DELETED.*`); `Output(...)` throws on PostgreSQL/SQLite. Multi-row `INSERT` chains additional `.Values(...)` calls.

## Options

`SqlBuilderOptions` is passed via the `*With(options)` entry points (or resolved from `DbOptions.SqlBuilder` in Jakar.Database):

```csharp
SqlBuilderOptions opts = new()
{
    InitialBufferSize         = 1024,  // pooled char[] rent size
    AppendStatementTerminator = true,  // append a trailing ';'
    StrictTypes               = false, // reserved: validate CLR value vs ISqlColumn.ClrType (not yet enforced)
    AliasProjections          = false, // reserved: back-patch SELECT-list aliases (not yet enforced)
};

SqlResult r = SqlBuilder.PostgreSQLWith(opts).Select("*").From("users").Build();
```

`StrictTypes` and `AliasProjections` are accepted but not yet enforced. Bound parameters can be handed to Dapper/ADO.NET via `result.Parameters.ToDictionary()`.

## Two notes vs. the original sketch

1. **`HAVING` is chain-style, not lambda** in this phase: `.Having().Count<Order>(nameof(Order.Name)).GreaterThan(5)` rather than `.Having(h => …)`. The lambda/parenthesized-group predicate form is the next increment (it needs the ref-threaded predicate builder from spec §7.6).
2. **`as:` is `@as`** — `as` is a C# keyword, so pass the alias positionally (`Count<Order>(nameof(Order.ID), "orders")`) or as `@as:`.

## Validation

- **Compile-time** — stage types only expose legal transitions (e.g. `Having()` is only reachable after `GroupBy`, terminals only where a statement is complete).
- **Runtime** — `Build()` verifies balanced parentheses; unsupported features (FULL JOIN / RETURNING / OUTPUT on the wrong dialect) and unmapped typed columns throw `SqlBuildException` with the partial SQL attached.

## Not yet implemented (tracked in SPECIFICATION.md)

`INTERSECT`/`EXCEPT` (`UNION`/`UNION ALL` are implemented), CTEs, window functions, sub-queries, DDL (`CreateTable<T>`), alias-qualified typed projections (spec §9.5), strict-mode value type checks, and the lambda predicate grouping. The legacy `SqlCommand` path remains the production SQL source until these land and the stores are migrated incrementally.

## Allocation posture

One pooled `char[]` per query (returned at `Build()`), the final `string`, and a `SqlParameter[]` only when parameters are bound. Stages are `ref struct`s carried by value (move semantics over a shared buffer) — no per-call heap allocation. The dialect is dispatched on `SqlDialectKind` (a switch); the static-abstract devirtualized strategy from the spec remains a future optimization.

---

## Update — re-created flat layout + set ops

The implementation files were re-created **flat at the project root** (not in `Abstractions/Dialects/Fluent/Results/Writing` subfolders) after a workspace sync dropped the subfolders. C# namespaces are folder-independent, so this compiles identically; you may reorganize into folders later. An inert `Sb_SqlDialectKind.cs` placeholder (it only declares the namespace) can be deleted at your convenience — a transient mount state prevented its removal here.

`Union()` / `UnionAll()` are now implemented: they're available on the pre-`ORDER BY` SELECT terminals (`FromStage`, `SelectWhereStage`, `GroupByStage`) and return a fresh `SqlRoot` so you continue with another `.Select(...)`:

```csharp
SqlResult r = SqlBuilder.PostgreSQL
    .Select("id").From("a").Where("active").EqualTo(true)
    .Union()
    .Select("id").From("b")
    .Build();
// SELECT "id" FROM "a" WHERE "active" = TRUE UNION SELECT "id" FROM "b";
```

Still pending from the spec: `INTERSECT`/`EXCEPT`, CTEs, window functions, sub-queries, DDL (`CreateTable<T>`), alias-qualified typed projections, strict-mode type checks, and the lambda predicate grouping.
