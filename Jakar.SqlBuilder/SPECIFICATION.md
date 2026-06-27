 # Jakar.SqlBuilder — Developer Specification

> **Status:** Draft v1.0 — design specification for a fresh ref-struct redesign.
> **Target framework:** `net10.0` (C# 14 language features).
> **Owner:** Jakar.Database project.
> **Goal:** Generate provably valid, dialect-correct SQL from a fluent chain of method calls, using `ref struct` builders and zero intermediate heap allocations on the hot path.

> **Implementation status (as built).** Phase 1 is implemented and compiles against `net10.0`/C# 14: the lean contracts (`ISqlColumn`, `ISqlTable<T>`, `SqlDialectKind`), the dialect-aware `SqlWriter`, results (`SqlResult`/`SqlParameterSet`/`SqlBuildException`/`SqlBuilderOptions`), `SqlValue`, and the fluent SELECT/INSERT/UPDATE/DELETE stage graph with typed `<T>` overloads, `UNION`/`UNION ALL`, and dialect-correct identifiers/parameters/paging/RETURNING-vs-OUTPUT. Two deviations from this design were made for pragmatism and are documented in `README.md`: the dialect is dispatched at runtime on `SqlDialectKind` (a `switch` in `SqlDialects`) rather than via static-abstract generic strategy types, and `HAVING` is chain-style rather than the lambda form. The **dependency split was implemented as contracts-in-core, not the separate "bridge assembly" of §9.8** — `ISqlTable`/`ISqlColumn`/`SqlDialectKind` live in core `Jakar.SqlBuilder`, and Jakar.Database's `ITableRecord<TSelf>` simply extends `ISqlTable<TSelf>` (see `DEPENDENCY-REFACTOR.md`). The former `EasySqlBuilder` prototype has been **removed**, not retained as a reference.

---

## 1. Overview

Jakar.SqlBuilder is a zero-allocation, type-state-validated SQL query builder. The caller composes a query as a fluent chain (`SqlBuilder.PostgreSql.Select()...`), and the library emits syntactically valid SQL for the chosen dialect plus a captured parameter set. Two layers of validation guarantee correctness:

1. **Compile-time (type-state).** The fluent interface is shaped so that only legal clause orderings compile. You cannot call `.Having()` before `.GroupBy()`, cannot `.Set()` on a `SELECT`, and cannot terminate a query with an open parenthesis group. Illegal sequences are simply not in the type's method surface.
2. **Runtime (structural).** At `Build()` time the writer verifies balanced parentheses, that all required clauses were emitted, that parameter indices are contiguous, and that identifier quoting closed correctly. This is the safety net for the small set of invariants the type system cannot express cheaply.

The redesign replaced the previous `EasySqlBuilder` prototype (`record struct` over `StringBuilder`), which has since been removed; the architecture here is built from `ref struct` writers and pooled buffers from the ground up.

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

> **As built.** Two of the principles above were adjusted in the implementation (see the per-section notes): the single writer is **carried by value (move semantics), not threaded by `ref`** — stages hold `SqlWriter` by value, which is sound because it has no `ref`/`Span` fields (§3.2); and the dialect is a **runtime `SqlDialectKind` switch (`SqlDialects`), not a static-abstract `TDialect`** (§4). The ref-struct, type-state, fail-closed, and convention principles hold as written.

---

## 3. Architecture

### 3.1 Component map

> **As built.** The diagram below is the *design* shape (generic `Root<TDialect>`, static-abstract `ISqlDialect`, a `Span<char>`/`IBufferWriter` sink). The implementation is the non-generic equivalent: `SqlRoot` (carrying a `SqlDialectKind`), a `char[]`-backed `SqlWriter` with no `IBufferWriter` sink yet, and a `SqlDialects` switch in place of `ISqlDialect`. See the §3.2/§3.4/§4 notes.

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

`Select(...)` constructs a `SqlWriter` (renting a buffer), writes `SELECT "id", "name"`, and returns a `SelectStage`. Each subsequent call writes into the writer and returns the next stage type by value. `Build()` runs runtime verification, appends `;`, and materializes the `string`, then returns the buffer to the pool.

> **As built.** The implementation uses a **by-value move** rather than threading a `ref SqlWriter`. Each stage holds the `SqlWriter` *by value* (`private SqlWriter __w;`); a method mutates `__w` then returns `new NextStage(__w)`, copying it forward. Because `SqlWriter` contains no `ref`/`Span` fields — only a pooled `char[]`, a few ints, and the parameter collector — copying it is unrestricted and side-steps the ref-safety/escape rules entirely (which is why stages compile without `scoped`/`[UnscopedRef]` gymnastics). The shared backing array means only the latest copy is ever read; abandoned copies are never touched. The example above also shows the *target* surface (`From<User>()`, `param:` named args); the current entry point is `SqlBuilder.PostgreSQL` and bound values use `SqlValue.Param(...)` / `.EqualTo(param)` (see `README.md`).

### 3.3 Why `ref struct` and not `record struct`

A `record struct` (the old `EasySqlBuilder`) is copyable to the heap, can be boxed, and can be captured — so the compiler cannot guarantee the buffer's lifetime, and that design defensively used a `StringBuilder` (heap, resizes allocate). A `ref struct` cannot escape its stack frame, be boxed, captured in a closure, or stored in a field/collection/`async` state machine, so the pooled buffer has a deterministic lifetime ending at `Build()`. For a fluent builder consumed in a single expression, those restrictions are exactly the desired contract.

### 3.4 Buffer lifecycle

`SqlWriter` rents an initial `char[]` from `ArrayPool<char>.Shared` (`SqlBuilderOptions.InitialBufferSize`, default 1 KiB). Writes append at `length`; when capacity is exceeded the writer rents a larger buffer (at least double), copies, and returns the old one. `Build()` materializes the `string`, builds the `SqlParameterSet`, and returns the buffer to the pool in a `finally`. Because the writer is a `ref struct`, it cannot be retained past the build expression, so there is no use-after-return hazard.

> **As built.** Only the `Build()` terminal exists today (it allocates the final `string`). The zero-copy `WriteTo(IBufferWriter<char>)` terminal — streaming straight into a `PipeWriter`/`ArrayBufferWriter<char>` with no final string allocation — is **planned, not yet implemented**.

---

## 4. Dialect abstraction

> **As built — runtime dispatch, not static-abstract generics.** Sections 4.1–4.4 describe the original *design* in which the dialect is a `struct` type parameter (`TDialect`) with `static abstract` members, JIT-devirtualized per dialect. The **implementation instead carries a `SqlDialectKind` enum on the (non-generic) `SqlWriter` and `switch`es on it** in a static `SqlDialects` helper. This was chosen so the runtime entry point `SqlBuilder.For(SqlDialectKind)` is trivial and the named entry points share one `SqlRoot` type. The cost is a small enum `switch` per identifier/parameter/paging emit instead of a devirtualized call — negligible in practice. The static-abstract strategy remains a viable future optimization; everything below maps 1:1 onto the enum form (`PostgreSqlDialect.QuoteIdentifier` → `SqlDialects.QuoteChars(SqlDialectKind.PostgreSql)`, etc.).

### 4.1 Root entry points

The dialect is fixed at the root and there is no way to mix dialects within one query; choosing a dialect chooses the entire grammar surface. *As built*, the root is the non-generic `SqlRoot`:

```csharp
public static class SqlBuilder
{
    // Named dialects
    public static SqlRoot PostgreSQL => new(SqlDialectKind.PostgreSql, SqlBuilderOptions.Default);
    public static SqlRoot SqlServer  => new(SqlDialectKind.SqlServer,  SqlBuilderOptions.Default);
    public static SqlRoot Sqlite     => new(SqlDialectKind.Sqlite,     SqlBuilderOptions.Default);

    // Runtime-chosen dialect, and per-call options
    public static SqlRoot For( SqlDialectKind dialect, SqlBuilderOptions? options = null );
    public static SqlRoot PostgreSQLWith( SqlBuilderOptions options );
    public static SqlRoot SqlServerWith( SqlBuilderOptions  options );
    public static SqlRoot SqliteWith( SqlBuilderOptions     options );
}
```

The original generic design, for reference:

```csharp
// DESIGN (not implemented): dialect as a static-abstract struct type argument
public static Root<PostgreSqlDialect> PostgreSql => new();
public static Root<TDialect> For<TDialect>() where TDialect : struct, ISqlDialect, allows ref struct => new();
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

## 9. Strongly-typed column references via `TableRecord<T>`

Jakar.Database models every table as a `TableRecord<TSelf>` implementing `ITableRecord<TSelf>`. The builder integrates with this directly so columns can be referenced as `nameof(User.Name)` with the record type supplying both the table context and a validation source. Every column-accepting fluent method gains a `<TRecord>` overload that sits alongside the raw-string overload; the two compose freely in one query.

### 9.1 What the record types already provide

`ITableRecord<TSelf>` exposes, as `static abstract` members, everything the builder needs — with no instance, no runtime reflection, and no allocation, because the metadata is computed once and frozen:

```csharp
public interface ITableName
{
    static abstract ref readonly SqlName TableName { get; }              // sanitized table name
}

public interface ITableRecord<TSelf> : ITableName, IEqualComparable<TSelf>
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    static abstract ref readonly ImmutableArray<PropertyInfo> ClassProperties { get; }
    static abstract              TableMetaData<TSelf>          MetaData        { get; }
    static abstract              int                           PropertyCount   { get; }
}
```

`TableMetaData<TSelf>` holds `FrozenDictionary<string, ColumnMetaData> Properties` keyed by **property name**, plus an indexer `meta[propertyName]` and a dialect-typed indexer `meta[propertyName, DatabaseType]`. Each `ColumnMetaData` carries the real `ColumnName` (honoring `[Column("…")]` renames), `PropertyType`, `DbType`, `IsNullable`, `IsPrimaryKey`, `IsUnique`, `ForeignKey`, and a `FrozenDictionary<DatabaseType, string> DataTypes` of per-dialect type names. This is the entire schema the builder needs to validate references and translate names.

### 9.2 The constraint and the dialect mapping

Typed overloads constrain the record type to the existing CRTP shape, so only real table records are accepted and `TableName` / `MetaData` are reachable as static-abstract calls (JIT-monomorphized, zero dispatch):

```csharp
public WhereStage<TDialect> Where<TRecord>(string propertyName)
    where TRecord : TableRecord<TRecord>, ITableRecord<TRecord>;
```

Each dialect advertises which `Jakar.Database` `DatabaseType` its metadata corresponds to, so the builder pulls dialect-correct type strings straight from `ColumnMetaData`:

```csharp
// added to ISqlDialect
static abstract DatabaseType MetadataType { get; }
// PostgreSqlDialect => DatabaseType.PostgreSQL
// SqlServerDialect  => DatabaseType.MicrosoftSqlServer
// SqliteDialect     => DatabaseType.SQLite
```

### 9.3 The target API

The user-facing goal — both forms valid, mixable in one chain:

```csharp
SqlResult r = SqlBuilder.SqlServer
    .Select<User>(nameof(User.Name)).Count<Order>(nameof(Order.ID), as: "orders")
    .From<User>("u")
    .LeftJoin<Order>("o").On(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID))
    .Where<User>(nameof(User.Active)).EqualTo(true)
    .GroupBy<User>(nameof(User.Name))
    .Having(h => h.Count<Order>(nameof(Order.Name)).GreaterThan(5))
    .OrderByDesc("orders")          // raw string still works for projection aliases
    .Offset(0).FetchNext(20)
    .Build();
```

Note `.On(nameof(Order.UserID))` carries no type argument: a `Join<TRecord>` remembers its record type for the duration of the join, so the bare `.On(...)` defaults to that join's `TRecord` (here `Order`), while `.EqualToColumn<User>(…)` names the other side explicitly. Projection aliases that are not columns (e.g. `"orders"`) stay raw strings — there is nothing to validate against a record.

### 9.4 What each typed overload does

A typed overload performs three steps, all allocation-free:

1. **Validate.** Look up `propertyName` in `TRecord.MetaData.Properties`. `nameof` already guarantees at *compile time* that the member exists on the type; the builder adds the *runtime* guarantee that the member is a **mapped column** (not `[DbIgnore]`d, not unmapped). A miss throws `SqlBuildException(Reason.UnknownColumn)` naming the record, the property, and the nearest matches.
2. **Translate.** Emit `ColumnMetaData.ColumnName`, not the property name — so a `[Column("user_name")]` rename or a casing convention flows through automatically, and a future property rename caught by `nameof` keeps the SQL correct.
3. **Quote & qualify.** Pass the column name through the dialect's `QuoteIdentifier`, and prefix it with the source qualifier per §9.5.

Optional **strict mode** (`SqlBuilderOptions.StrictTypes`) adds a fourth step on value comparisons: check the CLR value against `ColumnMetaData.PropertyType` (e.g. reject `.Where<User>(nameof(User.Age)).EqualTo("oops")` when `Age` is `int`) and reject `IS NULL` on a column whose metadata says `IsNullable == false`. Strict mode is off by default to keep the common path branch-free.

### 9.5 Aliases and qualification (single-pass nuance)

`From<TRecord>("u")` and `Join<TRecord>("o")` register a binding from the record type to its alias in a small stack-resident alias table inside `SqlWriter` (a span of `(typeHandle, aliasSpan)` entries — bounded, no heap). A later typed reference whose `TRecord` has a registered alias emits `alias.column`; with no alias it emits `TableName.column`; with a single unambiguous source it may emit the bare column.

The one wrinkle is the projection list, which is written *before* `FROM` in a single forward pass. When `.Select<User>(nameof(User.Name))` runs, the `"u"` alias is not yet known. Two supported behaviors:

- **Default — qualify by table name.** Emit `[users].[name]`, always valid SQL, fully streaming, zero backtracking.
- **Opt-in — alias backpatch.** With `SqlBuilderOptions.AliasProjections`, the writer records the byte offset of each unresolved projection qualifier and, once `From`/`Join` declares the alias, patches the slot in place within the same buffer (bounded in-buffer rewrite, no new allocation). This reproduces the exact `[u].[name]` form shown in §9.3.

This is called out explicitly because it is the only place the typed feature interacts with the zero-copy forward-writer design; everywhere else (WHERE, JOIN ON, GROUP BY, HAVING, ORDER BY) the typed reference occurs after the source is declared, so the alias is already known and qualification is immediate.

### 9.6 DDL and DML generated from metadata

Because the metadata is complete, whole statements can be generated from a record type. `CreateTable<TRecord>()` walks `TRecord.MetaData.SortedColumns`, emitting each column's dialect type via `ColumnMetaData[dialect.MetadataType]`, plus `NOT NULL`, `PRIMARY KEY`, `UNIQUE`, `FOREIGN KEY`, identity/auto-increment (`IDENTITY(1,1)` / `GENERATED … AS IDENTITY` / `INTEGER PRIMARY KEY AUTOINCREMENT`), and `DEFAULT` from the corresponding attributes — the table writes itself, correctly per dialect. Likewise `Insert<TRecord>()` can default its column list to the mapped, non-identity columns, and `Update<TRecord>()` can target the table by `TableName`. These reuse the same `TableMetaData` already used by Jakar.Database's migration pipeline, so the builder and the migrations agree on schema by construction.

### 9.7 Allocation posture

The typed overloads preserve the zero-allocation guarantee of §11. `nameof(...)` is a compile-time constant string. `TRecord.TableName` and `TRecord.MetaData` are static-abstract reads over a `FrozenDictionary`/`ImmutableArray` — O(1), no allocation, no reflection at call time (reflection ran once at type initialization). The generic type argument means no boxing of the record. The alias table lives in the stack-resident `SqlWriter`. The only added cost over a raw-string call is one frozen-dictionary lookup per typed column reference.

### 9.8 Dependency direction (avoiding a cycle)

> **As built — contracts-in-core (the alternative below), not a bridge assembly.** Jakar.SqlBuilder defines the lean contracts `ISqlColumn` / `ISqlTableName` / `ISqlTable<TSelf>` (and `SqlDialectKind`) directly. Jakar.Database references Jakar.SqlBuilder and **implements** them: `ITableRecord<TSelf> : ISqlTable<TSelf>`, `ColumnMetaData : ISqlColumn`, with `TableMetaData<TSelf>` exposing `SqlColumns`/`TrySqlColumn` and `DatabaseTypeMap` doing the `DatabaseType ↔ SqlDialectKind` mapping. The typed `<T>` overloads therefore live in **core**, constrained `where T : ISqlTable<T>` — there is no separate `Jakar.SqlBuilder.Database` project. The narrative below documents the two options that were considered; the second was chosen.

Neither project referenced the other before this work. The desire is for **Jakar.Database to use Jakar.SqlBuilder** to build its queries — so the reference points `Jakar.Database → Jakar.SqlBuilder`. But the typed overloads of this section need a record's table name and column metadata, which live in Jakar.Database. Putting overloads that referenced `ITableRecord`/`TableMetaData`/`ColumnMetaData`/`DatabaseType` directly in the core builder would force `Jakar.SqlBuilder → Jakar.Database` and create a cycle.

The resolution keeps the core builder dependency-free and adds the binding in a thin **bridge assembly** `Jakar.SqlBuilder.Database` that references *both*:

- **Core `Jakar.SqlBuilder`** stays free of any Jakar.Database reference. It defines the column-source contract it needs as its own minimal interface — e.g. `ISqlTableSource` with `static abstract` `TableName` and a column-name resolver — and all raw-string overloads.
- **`Jakar.SqlBuilder.Database`** (new project) references core + Jakar.Database and provides the `where TRecord : TableRecord<TRecord>, ITableRecord<TRecord>` extension overloads (`Select<TRecord>`, `Where<TRecord>`, `On<TRecord>`, `EqualToColumn<TRecord>`, `CreateTable<TRecord>`, …), adapting `TableMetaData`/`ColumnMetaData` to the core contract and supplying the §9.2 `DatabaseType` mapping.
- **`Jakar.Database`** references core `Jakar.SqlBuilder` (and optionally the bridge) to build its own SQL — no cycle, because the bridge depends on Jakar.Database, not the reverse for the core.

The **chosen** resolution: core `Jakar.SqlBuilder` owns minimal contracts (`ISqlColumn`, `ISqlTable<TSelf>`) that expose only the SQL-shape a builder needs — no driver/Identity types — and Jakar.Database adapts its existing `ColumnMetaData`/`TableMetaData`/`TableRecord` to them. No new project, no change to Jakar.Database's layering beyond adding the reference and the interface implementations. (The earlier draft proposed a separate `Jakar.SqlBuilder.Database` bridge project; that was superseded — see `DEPENDENCY-REFACTOR.md`.) The hard rule holds either way: **core `Jakar.SqlBuilder` takes no dependency on Jakar.Database.**

---

## 10. Error handling

All build-time failures throw `SqlBuildException : InvalidOperationException`, carrying the partially built SQL (`PartialSql`), the failing invariant (`Reason` enum: `UnbalancedParentheses`, `MissingRequiredClause`, `UnsupportedFeature`, `ParameterGap`, `EmptyPredicate`, `UnclosedIdentifier`, `InvalidLiteral`), and, for dialect-gating failures, the offending `SqlDialectKind`. Messages are specific and actionable (e.g. `"SQL Server OFFSET/FETCH requires an ORDER BY clause; none was emitted."`). Argument-level misuse caught before any SQL is written (null identifier, empty column list) throws `ArgumentException`/`ArgumentNullException` at the call site. The library never silently emits questionable SQL — §2 "fail closed."

Because stages are `ref struct`s, exceptions cannot leak a half-built writer to the heap; the rented buffer is returned in the terminal's `finally` even on the throwing path.

The `Reason` enum gains `UnknownColumn` and `TypeMismatch` for the typed-overload validation of §9.4.

---

## 11. Performance

### 11.1 Targets

The hot path (compose → `BuildSql`) must perform zero managed heap allocations except the single final `string`, and zero allocations at all on the `WriteTo(IBufferWriter<char>)` path for parameter-free queries. A typical 5–8 clause `SELECT` should build in well under a microsecond and stay within the initially rented 1 KiB buffer (no resize). Parameter capture for a query with N bound values allocates at most one `SqlParameter[]` (pooled) plus the `SqlParameterSet` wrapper.

### 11.2 Techniques

State lives in one `ref struct` threaded by `ref`, so chaining copies only a managed pointer per call. Buffers come from `ArrayPool<char>.Shared` and are returned deterministically. Keyword and dialect-token writes use `ReadOnlySpan<char>` constants (the `KeyWords` table, extended) — no interpolation, no `string.Format`. Integer and boolean inline literals format directly into the span via `TryFormat`, never `ToString()`. Dialect calls are `static abstract` over a struct type argument, so the JIT devirtualizes them to direct calls and can inline the small ones. `params ReadOnlySpan<T>` overloads (C# 13) let column lists pass without an intermediate array.

### 11.3 Benchmark plan

A BenchmarkDotNet suite (`Jakar.SqlBuilder.Benchmarks`) measures allocations (`[MemoryDiagnoser]`) and time for: a trivial `SELECT *`, a representative join+filter+order query, a 100-row multi-`VALUES` insert, and a deeply nested predicate. The acceptance bar is `0 B` allocated for the `WriteTo` path on parameter-free queries and exactly one allocation (the string) for `BuildSql`. Results are tracked over time as a CI gate to guard against allocation regressions.

---

## 12. Testing strategy

Correctness rests on four test layers, in the `Jakar.SqlBuilder.Tests` project:

The **golden-SQL** layer asserts exact emitted strings for each construct across all three dialects — a large parameterized table of (chain → expected SQL, expected parameters). This is the primary regression net and the executable form of §7. The **round-trip execution** layer runs generated SQL against real engines — SQLite in-memory (in-proc), SQL Server and PostgreSQL via Testcontainers — confirming the SQL not only matches a string but actually parses and runs, catching gaps the golden strings might enshrine. The **validation/negative** layer asserts that illegal chains throw the right `SqlBuildException.Reason` (and, via a small set of compile-fail snippets checked by a source-level test, that type-state genuinely blocks illegal orderings). The **property/fuzz** layer generates random valid chains and asserts every output is balanced, terminates with `;`, has contiguous parameters, and re-parses on the target engine.

A **typed-overload** layer (§9) asserts that `nameof`-based references translate to the correct `ColumnMetaData.ColumnName` (including `[Column]` renames), that unmapped/`[DbIgnore]` properties throw `Reason.UnknownColumn`, that alias qualification matches both the table-name default and the opt-in backpatch form, that `CreateTable<TRecord>()` emits dialect-correct DDL agreeing with the migration pipeline, and that strict mode flags type mismatches. These run against the real `TableRecord<TSelf>` types in Jakar.Database to keep the builder and the schema model in lockstep.

A **compile-time guarantee** test compiles known-bad snippets (e.g. `.Having()` before `.GroupBy()`) with the Roslyn scripting/`CSharpCompilation` API and asserts they fail to compile — turning the type-state claims of §6.1 into enforced tests rather than prose. Allocation is asserted in the benchmark suite's `[MemoryDiagnoser]` runs wired into CI as a threshold gate.

---

## 13. Project structure

**As built.** Files currently live flat at the project root (a workspace sync drop