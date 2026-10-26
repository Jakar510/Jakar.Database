# Jakar.SqlBuilder — validated fluent SQL builder

A low-allocation, dialect-aware SQL builder built on `ref struct` stages. The type-state only lets legal clause orders compile, and `Build()` verifies the rest. Every construct is written in the chosen dialect's syntax, emulated where the dialect has an equivalent, or rejected with `SqlBuildException`, so the builder never emits SQL the engine would refuse. It implements all of `SPECIFICATION.md` for six dialects. `SPECIFICATION-COMPLIANCE.md` maps each section to its code and tests.

The legacy `SqlCommand` and `SqlInterpolatedStringHandler` paths in Jakar.Database are untouched and work side by side.

## Entry points

```csharp
SqlResult a = SqlBuilder.PostgreSQL.Select("id", "name").From("users").Build();   // named dialect
SqlResult b = SqlBuilder.For(db.Dialect).Select("*").From("users").Build();        // runtime dialect
SqlResult c = SqlBuilder.PostgreSQLWith(options).Select("*").From("users").Build(); // with options
```

The terminals:

- `Build()` returns a `SqlResult` with `.Sql` and `.Parameters`; it converts to `string` implicitly.
- `BuildSql()` returns only the SQL.
- `WriteTo(IBufferWriter<char>)` copies the SQL into your buffer without making a string and returns the parameters.

`Build()` and `BuildSql()` can be called again on the same stage and return the same result. `WriteTo` on a stage that wasn't built is final: a later terminal on it throws. Stages are single-use: once you continue a stage (`from.Where(...)`), the earlier stage (`from`) throws if you use it again, because both share one pooled buffer.

## Dialects

`SqlBuilder.PostgreSQL`, `SqlServer`, `Sqlite`, `MySql`, `Oracle`, `Firebird` (or `SqlBuilder.For(kind)`). Each `Supports...` flag in `SqlDialects` documents the version it assumes: PostgreSQL 15, SQL Server 2022, SQLite 3.39, MySQL 8.0.31, Oracle 23ai, Firebird 5.

| | PostgreSQL | SQL Server | SQLite | MySQL | Oracle | Firebird |
|---|---|---|---|---|---|---|
| Identifiers | bare, snake_case lower | `[Name]` | `"Name"` | `` `Name` `` | `"Name"` | `"Name"` |
| Parameters | `$1` (or `@p0`) | `@p0` | `@p0` | `@p0` | `:p0` | `@p0` |
| Booleans | `TRUE` / `FALSE` | `1` / `0` | `1` / `0` | `1` / `0` | `1` / `0` | `TRUE` / `FALSE` |
| Paging | `LIMIT` / `OFFSET` | `OFFSET ... FETCH` / `TOP` | `LIMIT` / `OFFSET` | `LIMIT` / `OFFSET` | `OFFSET ... FETCH` | `OFFSET ... FETCH` |
| Returning rows | `RETURNING` | `OUTPUT INSERTED/DELETED` | `RETURNING` | — | — | `RETURNING` |
| Upsert | `ON CONFLICT` | `MERGE` | `ON CONFLICT` | `ON DUPLICATE KEY UPDATE` | `MERGE` | `MERGE` |
| `SELECT` without `FROM` | as is | as is | as is | as is | `FROM DUAL` | `FROM RDB$DATABASE` |
| Trailing `;` | yes | yes (always on `MERGE`) | yes | yes | never | yes |

When a dialect lacks a construct that has an exact equivalent, the builder writes the equivalent:

| Construct | Equivalent written | Where |
|---|---|---|
| `NULLS FIRST/LAST` | an extra `CASE WHEN x IS NULL ...` sort key | SQL Server, MySQL |
| `ILIKE` | `LOWER(x) LIKE LOWER(p)` | every dialect but PostgreSQL |
| `FILTER (WHERE ...)` | a `CASE WHEN ...` aggregate argument | where unsupported |
| `IS DISTINCT FROM` | `IS NOT` | SQLite |
| | `<=>` | MySQL |
| | `DECODE` | Oracle |
| `VALUES` tables | `UNION ALL` selects | SQLite, Firebird |
| `TRUNCATE` | `DELETE FROM` | SQLite, Firebird |
| `DEFAULT VALUES` | `() VALUES ()` | MySQL |
| `IF NOT EXISTS` | an `IF OBJECT_ID(...) IS NULL` / `sys.indexes` guard | SQL Server |
| `WITH TIES` / `PERCENT` | inside `TOP` | SQL Server |

Paging and locking are recorded and written when the statement ends, so `Limit` / `Offset` / `FetchNext` / `Top` / `ForUpdate` can be called in any order. Everything else a dialect can't express throws `SqlBuildException(UnsupportedFeature)` naming the dialect.

## SELECT

```csharp
SqlResult r = SqlBuilder.SqlServer
    .Select("u.name").Count("o.id", "orders")
    .From("users", "u")
    .LeftJoin("orders", "o").On("o.user_id").EqualToColumn("u.id")
    .Where("u.active").EqualTo(true)
    .GroupBy("u.name")
    .Having(h => h.Count("o.id").GreaterThan(5))          // or .Having().Count(...).GreaterThan(5)
    .OrderByDesc("orders").NullsLast()
    .Offset(0).FetchNext(20)
    .Build();
```

- **Projection:**
  - columns, `*`, `As`, aggregates (`Count` / `Sum` / `Avg` / `Min` / `Max` / `CountDistinct`);
  - `Column(e => ...)` for expressions and scalar subqueries;
  - `SelectDistinct`, `SelectDistinctOn` (PostgreSQL), `Top(n, percent, withTies)`.
- **Sources:**
  - `From(table | <T> | subquery, alias)`, `FromValues`, `FromFunction`;
  - `CrossApply` / `OuterApply` (written as `LATERAL` where that's the dialect's form), `AndFrom` (comma join), `WithHints` (SQL Server).
- **Joins:**
  - `Inner` / `Left` / `Right` / `Full` / `Cross` / `Natural` joins, to tables or derived tables;
  - `On(...)` (column, expression, or a predicate lambda) or `Using(...)`;
  - a typed `JoinStage<T>` binds a bare `On(nameof(...))` to its record.
- **Filtering:** the shared predicate grammar (below), as a chain or a lambda group.
- **Grouping:**
  - `GroupBy` (columns, typed, expressions), `GroupByRollup` / `GroupByCube` / `GroupByGroupingSets`;
  - `Having()` as a chain, or `Having(h => ...)`.
- **Windows:**
  - `e.RowNumber().Over(w => w.PartitionBy(...).OrderByDesc(...).Rows(...))`, `Over("w")` with `.Window("w", ...)`;
  - `Rank`, `DenseRank`, `Ntile`, `Lag` / `Lead`, `FirstValue` / `LastValue` / `NthValue`;
  - aggregate `filter:`, `StringAgg`, `ArrayAgg`.
- **Set operations:** `Union`, `UnionAll`, `Intersect[All]`, `Except[All]`; each operand has its own tables.
- **CTEs:**
  - `With("name", columns).As(q => ...)`, more CTEs with `.With(...)`, `WithRecursive`;
  - `AsMaterialized` / `AsNotMaterialized`;
  - then any statement: SELECT, INSERT, UPDATE, DELETE or MERGE.
- **Locking:** `ForUpdate` / `ForShare` with `NoWait` / `SkipLocked`.

### Predicates and expressions

Comparisons (inline, bound, or against an expression or a column) and `IsNull` / `IsNotNull`:

```csharp
.Where("age").GreaterThan(18)
.Where("age").GreaterThan(e => e.Number(2).Times().Number(9))
.Where("id").EqualToColumn("o.user_id")
.Where("email").IsNull()
```

Distinctness, ranges and sets:

```csharp
.Where("email").IsDistinctFrom(SqlValue.Null)
.Where("age").NotBetween(30, 50)
.Where("id").In(1, 2, 3)           // also InParams, InValues, In(subquery), the NotIn variants, Any (PostgreSQL)
```

Patterns and existence:

```csharp
.Where("name").ILike("ada")        // also Like / NotLike with an escape character, Glob, SimilarTo
.WhereExists(q => q.Select("id").From("orders").Where("orders.user_id").EqualToColumn("users.id"))
```

Groups, from lambdas that balance their own parentheses:

```csharp
.Where(g => g.Column("age").GreaterThan(18).And("country").EqualTo("US"))
.Or(g => g.Column("vip").EqualTo(true))
// WHERE (age > 18 AND country = $1) OR (vip = TRUE)
```

Expressions (`e => ...`):

- arithmetic, `Modulo`, `Negate`, `Concat`;
- `Coalesce`, `NullIf`, `Cast(x, SqlType)`;
- `Lower`, `Upper`, `Trim`, `Length`, `Abs`, `Round`;
- `Case`, `JsonText` / `JsonObject`, `CurrentDate` / `CurrentTimestamp` / `AddInterval`;
- `Param`, `Value`, `Raw`.

## INSERT / UPDATE / DELETE / MERGE

```csharp
SqlBuilder.PostgreSQL
    .Insert().Into<User>(nameof(User.Name), nameof(User.Email))
    .ValuesParams("Ada", "ada@x.io")
    .OnConflict("email").DoUpdateSet("name", fromExcluded: true)
    .Returning("id");
// INSERT INTO users (name, email) VALUES ($1, $2) ON CONFLICT (email) DO UPDATE SET name = EXCLUDED.name RETURNING id;

SqlBuilder.SqlServer.Update("orders", "o").Set("total", SqlValue.Of(0)).From("users", "u").Where("u.id").EqualToColumn("o.user_id");
// UPDATE [o] SET [total] = 0 FROM [orders] [o], [users] [u] WHERE [u].[id] = [o].[user_id];

SqlBuilder.PostgreSQL.MergeInto("users", "t").Using("staged", "s").On(p => p.Column("t.id").EqualToColumn("s.id"))
    .WhenMatchedThenUpdate(s => s.Set("name", e => e.Column("s.name")))
    .WhenNotMatchedThenInsert(["id", "name"], [e => e.Column("s.id"), e => e.Column("s.name")]);
```

- **INSERT:**
  - `Values` / `ValuesParams` (one or more rows; the value count must match the columns);
  - `Select(q => ...)`, `DefaultValues()`, `OverridingSystemValue()` (PostgreSQL);
  - `OnConflict(...)` then `DoNothing()` / `DoUpdateSet(...).Set(...).Where(...)`;
  - `Returning` / `Output`;
  - `Insert<T>()` lists the record's mapped, non-identity columns.
- **UPDATE:**
  - `Set` (value, expression, `SqlValue.Default`), `SetRow([...], ...)`;
  - `From(...)`, written `UPDATE t, o SET` on MySQL;
  - `Where` (chain or lambda), `Returning` / `Output`.
- **DELETE:**
  - `DeleteFrom(table, alias)`;
  - `Using(...)`: `USING` on PostgreSQL, `FROM` on SQL Server, `DELETE t FROM t, o` on MySQL;
  - `Where`, `Returning` / `Output`.
- **Truncate:** `Truncate(table)` / `Truncate<T>()`.
- **MERGE:**
  - `WhenMatchedThenUpdate` / `WhenMatchedThenDelete` / `WhenNotMatchedThenInsert`, each with an optional condition (written in Oracle's `WHERE` form for Oracle);
  - `WhenNotMatchedBySourceThenDelete` and `Output` (SQL Server).
  - A MERGE needs at least one `WHEN` before it can be built.

`OUTPUT` is moved to where SQL Server wants it: before `VALUES`, `FROM` or `WHERE`.

## DDL

```csharp
SqlBuilder.MySql.CreateTable("widgets", ifNotExists: true)
    .Column("id",    SqlType.BigInt,      c => c.Identity().PrimaryKey())
    .Column("code",  SqlType.VarChar(20), c => c.NotNull().Unique())
    .Column("qty",   SqlType.Int,         c => c.Default(SqlValue.Of(0)).Check(p => p.Column("qty").GreaterOrEqual(0)))
    .Column("owner", SqlType.Int,         c => c.References("users", "id", SqlReferentialAction.SetNull))
    .Constraint("widgets_uq").Unique("code", "owner")
    .Build();

SqlBuilder.For(kind).CreateTable<UserRecord>();    // from the record's metadata, on any dialect; same schema as Jakar.Database's migrations
```

- **`CreateTable`:**
  - columns with portable `SqlType`s or raw type names;
  - identity (`GENERATED ... AS IDENTITY`, `IDENTITY(1,1)`, `AUTO_INCREMENT`, `INTEGER PRIMARY KEY AUTOINCREMENT`), `DEFAULT`, nullability, keys, `CHECK`, `REFERENCES`;
  - MySQL gets a separate `FOREIGN KEY` clause, because it ignores inline `REFERENCES`;
  - table-level `PrimaryKey` / `Unique` / `ForeignKey` / `Check`, optionally named with `Constraint(name)`.
- **`AlterTable`:**
  - `AddColumn`, `DropColumn`, `AlterColumnType`, `RenameColumn`, `RenameTo` (`sp_rename` on SQL Server);
  - `AddPrimaryKey` / `AddUnique` / `AddForeignKey` / `AddCheck`, `DropConstraint`.
- **Drops:** `DropTable` / `DropView` / `DropIndex`, with `ifExists` and `cascade`.
- **`CreateIndex`:** `unique`, `ifNotExists`, `Using("gin")`, columns with directions, `Include`, a partial `Where`.
- **Views:** `CreateView` / `CreateOrReplaceView` (`CREATE OR ALTER` on SQL Server and Firebird).

DDL rejects bound parameters (`InvalidLiteral`): defaults and checks must be inline.

## Values: parameters vs inline

```csharp
.Where("status").EqualTo("active")                   // object  -> $1, bound
.Where("status").EqualTo(SqlValue.Param("active"))   // explicit bound
.Where("age").GreaterThan(18)                        // long    -> 18, inline
.Where("active").EqualTo(true)                       // bool    -> TRUE / 1 per dialect
.Where("id").EqualTo(SqlValue.Inline(guid))          // typed literals: Guid, DateTime(Offset), DateOnly, TimeOnly, byte[], decimal, double
```

`SqlValue.Raw(...)` is the only un-escaped path: it is the audited injection boundary. Identifiers always go through the dialect's quoting, which doubles any quote character inside the name.

## Strongly-typed columns (`nameof`)

Any `ISqlTable<T>` works with the `<T>` overloads. Every Jakar.Database `TableRecord` is one. The overloads check that the property is a mapped column (suggesting near matches when it isn't) and write its real column name. Projections, which are written before `FROM`, are qualified with the alias declared later:

```csharp
SqlBuilder.SqlServer
    .Select<User>(nameof(User.Name)).Count<Order>(nameof(Order.ID), "orders")
    .From<User>("u")
    .LeftJoin<Order>("o").On(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID))
    ...
// SELECT [u].[name], COUNT([o].[id]) AS [orders] FROM [users] [u] LEFT JOIN [orders] [o] ON [o].[user_id] = [u].[id] ...
```

With `StrictTypes`, a value is checked against the column's CLR type, and `IS NULL` against its nullability (`TypeMismatch`).

## Options

```csharp
SqlBuilderOptions opts = new()
{
    InitialBufferSize         = 1024,  // pooled char[] rent size
    AppendStatementTerminator = true,  // trailing ';' (never on Oracle; always on a SQL Server MERGE)
    StrictTypes               = false, // check typed comparisons' values against the column type
    NamedPostgresParameters   = false, // PostgreSQL @p0 instead of $1 (Dapper, Npgsql named binding)
};
```

`AliasProjections` is always on; the property stays for configuration compatibility.

Bound parameters go to ADO.NET with `result.Parameters.AddTo(command)`, which sets Oracle's `BindByName`, or to Dapper with `ToDapper()`.

## Command parameters

`CommandParameters` is a set of `CommandParameter`s for one command against an `ISqlCommandTable`. Each one is a value bound to one column, with an ADO.NET direction and source version. For multi-row statements it holds one group per row, and parameter names are numbered per row.

It also renders the fragments that go with the parameters:

- `ColumnNames`;
- `VariableNames`: literal values are inlined, everything else becomes `@name`;
- `KeyValuePairs`.

The host implements `ISqlCommandTable` / `ISqlCommandColumn`; in Jakar.Database those are `TableMetaData` / `ColumnMetaData`. The driver conversions are extension members there: `ToPostgresParameter`, `ToSqlParameter`, `ToSqliteParameter`, `ToMySqlParameter` and `ToDbParameter`.

## Validation

- **Compile-time.** Stage types only expose legal transitions. `CompileTimeTests` compiles illegal chains with Roslyn and requires them to fail, for example:
  - `Having()` before `GroupBy`;
  - `WHERE` after `ORDER BY`;
  - `MERGE` without `WHEN`.
- **Runtime.** Every terminal checks:
  - balanced parentheses and closed identifiers;
  - closed nested queries;
  - contiguous parameters;
  - required clauses, including `ORDER BY` for SQL Server paging and `WITH TIES`;
  - no bound parameters in DDL.

  Unsupported features throw `SqlBuildException` with the partial SQL attached, and empty column or value lists throw `ArgumentException`.
- **On real engines.** `Jakar.SqlBuilder.Tests/Execution` runs about 330 patterns on SQLite in-process and, through Testcontainers, on PostgreSQL 18, SQL Server 2022, MySQL 8.4, Oracle 23ai Free and Firebird 5. Each pattern must return the same rows everywhere, or be rejected at build time where the dialect can't express it. 400 fuzzed chains per engine must agree with SQLite.

## Allocation posture

The final `string` is the only allocation for `BuildSql()`. `WriteTo` on a parameter-free query allocates nothing, and with parameters the only allocations are one `SqlParameter[]` plus its `SqlParameterSet`. `AllocationTests` gates both, and `Jakar.SqlBuilder.Benchmarks` measures them (`dotnet run -c Release --project Jakar.SqlBuilder.Benchmarks -- --filter *`). Stages are `ref struct`s carried by value over one pooled buffer. The dialect is a `SqlDialectKind` switch.

`Models/Sb_SqlDialectKind.cs` is an inert placeholder (it only declares the namespace) and can be deleted.
