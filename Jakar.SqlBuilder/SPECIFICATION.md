# Jakar.SqlBuilder — Developer Specification

> **Status:** Draft v1.0 — design specification for a fresh ref-struct redesign.
> **Target framework:** `net10.0` (C# 14 language features).
> **Owner:** Jakar.Database project.
> **Goal:** Generate provably valid, dialect-correct SQL from a fluent chain of method calls, using `ref struct` builders and zero intermediate heap allocations on the hot path.

---

## 1. Overview

Jakar.SqlBuilder is a zero-allocation, type-state-validated SQL query builder. The caller composes a query as a fluent chain (`SqlBuilder.PostgreSql.Select()...`), and the library emits syntactically valid SQL for the chosen dialect plus a captured parameter set. Two layers of validation guarantee correctness:

1. **Compile-time (type-state).** The fluent interface is shaped so that only legal clause orderings compile. You cannot call `.Having()` before `.GroupBy()`, cannot `.Set()` on a `SELECT`, and cannot terminate a query with an open parenthesis group. Illegal sequences are simply not in the type's method surface.
2. **Runtime (structural).** At `Build()` time the writer verifies balanced parentheses, that all required clauses were emitted, that parameter indices are contiguous, and that identifier quoting closed correctly. This is the safety net for the small set of invariants the type system cannot express cheaply.

The redesign supersedes the existing `EasySqlBuilder` (`record struct` over `StringBuilder`). That code remains as a behavioral reference but is not extended; the new architecture is built from `ref struct` writers and span buffers from the ground up.

### 1.1 Design goals

The builder must support **all reachable SQL syntax** for three dialects — SQLite, Microsoft SQL Server (T-SQL), and PostgreSQL — selected from the root entry point (e.g. `SqlBuilder.PostgreSql`, `SqlBuilder.SqlServer`, `SqlBuilder.Sqlite`). It must avoid heap allocations while building a query: intermediate state lives on the stack or in pooled buffers, and the only unavoidable allocation is the final `string` (and only when the caller asks for a `string` rather than writing into their own buffer). It must offer **both** parameterized values (`@p0`, `$1`, `?`) and escaped inline literals, chosen per value by the caller. And it must be ergonomic: the common case reads like SQL.

### 1.2 Non-goals

The library does not execute SQL, manage connections, or map results — it produces a query string and parameter set that a caller hands to Dapper, ADO.NET, or any other executor. It is not an ORM and performs no schema introspection at runtime. It does not attempt to validate that referenced tables or columns exist; it validates *syntax and structure*, not *semantics against a live schema*.

---

## 2. Design principles

**Ref struct everywhere on the build path.** Every fluent stage is a `ref struct`. A `ref struct` cannot be boxed, captured in a closure, stored on the heap, or escape to an `async` state machine — exactly the constraints that make a stack-only builder safe and allocation-free. The compiler enforces that the builder never outlives the buffer it writes into.

**One mutable writer, threaded by `ref`.** All real state — the character buffer, the parameter collector, the parenthesis-depth counter, the emitted-clause flags — lives in a single `SqlWriter` value created once at the root. Each fluent stage is a tiny `ref struct` holding a `ref SqlWriter` field. Stages carry no buffer of their own; they only encode *type-state* (where you are in the grammar) and forward writes to the shared writer. Chaining a hundred clauses therefore allocates nothing and copies only a machine word (the managed `ref`) per call.

**Type-state via distinct stage types, not interfaces returned by value.** The previous design returned interfaces (`ISelector`, `IWhere`), which boxes a struct the moment it is assigned to the interface variable. The redesign instead returns *concrete `ref struct` stage types*. The grammar position is the type. Interfaces still exist, but only as **generic constraints** using the C# 13 `allows ref struct` anti-constraint — never as by-value return types on the hot path.

**Dialect as a static-abstract strategy type parameter.** Dialect differences (identifier quoting, parameter prefix, `LIMIT` vs `TOP` vs `OFFSET/FETCH`, `RETURNING` vs `OUTPUT`, supported features) are expressed through an `ISqlDialect` interface with **`static abstract` members** (C# 11+). Builders are generic over `TDialect`. Because the dialect is a struct type argument and its members are static-abstract, the JIT monomorphizes and devirtualizes every dialect call — there is no virtual dispatch and no dialect object on the heap.

**Fail closed.** Anything ambiguous throws rather than emitting questionable SQL. An open parenthesis at `Build()`, a `WHERE` with no predicate, a non-contiguous parameter set — all throw `SqlBuildException` with the partially built SQL attached for debugging.

**Codebase conventions.** Per project rules: never `var`; constants are `UPPER_CASE`; every block uses `{` and `}`. Public surface is `Nullable` enabled. `internal` members carry the buffer plumbing; only stage transitions and terminals are `public`.

---

## 3. Architecture

### 3.1 Component map

```
SqlBuilder (static root)
   └─ .PostgreSql / .SqlServer / .Sqlite   →  fixes TDialect
         └─ Root<TDialect> (ref struct)     →  owns SqlWriter, exposes verbs
               ├─ Select() → SelectStage<TDialect>
               ├─ Insert() → InsertStage<TDialect>
               ├─ Update() → UpdateStage<TDialect>
               ├─ Delete() → DeleteStage<TDialect>
               └─ With()   → CteStage<TDialect>   (recursive/non-recursive CTEs)

SqlWriter (ref struct)            ← the single source of mutable state
   ├─ Span<char> buffer           ← rented from ArrayPool<char>, grows by doubling
   ├─ int length                  ← write cursor
   ├─ int parenDepth              ← incremented on '(', decremented on ')'
   ├─ ClauseFlags emitted         ← bitset of clauses written (for required-clause checks)
   ├─ ParameterCollector params   ← captures values + assigns ordinals
   └─ ref into caller IBufferWriter<char>   (optional zero-copy sink)

ISqlDialect (static-abstract interface)
   ├─ static abstract void QuoteIdentifier(ref SqlWriter w, ReadOnlySpan<char> id)
   ├─ static abstract void WriteParameter(ref SqlWriter w, int ordinal)
   ├─ static abstract void WriteLimitOffset(ref SqlWriter w, ...)
   ├─ static abstract bool SupportsReturning  { get; }
   ├─ static abstract bool SupportsFullOuterJoin { get; }
   └─ ... feature flags + emitters

  implemented by  PostgreSqlDialect, SqlServerDialect, SqliteDialect (readonly structs)
```

### 3.2 The threading model in one example

```csharp
// All of this is stack-only. The only heap object is the final string.
string SQL = SqlBuilder.PostgreSql
                       .Select("id", "name")
                       .From<User>()
                       .Where("age").GreaterThan(18)        // 18 -> inline literal
                       .And("status").EqualTo(param: "active")
                       .OrderBy("name").Asc()
                       .Limit(50)
                       .Build();

// Produces (PostgreSQL):
//   SELECT "id", "name" FROM "users" WHERE "age" > 18 AND "status" = $1 ORDER BY "name" ASC LIMIT 50;
// Parameters: { $1 = "active" }
```

`Select(...)` constructs a `SqlWriter` (renting a buffer), writes `SELECT "id", "name"`, and returns a `SelectStage<PostgreSqlDialect>` whose only field is `ref SqlWriter`. Each subsequent call writes into that same writer and returns the next stage type by value (a single-word copy). `Build()` runs runtime verification, appends `;`, and materializes the `string`, then returns the buffer to the pool.

### 3.3 Why `ref struct` and not `record struct`

A `record struct` (the old design) is copyable to the heap, can be boxed, and can be captured — so the compiler cannot guarantee the buffer's lifetime, and the design defensively used a `StringBuilder` (heap, resizes allocate). A `ref struct` holding a `ref SqlWriter` cannot escape its stack frame, so the buffer can live in pooled memory with a deterministic return at `Build()`. The trade-off is the usual `ref struct` ergonomic cost: stages cannot be stored in fields, used across `await`, or placed in collections. For a fluent builder that is consumed in a single expression, those restrictions are exactly the desired contract.

### 3.4 Buffer lifecycle

`SqlWriter` rents an initial `char[]` from `ArrayPool<char>.Shared` (default 1 KiB, tunable). Writes append at `length`; when capacity is exceeded the writer rents the next power-of-two buffer, copies, and returns the old one. `Build()` (or the `WriteTo` terminal) returns the buffer to the pool in a `finally`. Because the writer is a `ref struct`, it cannot be retained past the build expression, so there is no use-after-return hazard — but callers who use the `WriteTo(IBufferWriter<char>)` terminal skip the final copy entirely and stream characters straight into their sink (e.g. a `PipeWriter` or a pre-sized `ArrayBufferWriter<char>`).

---

## 4. Dialect abstraction

### 4.1 Root entry points

The dialect is fixed at the root and flows through every stage as the `TDialect` type parameter. There is no way to mix dialects within one query; choosing a dialect chooses the entire grammar surface.

```csharp
public static class SqlBuilder
{
    public static Root<PostgreSqlDialect> PostgreSql => new();
    public static Root<SqlServerDialect>  SqlServer  => new();
    public static Root<SqliteDialect>     Sqlite     => new();

    // Generic escape hatch for custom/derived dialects.
    public static Root<TDialect> For<TDialect>()
        where TDialect : struct, ISqlDialect, allows ref struct => new();
}
```

### 4.2 The `ISqlDialect` contract

Dialect behavior is exposed through `static abstract` interface members so calls devirtualize to direct calls under a struct type argument. The interface is split into *emitters* (write dialect-specific syntax) and *feature flags* (gate grammar so unsupported constructs are caught at compile time where possible, at runtime otherwise).

```csharp
public interface ISqlDialect
{
    // ---- Identity / quoting ----
    static abstract char   OpenQuote  { get; }   // " for PG/SQLite/ANSI, [ for SQL Server
    static abstract char   CloseQuote { get; }   // " ... ] ...
    static abstract void   QuoteIdentifier(ref SqlWriter w, ReadOnlySpan<char> identifier);
    static abstract void   QuoteStringLiteral(ref SqlWriter w, ReadOnlySpan<char> value);

    // ---- Parameters ----
    static abstract void   WriteParameter(ref SqlWriter w, int ordinal); // @p0 / $1 / ?
    static abstract string ParameterName(int ordinal);                   // for the param set

    // ---- Paging ----
    static abstract void   WriteLimitOffset(ref SqlWriter w, long? limit, long? offset, bool hasOrderBy);

    // ---- Feature flags (compile-time gates via wrapper types; runtime guard otherwise) ----
    static abstract bool   SupportsReturning        { get; }  // PG + SQLite 3.35+; SQL Server uses OUTPUT
    static abstract bool   SupportsOutputClause     { get; }  // SQL Server
    static abstract bool   SupportsFullOuterJoin    { get; }  // false for SQLite < 3.39 / MySQL
    static abstract bool   SupportsBooleanType      { get; }  // PG yes; SQL Server/SQLite emulate
    static abstract bool   SupportsOnConflict       { get; }  // PG + SQLite
    static abstract bool   SupportsMerge            { get; }  // SQL Server, PG 15+, SQLite no
    static abstract bool   SupportsDistinctOn       { get; }  // PG only
    static abstract bool   SupportsFilterClause     { get; }  // PG aggregate FILTER(WHERE ...)
    static abstract bool   RequiresOrderByForOffset { get; }  // SQL Server OFFSET/FETCH needs ORDER BY
    static abstract bool   FoldsUnquotedToLower     { get; }  // PG lowercases unquoted identifiers
}
```

### 4.3 Per-dialect divergence table

| Concern                | SQLite                        | SQL Server (T-SQL)                  | PostgreSQL                         |
|------------------------|-------------------------------|-------------------------------------|------------------------------------|
| Identifier quote       | `"id"` (also `` `id` ``,`[id]`) | `[id]` (also `"id"` w/ QUOTED_IDENTIFIER) | `"id"`                       |
| String literal quote   | `'a''b'`                       | `'a''b'` (N'...' for unicode)       | `'a''b'` (or `E'...\n'`)           |
| Parameter token        | `?` positional or `@name`/`$name`/`:name` | `@p0`                    | `$1` positional                    |
| Row paging             | `LIMIT n OFFSET m`             | `OFFSET m ROWS FETCH NEXT n ROWS ONLY` (needs `ORDER BY`); legacy `TOP n` | `LIMIT n OFFSET m` |
| Returning rows         | `RETURNING ...` (3.35+)        | `OUTPUT INSERTED.* / DELETED.*`     | `RETURNING ...`                    |
| Upsert                 | `ON CONFLICT (...) DO UPDATE`  | `MERGE` / `IF EXISTS`               | `ON CONFLICT (...) DO UPDATE`      |
| Boolean                | `0` / `1` (no bool type)       | `0` / `1` (`BIT`)                   | `TRUE` / `FALSE`                   |
| `FULL OUTER JOIN`       | 3.39+ only                    | yes                                 | yes                                |
| `DISTINCT ON`          | no                             | no                                  | yes                                |
| Auto-increment DDL     | `INTEGER PRIMARY KEY AUTOINCREMENT` | `IDENTITY(1,1)`                | `GENERATED ... AS IDENTITY` / `SERIAL` |

### 4.4 Compile-time vs runtime feature gating

Where a feature changes the *shape* of the chain (e.g. `RETURNING` exists), the stage type exposes the method only when the dialect advertises support, using a constrained extension method:

```csharp
public static class ReturningExtensions
{
    public static ReturningStage<TDialect> Returning<TDialect>(
        this InsertValuesStage<TDialect> stage, params ReadOnlySpan<string> columns)
        where TDialect : struct, ISqlDialect, ISupportsReturning, allows ref struct
    {
        return stage.WriteReturning(columns);
    }
}
```

`SqliteDialect` and `PostgreSqlDialect` implement the marker interface `ISupportsReturning`; `SqlServerDialect` does not, so `.Returning(...)` is not even offered on a SQL Server chain — the caller is steered to `.Output(...)` instead. Where a feature only changes *content* (e.g. boolean literal spelling), the dialect emitter handles it transparently and no gating is needed. Where a runtime condition is required (SQL Server `OFFSET` needing `ORDER BY`), `Build()` consults `RequiresOrderByForOffset` and the emitted-clause flags, throwing `SqlBuildException` if violated.

---

## 5. Values: parameters and inline literals

Every value-accepting method comes in two forms so the caller chooses, per value, between a bound parameter and an escaped inline literal.

### 5.1 The two paths

```csharp
.Where("status").EqualTo(param: "active")   // → ... = $1   ; param set gets $1 = "active"
.Where("age").GreaterThan(18)               // → ... > 18   ; inline literal, no parameter
.Where("name").Like(inline: "A%")           // → ... LIKE 'A%' (escaped)
```

The convention: a method taking a CLR value as a **`param:`-named** argument (or via the `.Param(value)` value-wrapper) binds a parameter; a method taking the value positionally, or via `.Inline(value)`, emits an escaped literal. Numeric and boolean inline literals are emitted directly (after a type check); string, `char`, `DateTime`, `Guid`, `byte[]`, and `decimal` inline literals are emitted through the dialect's `QuoteStringLiteral` / type-specific formatter so they are correctly escaped and culture-invariant.

### 5.2 `SqlValue` — the value wrapper

To keep the surface allocation-free and uniform, both forms funnel through a non-boxing `readonly ref struct SqlValue` that holds either a literal span to inline or a reference to the CLR value to bind:

```csharp
public readonly ref struct SqlValue
{
    // exactly one mode is active
    internal readonly ReadOnlySpan<char> InlineText;   // pre-formatted literal, or default
    internal readonly object?            BoundValue;    // bound parameter payload, or null sentinel
    internal readonly SqlValueKind       Kind;          // Inline | Bound | Null | Default | RawSql

    public static SqlValue Param(object? value);        // bind
    public static SqlValue Inline(ReadOnlySpan<char> formatted);
    public static SqlValue Of(long value);              // inline numeric fast paths (no alloc)
    public static SqlValue Of(bool value);
    public static SqlValue Null   { get; }              // emits NULL / IS NULL
    public static SqlValue Raw(ReadOnlySpan<char> sql); // trusted raw fragment, never escaped
}
```

`SqlValue.Raw` is the explicit, audited escape hatch for trusted SQL fragments (e.g. `CURRENT_TIMESTAMP`, a sub-expression). It is the *only* path that bypasses escaping, is named to be greppable in code review, and is documented as the injection-risk boundary.

### 5.3 ParameterCollector

Bound values are gathered by a `ParameterCollector` embedded in `SqlWriter`. It assigns sequential ordinals (`0,1,2,…`), stores each payload in a pooled buffer, and at `Build()` produces the parameter set. Ordinals are dialect-rendered lazily (`$1`, `@p1`, `?`) when the token is written, but stored canonically so the returned `SqlParameterSet` is dialect-correct and contiguous. The collector itself avoids per-value heap allocation by writing into a rented `SqlParameter[]` that doubles on overflow; payloads that are reference types are simply referenced, not copied.

```csharp
public readonly struct SqlParameter
{
    public required string Name  { get; init; }   // "$1" / "@p1" / "?"
    public required object? Value { get; init; }
    public DbType? DbType { get; init; }           // optional explicit typing
}

public sealed class SqlParameterSet : IReadOnlyList<SqlParameter>
{
    public bool IsEmpty { get; }
    public void AddTo(IDbCommand command);          // ADO.NET convenience
    public DynamicParameters ToDapper();            // Dapper.Contrib convenience
}
```

The `SqlParameterSet` is the single intentional heap allocation produced alongside the SQL string, returned only when the query actually bound parameters. A parameter-free query returns `SqlParameterSet.Empty`, which allocates nothing.

### 5.4 Safety posture

Bound parameters are injection-safe by construction. Inline literals are escaped by the dialect formatter and are safe for the supported scalar types, but the spec is explicit that inline string literals derived from untrusted input are discouraged in favor of `param:`. `SqlValue.Raw` is never escaped and must only receive constant or developer-authored fragments. Identifiers (table/column names) are always passed through the dialect's `QuoteIdentifier`, which doubles the closing-quote character to prevent identifier injection.

---

## 6. Validation

### 6.1 Layer 1 — compile-time type-state

The fluent surface is a directed graph of `ref struct` stage types; each stage exposes only the transitions legal from that grammar position. Representative fragment for `SELECT`:

```
SelectStage ──.From()──▶ FromStage ──.Join()──▶ JoinOnStage ──.On()──▶ FromStage
                 │                      └──.Where()──▶ WhereStage
                 ├──.Where()──▶ WhereStage ──.And()/.Or()──▶ WhereStage
                 │                          └──.GroupBy()──▶ GroupByStage
                 ├──.GroupBy()──▶ GroupByStage ──.Having()──▶ HavingStage
                 ├──.OrderBy()──▶ OrderStage ──.Asc()/.Desc()──▶ OrderStage
                 └──(terminal).Limit()/.Build()/.WriteTo()
```

Because `HavingStage` is only reachable from `GroupByStage`, `.Having()` before `.GroupBy()` does not exist on the type and does not compile. Because terminals (`Build`, `WriteTo`) are only present on stages where the query is complete, you cannot build a half-finished statement. Sub-expression scopes (`.OpenGroup()` / a sub-builder lambda) return a nested stage parameterized by a continuation type, so a group must be closed before the outer chain can terminate. The set of invalid orderings this eliminates includes: aggregate clauses out of order, `SET` on a non-`UPDATE`, `VALUES` without `INSERT INTO`, `ON` without a preceding `JOIN`, and terminating with an open group.

### 6.2 Layer 2 — runtime structural verification

Some invariants are impractical to encode in types (the cost is type explosion). These are checked in `SqlWriter.Verify()` invoked by every terminal:

- **Balanced parentheses.** `parenDepth` must be `0`. A non-zero depth throws `SqlBuildException("Unbalanced parentheses", partialSql)`.
- **Required clauses present.** `ClauseFlags` must contain the mandatory members for the statement kind (e.g. `INSERT` requires `INTO` and either `VALUES`/`SELECT`/`DEFAULT VALUES`; `UPDATE` requires `SET`).
- **Dialect runtime gates.** e.g. SQL Server `OFFSET/FETCH` requires `ORDER BY` → checked against flags; `FULL OUTER JOIN` on a dialect that lacks it → throws with a clear message naming the dialect.
- **Parameter contiguity.** Ordinals must be `0..n-1` with none skipped (guards against a partially consumed value wrapper).
- **Identifier/quote closure.** Every `OpenQuote` written has a matching `CloseQuote` (tracked by a parallel counter), catching a malformed identifier emit.
- **Empty-predicate guard.** A `WHERE`/`HAVING`/`ON` that opened but received no predicate throws rather than emitting a dangling keyword.

### 6.3 Why both

Type-state gives instant editor feedback and makes whole classes of bugs unrepresentable, but encoding *every* rule in types causes combinatorial type growth and harsh error messages. Runtime checks are cheap (a handful of integer comparisons at build time), produce actionable messages with the offending partial SQL, and cover the long tail. Together they make "the builder emitted invalid SQL" a non-occurrence: either it does not compile, or it throws before returning a string.

---

## 7. Grammar coverage

The builder must express all reachable SQL for the three target dialects. Coverage is organized by statement family. Each construct lists its fluent entry and the stage it transitions to. Constructs unsupported by a dialect are gated per §4.4.

### 7.1 SELECT / query expressions

The `SELECT` family is the largest surface and must cover:

- **Projection:** column list, `*`, qualified `table.column`, aliases (`AS`), expressions, `DISTINCT`, `DISTINCT ON (...)` (PG), `TOP n` / `TOP n PERCENT` / `WITH TIES` (SQL Server), scalar subqueries, `CASE WHEN ... THEN ... ELSE ... END`.
- **Sources (`FROM`):** table, table alias, subquery (derived table), `VALUES` table constructor, function source (`generate_series`, table-valued functions), `LATERAL` / `CROSS APPLY` / `OUTER APPLY`, comma-joined sources.
- **Joins:** `INNER`, `LEFT [OUTER]`, `RIGHT [OUTER]`, `FULL [OUTER]`, `CROSS`, `NATURAL`, self-join, `ON` predicate, `USING (cols)`.
- **Filtering:** `WHERE` with the full predicate grammar (see §7.6).
- **Grouping:** `GROUP BY` columns/expressions, `GROUP BY GROUPING SETS / ROLLUP / CUBE`, `HAVING`.
- **Windowing:** `OVER (PARTITION BY ... ORDER BY ... ROWS/RANGE/GROUPS frame)`, named `WINDOW w AS (...)`, ranking/aggregate window functions, aggregate `FILTER (WHERE ...)` (PG).
- **Set operations:** `UNION`, `UNION ALL`, `INTERSECT`, `INTERSECT ALL`, `EXCEPT` / `EXCEPT ALL` (and SQL Server `EXCEPT` without `ALL`), with correct precedence/parenthesization.
- **Ordering & paging:** `ORDER BY col [ASC|DESC] [NULLS FIRST|LAST]`, `LIMIT/OFFSET` vs `OFFSET/FETCH` vs `TOP` per dialect.
- **CTEs:** `WITH name AS (...)`, multiple CTEs, `WITH RECURSIVE`, `MATERIALIZED`/`NOT MATERIALIZED` hints (PG).
- **Locking:** `FOR UPDATE` / `FOR SHARE` / `SKIP LOCKED` (PG), `WITH (NOLOCK)` / table hints (SQL Server).

```csharp
// Window function + CTE + set op example (PostgreSQL)
string SQL = SqlBuilder.PostgreSql
    .With("ranked").As(cte => cte
        .Select("user_id", "score")
        .Column(c => c.RowNumber().Over(o => o.PartitionBy("user_id").OrderByDesc("score")), as: "rn")
        .From<Score>())
    .Select("user_id", "score")
    .From("ranked")
    .Where("rn").EqualTo(1)
    .Build();
// WITH "ranked" AS (
//   SELECT "user_id", "score", ROW_NUMBER() OVER (PARTITION BY "user_id" ORDER BY "score" DESC) AS "rn"
//   FROM "scores")
// SELECT "user_id", "score" FROM "ranked" WHERE "rn" = 1;
```

### 7.2 INSERT

Covers: `INSERT INTO t (cols) VALUES (...)`, multi-row `VALUES`, `INSERT ... SELECT`, `INSERT ... DEFAULT VALUES`, `ON CONFLICT (...) DO NOTHING | DO UPDATE SET ...` (PG/SQLite), `RETURNING` (PG/SQLite) / `OUTPUT INSERTED.*` (SQL Server), `WITH` prefix, `OVERRIDING SYSTEM VALUE` (PG identity).

```csharp
string SQL = SqlBuilder.PostgreSql
    .Insert().Into<User>("name", "email")
    .Values(param: "Ada", param: "ada@x.io")
    .OnConflict("email").DoUpdateSet("name", fromExcluded: true)
    .Returning("id")
    .Build();
// INSERT INTO "users" ("name","email") VALUES ($1,$2)
// ON CONFLICT ("email") DO UPDATE SET "name" = EXCLUDED."name" RETURNING "id";
```

### 7.3 UPDATE

Covers: `UPDATE t SET col = val, ...`, `SET (a,b) = (...)` row form (PG), `FROM` join-update (PG/SQLite) / `UPDATE ... FROM` (SQL Server `UPDATE t SET ... FROM ... JOIN ...`), `WHERE`, `RETURNING`/`OUTPUT`, `WITH` prefix.

### 7.4 DELETE

Covers: `DELETE FROM t`, `USING` (PG) / `DELETE ... FROM ... JOIN` (SQL Server), `WHERE`, `RETURNING`/`OUTPUT DELETED.*`, truncate via dedicated `.Truncate<T>()`.

### 7.5 DDL and other statements

Beyond DML the builder exposes DDL so "all reachable SQL" holds: `CREATE TABLE` (columns, types, `PRIMARY KEY`, `FOREIGN KEY`, `UNIQUE`, `CHECK`, `DEFAULT`, identity/auto-increment per dialect, `IF NOT EXISTS`), `ALTER TABLE` (`ADD/DROP/ALTER COLUMN`, constraints, rename), `DROP TABLE/INDEX/VIEW` (`IF EXISTS`, `CASCADE`), `CREATE [UNIQUE] INDEX` (columns, `INCLUDE`, partial `WHERE`, `USING gin/gist` PG), `CREATE VIEW`, and `MERGE` (SQL Server, PG 15+). DDL is a separate stage tree from DML so its grammar does not pollute the DML stage types. Column type names are emitted through the dialect (`INTEGER`/`INT`, `TEXT`/`NVARCHAR(max)`, `BOOLEAN`/`BIT`, `BYTEA`/`VARBINARY`/`BLOB`, `TIMESTAMPTZ`/`DATETIME2`/`TEXT`).

### 7.6 Predicate / expression grammar (shared)

`WHERE`, `HAVING`, `ON`, and `CHECK` all consume the same predicate sub-grammar, implemented once and reused:

- **Comparisons:** `=`, `<>`/`!=`, `<`, `<=`, `>`, `>=`, `IS [NOT] DISTINCT FROM`.
- **Null:** `IS NULL`, `IS NOT NULL`.
- **Range/set:** `BETWEEN a AND b`, `IN (...)`, `IN (subquery)`, `NOT IN`, `= ANY(array)` (PG).
- **Pattern:** `LIKE`, `NOT LIKE`, `ILIKE` (PG), `GLOB` (SQLite), `SIMILAR TO` (PG), `ESCAPE`.
- **Existence:** `EXISTS (subquery)`, `NOT EXISTS`.
- **Boolean composition:** `AND`, `OR`, `NOT`, explicit grouping `( ... )` via `.OpenGroup()`/`.CloseGroup()` or a nested lambda that auto-balances.
- **Operators/functions:** arithmetic (`+ - * / %`), string concat (`||` / `+` / `CONCAT`), `COALESCE`, `NULLIF`, `CAST(x AS type)` / `x::type` (PG), JSON operators (`->`, `->>`, `#>`, `@>` PG), arithmetic on intervals/dates.
- **Aggregates:** `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`, `COUNT(DISTINCT ...)`, `STRING_AGG`/`GROUP_CONCAT`/`ARRAY_AGG`, with optional `FILTER (WHERE ...)`.

Grouping via lambda keeps parentheses balanced automatically:

```csharp
.Where(g => g
    .Column("age").GreaterThan(18)
    .And("country").EqualTo(param: "US"))
.Or(g => g.Column("vip").EqualTo(true))
// WHERE ("age" > 18 AND "country" = $1) OR ("vip" = TRUE)
```

The lambda form takes a `ref struct` predicate builder by `ref`; because it is a `ref struct` it cannot escape the lambda, and the parenthesis counter guarantees the group closes — any imbalance is caught by §6.2 at build time, though the lambda shape makes imbalance unrepresentable in practice.

---

## 8. API surface summary

### 8.1 Naming conventions

Fluent verbs mirror SQL keywords in PascalCase (`Select`, `From`, `InnerJoin`, `GroupBy`, `OrderByDesc`). Value binding uses named arguments (`param:`, `inline:`) or the `SqlValue` factory. Generic `From<T>()` resolves the table name through `KeyWords.GetTableName` (reused from the existing code: attribute/`[Table]`-driven, falling back to a pluralized type name). Terminals are `Build()` (→ `SqlResult`), `BuildSql()` (→ `string` only), and `WriteTo(IBufferWriter<char>)` (zero-copy).

### 8.2 The result type

```csharp
public readonly struct SqlResult
{
    public required string           Sql        { get; init; }
    public required SqlParameterSet  Parameters { get; init; }
    public SqlDialectKind            Dialect    { get; init; }

    public override string ToString();          // the SQL text
    public void Deconstruct(out string sql, out SqlParameterSet parameters);
}
```

`SqlResult` is a normal (non-ref) `readonly struct` because it is the *output* — the builder's ref-struct lifetime has ended and the SQL is now an ordinary string safe to return, store, and pass to an executor.

### 8.3 Representative end-to-end surface

```csharp
// SELECT with join, group, having, order, paging — SQL Server
SqlResult r = SqlBuilder.SqlServer
    .Select("u.name").Count("o.id", as: "orders")
    .From<User>("u")
    .LeftJoin<Order>("o").On("o.user_id").EqualToColumn("u.id")
    .Where("u.active").EqualTo(true)
    .GroupBy("u.name")
    .Having(h => h.Count("o.id").GreaterThan(5))
    .OrderByDesc("orders")
    .Offset(0).FetchNext(20)
    .Build();
// SELECT [u].[name], COUNT([o].[id]) AS [orders] FROM [users] [u]
// LEFT JOIN [orders] [o] ON [o].[user_id] = [u].[id]
// WHERE [u].[active] = 1 GROUP BY [u].[name] HAVING COUNT([o].[id]) > 5
// ORDER BY [orders] DESC OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
```

---

## 9. Error handling

All build-time failures throw `SqlBuildException : InvalidOperationException`, carrying the partially built SQL (`PartialSql`), the failing invariant (`Reason` enum: `UnbalancedParentheses`, `MissingRequiredClause`, `UnsupportedFeature`, `ParameterGap`, `EmptyPredicate`, `UnclosedIdentifier`, `InvalidLiteral`), and, for dialect-gating failures, the offending `SqlDialectKind`. Messages are specific and actionable (e.g. `"SQL Server OFFSET/FETCH requires an ORDER BY clause; none was emitted."`). Argument-level misuse caught before any SQL is written (null identifier, empty column list) throws `ArgumentException`/`ArgumentNullException` at the call site. The library never silently emits questionable SQL — §2 "fail closed."

Because stages are `ref struct`s, exceptions cannot leak a half-built writer to the heap; the rented buffer is returned in the terminal's `finally` even on the throwing path.

---

## 10. Performance

### 10.1 Targets

The hot path (compose → `BuildSql`) must perform zero managed heap allocations except the single final `string`, and zero allocations at all on the `WriteTo(IBufferWriter<char>)` path for parameter-free queries. A typical 5–8 clause `SELECT` should build in well under a microsecond and stay within the initially rented 1 KiB buffer (no resize). Parameter capture for a query with N bound values allocates at most one `SqlParameter[]` (pooled) plus the `SqlParameterSet` wrapper.

### 10.2 Techniques

State lives in one `ref struct` threaded by `ref`, so chaining copies only a managed pointer per call. Buffers come from `ArrayPool<char>.Shared` and are returned deterministically. Keyword and dialect-token writes use `ReadOnlySpan<char>` constants (the `KeyWords` table, extended) — no interpolation, no `string.Format`. Integer and boolean inline literals format directly into the span via `TryFormat`, never `ToString()`. Dialect calls are `static abstract` over a struct type argument, so the JIT devirtualizes them to direct calls and can inline the small ones. `params ReadOnlySpan<T>` overloads (C# 13) let column lists pass without an intermediate array.

### 10.3 Benchmark plan

A BenchmarkDotNet suite (`Jakar.SqlBuilder.Benchmarks`) measures allocations (`[MemoryDiagnoser]`) and time for: a trivial `SELECT *`, a representative join+filter+order query, a 100-row multi-`VALUES` insert, and a deeply nested predicate. The acceptance bar is `0 B` allocated for the `WriteTo` path on parameter-free queries and exactly one allocation (the string) for `BuildSql`. Results are tracked against the legacy `EasySqlBuilder` (which allocates a `StringBuilder` plus its growth buffers) to demonstrate the redesign's improvement.

---

## 11. Testing strategy

Correctness rests on four test layers, in the `Jakar.SqlBuilder.Tests` project:

The **golden-SQL** layer asserts exact emitted strings for each construct across all three dialects — a large parameterized table of (chain → expected SQL, expected parameters). This is the primary regression net and the executable form of §7. The **round-trip execution** layer runs generated SQL against real engines — SQLite in-memory (in-proc), SQL Server and PostgreSQL via Testcontainers — confirming the SQL not only matches a string but actually parses and runs, catching gaps the golden strings might enshrine. The **validation/negative** layer asserts that illegal chains throw the right `SqlBuildException.Reason` (and, via a small set of compile-fail snippets checked by a source-level test, that type-state genuinely blocks illegal orderings). The **property/fuzz** layer generates random valid chains and asserts every output is balanced, terminates with `;`, has contiguous parameters, and re-parses on the target engine.

A **compile-time guarantee** test compiles known-bad snippets (e.g. `.Having()` before `.GroupBy()`) with the Roslyn scripting/`CSharpCompilation` API and asserts they fail to compile — turning the type-state claims of §6.1 into enforced tests rather than prose. Allocation is asserted in the benchmark suite's `[MemoryDiagnoser]` runs wired into CI as a threshold gate.

---

## 12. Project structure

```
Jakar.SqlBuilder/
├─ SqlBuilder.cs                 # static root: PostgreSql / SqlServer / Sqlite / For<T>
├─ Root.cs                       # Root<TDialect> ref struct: verbs (Select/Insert/...)
├─ Writing/
│  ├─ SqlWriter.cs               # the single mutable ref struct (buffer, depth, flags)
│  ├─ ParameterCollector.cs      # ordinal assignment + payload pooling
│  ├─ SqlValue.cs                # ref struct value wrapper (param/inline/raw/null)
│  └─ ClauseFlags.cs             # [Flags] bitset of emitted clauses
├─ Dialects/
│  ├─ ISqlDialect.cs             # static-abstract contract + marker interfaces
│  ├─ PostgreSqlDialect.cs
│  ├─ SqlServerDialect.cs
│  └─ SqliteDialect.cs
├─ Stages/
│  ├─ Select/  (SelectStage, FromStage, JoinOnStage, WhereStage, GroupByStage,
│  │            HavingStage, OrderStage, WindowStage, SetOpStage ...)
│  ├─ Insert/  (InsertStage, InsertColumnsStage, InsertValuesStage, ConflictStage, ReturningStage)
│  ├─ Update/  (UpdateStage, SetStage, UpdateFromStage)
│  ├─ Delete/  (DeleteStage, DeleteUsingStage)
│  ├─ Ddl/     (CreateTableStage, AlterTableStage, IndexStage, ...)
│  └─ Predicate/ (PredicateStage shared by WHERE/HAVING/ON/CHECK)
├─ KeyWords.cs                   # span keyword constants + GetTableName (carried over)
├─ Results/
│  ├─ SqlResult.cs               # output struct
│  ├─ SqlParameter.cs / SqlParameterSet.cs
│  └─ SqlBuildException.cs
└─ GlobalUsings.cs

Jakar.SqlBuilder.Tests/          # golden, round-trip (Testcontainers), negative, compile-fail, fuzz
Jakar.SqlBuilder.Benchmarks/     # BenchmarkDotNet + MemoryDiagnoser (CI alloc gate)
```

Naming follows the project conventions already in `Jakar.SqlBuilder` (e.g. `__`-prefixed private fields, `UPPER_CASE` constants, explicit types, braces).

---

## 13. Phased roadmap

The build order front-loads the architectural risk. **Phase 1** establishes `SqlWriter`, `SqlValue`, `ParameterCollector`, the dialect interface with all three implementations, and the `SELECT` stage tree through `WHERE`/`ORDER BY`/paging — proving the ref-struct threading, zero-alloc claim, and dialect strategy end to end with the golden + benchmark harness. **Phase 2** completes `SELECT` (joins, grouping/having, set ops, CTEs, windows) and adds `INSERT`/`UPDATE`/`DELETE` with `RETURNING`/`OUTPUT`/`ON CONFLICT`. **Phase 3** adds DDL and `MERGE`, the property/fuzz and compile-fail test layers, and the Testcontainers round-trip suite. **Phase 4** is hardening: the full per-dialect divergence matrix, locking clauses, JSON/array operators, and documentation with a migration guide from `EasySqlBuilder`.

---

## 14. Open questions

A few decisions are deferred to implementation and flagged here. Whether sub-queries should be expressed only via nested lambdas or also accept a pre-built `SqlResult`/fragment (the latter risks dialect mixing and parameter renumbering — likely lambda-only). How far to push compile-time gating before the type explosion cost outweighs the benefit versus deferring to runtime checks (the §6.3 boundary). Whether to expose an optional `record struct` façade for callers who cannot work within `ref struct` lifetime rules, accepting its allocations as the price of flexibility. And how strictly to validate identifier characters versus trusting `QuoteIdentifier` escaping (proposed: escape always, validate only on an opt-in strict mode).


