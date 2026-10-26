# Jakar.SqlBuilder — specification compliance

Status of every section of `SPECIFICATION.md` (2026-10-25).

## Summary

**Met or exceeded:** every section. Where the spec targets three dialects, the implementation covers six.

**One shortfall:** the ≈1 µs build time for a typical 5–8 clause query (§11.1). The allocation targets are met.

**Evidence.** Jakar.SqlBuilder.Tests: 2,436 passed, 5 skipped (an engine without the function or type under test).

- Every pattern runs on six real engines: SQLite in-process, and PostgreSQL 18, SQL Server 2022, MySQL 8.4, Oracle 23ai and Firebird 5 in Testcontainers.
- Jakar.Database.Tests: 150 passed, 22 skipped (a fixture that needs an engine the library doesn't support yet).

## Section by section

| § | Requirement | Status | Implementation | Evidence |
|---|---|---|---|---|
| 1.1 | All reachable SQL, three dialects; pooled, allocation-free building; param or inline per value | **Exceeded**: six dialects | `SqlDialects`, `SqlWriter`, `SqlValue` | `Execution/*` on six engines; `AllocationTests` |
| 2 | Ref structs, single writer, type-state, fail closed | **Met** (the writer is carried by value; see deviations) | every stage is a `ref struct` | `CompileTimeTests` (escape attempts) |
| 3.4 | Pooled buffer, returned in `finally` | **Met** | `SqlWriter.Release` | `AllocationTests` |
| 4 | Dialect abstraction | **Met** (runtime switch; see deviations) | `SqlDialects` | `DialectMatrixTests` |
| 4.3 | Divergence handled per dialect | **Exceeded**: emulations where an exact equivalent exists | `SqlDialects`, stages | `SpecificationTests`, `SpecPatterns` |
| 4.4 | Unsupported constructs gated | **Met**: shape at compile time, dialect at runtime | `Require(...)` | each pattern's `RejectedBy` |
| 5.1–5.2 | Bound vs inline per value; typed literals; `Raw` as the only unescaped path | **Met** | `SqlValue`, `SqlLiterals` | `ValueTests`, `spec_typed_inline_values` |
| 5.3 | Contiguous ordinals, dialect tokens, `SqlParameterSet.Empty` | **Met** | `ParameterCollector`, `SqlParameterSet` (`AddTo`, `ToDapper`) | `FuzzTests`, `ValueTests` |
| 6.1 | Illegal orderings don't compile | **Met** | stage graph | `CompileTimeTests`: 22 illegal chains + 3 escapes, with legal controls |
| 6.2 | Runtime verification | **Exceeded**: also checks nested scopes, `WITH`, and params in DDL | `SqlWriter.Verify` | `ValidationTests`, `DialectMatrixTests` |
| 7.1 | SELECT: projection, `DISTINCT ON`, `TOP`/`PERCENT`/`WITH TIES`, subqueries, `CASE`, every source and join, grouping sets, windows, `FILTER`, set operations, `NULLS FIRST/LAST`, CTEs, locking | **Met** for all six dialects (gated where an engine has no equivalent) | `SqlRoot`, `SelectStages`, `WhereGroupOrderStages`, `SqlClauses`, `CteMergeStages` | `SpecPatterns.Select / Grouping / Ordering / Ctes / Locking` |
| 7.2 | INSERT: multi-row, `SELECT`, `DEFAULT VALUES`, `ON CONFLICT`, `RETURNING` / `OUTPUT`, `WITH`, `OVERRIDING SYSTEM VALUE` | **Exceeded**: MySQL upsert, value-count check, `Insert<T>()` | `InsertStages` | `SpecPatterns.Inserts`, `Spec_7_2_Upsert` |
| 7.3 | UPDATE: `SET`, row form, `FROM`, `RETURNING` / `OUTPUT`, `WITH` | **Met** (also MySQL multi-table and SQL Server aliased targets) | `UpdateDeleteStages` | `SpecPatterns.Updates`, `Update_From` |
| 7.4 | DELETE: `USING` / `FROM ... JOIN`, `RETURNING` / `OUTPUT DELETED`, `Truncate<T>()` | **Met** | `UpdateDeleteStages`, `SqlRoot.Truncate` | `SpecPatterns.Deletes`, `Truncate` |
| 7.5 | DDL (`CREATE` / `ALTER` / `DROP` table, index, view) and MERGE | **Met** | `DdlStages`, `CteMergeStages`, `SqlType` | `SpecPatterns.Ddl / Merges` (DDL actually executed per engine) |
| 7.6 | Shared predicate and expression grammar; auto-balanced lambda groups | **Met** | `SqlPredicates`, `SqlExpressions` | `SpecPatterns.Predicates / Expressions`, `Spec_7_6_Grouping`, fuzzing |
| 8.1–8.3 | Naming; `SqlResult`; terminals `Build` / `BuildSql` / `WriteTo` | **Met** | `SqlResult`, `SqlWriter` | `Spec_8_3_End_To_End` |
| 9.1–9.4 | Typed overloads: validate, translate, quote, qualify; `UnknownColumn` with suggestions; StrictTypes | **Met** | `TypedColumns`, `SqlOps.TypedColumn`, `CheckCompared` | `TypedOverloadTests`, `TableRecordOverloadTests`, `Spec_9_4_StrictTypes` |
| 9.5 | Alias back-patch of projections | **Met** (always on) | `SqlWriter.DeferredColumn` / `ResolvePending` | `Spec_9_3_Typed` (exact §9.3 output) |
| 9.6 | `CreateTable<T>()` from metadata, agreeing with migrations; `Insert<T>()` defaults | **Met** on all six dialects; agreement with the migrations proven on PostgreSQL and SQL Server | `CreateTableStage.Columns<T>`, `ISqlColumn` / `ISqlTable` DDL members | `PostgresCreateTableAgreementTests` / `SqlServerCreateTableAgreementTests` (migrations build the same catalog as the frozen legacy DDL); `Record_Tables_Create_From_Metadata` creates all 11 tables on all six engines |
| 9.7 | Typed overloads allocation-free | **Met** | frozen metadata, cached folding | `AllocationTests` |
| 9.8 | No core → Jakar.Database dependency | **Met** (contracts-in-core) | `ISqlColumn`, `ISqlTable` | project references |
| 10 | `SqlBuildException` with `Reason` / `PartialSql` / `Dialect`; `ArgumentException` for argument misuse | **Met** | `SqlBuildException`, `RequireItems` | `ValidationTests`, `Empty_Lists_Are_Rejected` |
| 11.1 | Only the final string allocated (`BuildSql`); 0 B on `WriteTo`; well under 1 µs | **Allocations met; time ≈1 µs** | span-based identifiers, cached PG folding and parameter tokens, lazy StrictTypes boxing | `AllocationTests` (six dialects), benchmarks |
| 11.3 | BenchmarkDotNet suite with `[MemoryDiagnoser]` and the four scenarios | **Met** | `Jakar.SqlBuilder.Benchmarks` | see below |
| 12 | Golden, round-trip, negative, compile-fail, fuzz and typed-overload layers | **Exceeded**: six engines, about 335 patterns, fuzz checked against a reference engine | `Jakar.SqlBuilder.Tests` | the suite |

## Benchmarks

`Jakar.SqlBuilder.Benchmarks`, Short job, development machine:

| Scenario | SQL Server | PostgreSQL | Allocated |
|---|---|---|---|
| `SELECT *`, `BuildSql` | 225 ns | 235 ns | the string only (72 B / 64 B) |
| `SELECT *`, `WriteTo` | 188 ns | 216 ns | **0 B** |
| Join + filter + group + order + limit, `BuildSql` | 993 ns | 1,164 ns | the string only |
| Join + filter + group + order + limit, `WriteTo` | 933 ns | 1,208 ns | **0 B** |
| 100-row inline `INSERT` | 7.8 µs | 9.9 µs | the string only (~3.3 KB) |
| 100-row parameterized `INSERT` (300 parameters) | 14.3 µs | 17.3 µs | parameter array + set |
| Deeply nested predicate | 1.1 µs | 1.1 µs | the string only |

## Deviations kept from the design (recorded in the spec's *as built* notes)

1. **Runtime dialect switch.** The dialect is a `SqlDialectKind` switch rather than a static-abstract `TDialect`. This keeps one `SqlRoot` type and a trivial `SqlBuilder.For(kind)`.
2. **Writer carried by value.** `SqlWriter` moves from stage to stage by value rather than through a `ref` field. It has no `ref` / `Span` fields, so this is sound and avoids `scoped` gymnastics.
3. **Contracts in core.** Core Jakar.SqlBuilder owns the contracts instead of a separate bridge assembly (§9.8).
4. **Values chosen by overload.** Inline versus bound is picked by overload (`long` / `bool` inline, `object` bound, `SqlValue` explicit) rather than `param:` / `inline:` named arguments. `SqlValue` is a `readonly struct` so rows can be `params ReadOnlySpan<SqlValue>`.

## Gaps and limits

- **Build time (§11.1).** About 1 µs for the representative query, against "well under a microsecond". Profiling the per-call dialect switches and `Separate()` is the obvious next step.
- **Type names for SQLite, MySQL, Oracle and Firebird** come from one portable map (`DbColumnTypes.GetPortableType`, rendered by `SqlType`), so `CreateTable<T>()` works on all six dialects. PostgreSQL and SQL Server keep their own maps for their richer types.
- **Migrations use the builder.** Jakar.Database's `TableMetaData.CreateTableSql` / `DropTableSql` and `ColumnMetaData.CreateIndex` / `DropIndex` now call `CreateTable<T>()`, `DropTable` and `CreateIndex` / `DropIndex`, so table and index migrations are correct on all six dialects. The `last_modified` trigger migration (`SetLastModifiedFunction`, generated by `LastModifiedTrigger`) has each dialect's form too, and every engine checks that it overwrites the column on insert and update. `DatabaseType.SQLite` is re-enabled for this (appended last, so the other values keep their numbers). The hand-written DDL they replaced is frozen in `Jakar.Database.Tests/Integration/LegacyDdl/`, and the agreement tests require the new migrations to build the same catalog as that reference on PostgreSQL and SQL Server.
- **Defaults now:** `decimal` is `(38, 9)` on every dialect unless `[Precision]` says otherwise (it was `(28, 28)`, which only held values below 1). Unconstrained strings that are keys, unique or indexed are `VARCHAR(450)` / `nvarchar(450)`, because unbounded text can't be indexed. Guid keys get a server-side default where the engine has one (`gen_random_uuid()`, `NEWID()`, `(UUID())`, `SYS_GUID()`); SQLite and Firebird rely on the application's id.
- **SQL Server type map (fixed).** `DbColumnTypes.GetMicrosoftSqlServerDataType` used to return PostgreSQL type names. It now returns T-SQL types: unconstrained key, unique or indexed strings get `nvarchar(450)`, and PostgreSQL-only types become text. The migration writer now emits `IDENTITY(1,1)` / `NEWID()` on SQL Server and `DEFAULT` instead of `DEFAULTS`. `SqlServerCreateTableAgreementTests` creates all 11 tables both ways on SQL Server 2022 and requires identical catalogs.
- **Engine limits that are rejected rather than emulated:**
  - Firebird can't order a set operation by a column name: order by an expression or position.
  - Firebird has no per-column `DESC` in an index.
  - Oracle can't combine `FOR UPDATE` with `FETCH`, and has no standalone `WHEN MATCHED THEN DELETE`.
  - MySQL's `ON DUPLICATE KEY UPDATE` can't take a `WHERE`.
  - SQLite has no `DEFAULT` keyword in `VALUES` / `SET`.
