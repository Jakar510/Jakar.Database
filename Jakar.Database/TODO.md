# Bug Review — TODO

Focused review of `Jakar.Database.Generators` (both files, fully) and the core of
`Jakar.Database` (`Api/`, `Models/RecordID.cs`, `Models/Service.cs`). This is **not
exhaustive** — 142 source files / ~17.8k lines were not all read. Items are listed
only, grouped by confidence. Several are flagged as "verify intent" rather than
confirmed defects.

---

## Jakar.Database (high confidence) — FIXED

- [x] **`Api/Mapping.cs` — `CompareTo` returned equal when keys differ. FIXED.**
  Changed `if ( ownerComparision == 0 )` to `!= 0` so `ValueID` is compared when keys match.

- [x] **`Api/Mapping.cs` — stray `@` before an inlined value. FIXED.**
  `WHERE s.{nameof(KeyID)} = @{key.Value}` → `= {key.Value}` (matches every sibling query).

- [x] **`Api/Mapping.cs` — `LEFT JOIN` with no `ON`. FIXED.**
  Added `ON s.{nameof(ValueID)} = v.{nameof(IUniqueID.ID)}`. (Join condition only — the
  surrounding WHERE logic was left as-is; review separately if the result set looks off.)

- [x] **`Models/Service.cs` — `async void ThreadStart()` + `__source` race. FIXED.**
  `ThreadStart` is now a synchronous body that blocks on the hosted service
  (`StartAsync(...).GetAwaiter().GetResult()`), so the dedicated thread stays alive and
  `Stop()`'s `Join()` actually waits. `__source` is created in `Start()` (linked to the
  external token) before `__thread.Start()`, and a `__started` flag guards `Start`/`Stop`
  ordering so a pre-start `Stop()` no longer touches a null source or `Join`s an unstarted
  thread.

## Jakar.Database.Generators (confirmed bug)

- [x] **`Import(NpgsqlBatchCommand batch, token)` — was an empty `=> default` stub. DONE.**
  `SyntaxNodeExtensions.cs` now generates a real body that adds one `NpgsqlParameter` per
  column (using `WriteExpression`, `DBNull.Value` for nulls, `.HasValue` for nullable value
  types). The caller sets `batch.CommandText`; generated parameter names match the property
  names (`@PropertyName`). Added `using System;` to the generated header for `DBNull`.
  Not yet compiled here (no .NET SDK in this environment) — build the solution to confirm.

- [x] **Build-time, size-packed column order. DONE (needs a build to verify).**
  `MetaData.SortedColumns` ordering is now computed at build time instead of by the runtime
  reflection sort. `SyntaxNodeExtensions.cs` ports `PostgresTypeComparer.GetSizeInfo` +
  `IsFixed`/`DbSize`/name and emits the order as
  `[GeneratedColumnOrder("…")]` on each generated partial type (new
  `MigrationApi/GeneratedColumnOrderAttribute.cs`). `TableMetaData.CreateIndexes` consumes it
  (`OrderedColumns`) and **falls back to `SortedProperties`** whenever the attribute is
  absent or doesn't cover every column — so the generated order is a pure optimization and
  can never change correctness. If any column type isn't confidently mappable, the generator
  emits no attribute and the runtime sorts as before.

- [x] **`Export` — implemented as a generated, allocation-free positional `Read<T>` factory. DONE (needs a build to verify).**
  Replaced the broken instance `ValueTask Export(NpgsqlBinaryExporter, token)` with a generated
  static factory `static ValueTask<TSelf> Export(NpgsqlBinaryExporter, token)` that reads each
  column positionally (in build-time column order) via `exporter.ReadAsync<T>` / `SkipAsync`
  and constructs the record through a generated private positional constructor chained to the
  framework base ctor (`TableRecord` / `LastModifiedRecord` / `PairRecord` / `OwnedTableRecord`
  / `Mapping`). Caller calls `StartRowAsync` per row.
  - Removed the instance `Export` from `ITableRecord`/`TableRecord` and the 6 hand-written
    `Export(...) => default` overrides (`Mapping`, `AddressRecord`, `FileRecord`, `RoleRecord`,
    `UserRecord`, `UserLoginProviderRecord`). `RecordPair` is a struct (not a `TableRecord`) so
    its standalone `Export` was left as-is.
  - **Safety valve:** the generator emits Export only when the base type and every column type
    are confidently handled (RecordID, Guid, string, bool, enum, the numeric/date types, JSON
    AdditionalData) and the record has no primary constructor. Anything else (e.g. `UserRights`,
    `byte[]`, a non-framework base, a primary-ctor record) gets **no** generated Export rather
    than wrong code — so this can't break a build. `UserRecord` (UserRights) is intentionally
    skipped and can be hand-written if needed.
  - Get-only computed columns are consumed from the stream (to stay aligned) but not assigned.
  - **Verify on build:** Npgsql exporter API (`ReadAsync<T>(token)`, `SkipAsync(token)`,
    `IsNull`) and that the generated nullable-reference annotations don't trip a
    warnings-as-errors build.

## Jakar.Database (lower confidence) — FIXED

- [x] **`Api/SqlInterpolatedStringHandler.cs` — `Sb.Length -= 3` off-by-one + empty-sequence corruption. FIXED.**
  Rewrote the three streaming/`ValueEnumerable` loops to append the `",\n"` separator
  *between* items (a `first` flag) instead of after every item + trailing `Sb.Length -= 3`.
  This removes the off-by-one (the separator is 2 chars) and the empty-sequence case can no
  longer trim characters off the preceding SQL. Matches the list-branch behavior.

- [x] **`Models/RecordID.cs` — `Create(Guid? id)` fabricated a new ID on null. FIXED (behavior change — review).**
  Now returns `Empty` on null instead of `New()`, and the moot `[NotNullIfNotNull]` was
  removed. **If any caller relied on null → fresh ID (an upsert-style pattern), this changes
  behavior** — revert that one line if so.

- [x] **`Models/RecordID.cs` — `TryFormat("b64")` ignored destination size. FIXED.**
  Returns `false` with `charsWritten = 0` when the base64 text won't fit, per the
  `ISpanFormattable` contract.

- [x] **`Api/CommandParameters.cs` — `[MustDisposeResource]` `Parameters` not disposed. FIXED.**
  `VariableNameLength` now materializes the buffer into a `using` local.

- [x] **`Api/CommandParameters.cs` — `Empty`/null-`Table` NPE. PARTIALLY FIXED.**
  `__Params` now uses `Table?.Sorter ?? Comparer<SqlParameter>.Default` so the `Empty`
  sentinel no longer NPEs. `default(CommandParameters)` (null backing lists) is still unsafe
  by design — always construct via the `Create<TSelf>` factories.

## Jakar.Database.Generators (lower confidence) — FIXED

- [x] **`SyntaxNodeExtensions.cs` — nullable `RecordID`/`UserRights` wrote the wrong value. FIXED.**
  Added `GetNullableValueExpression`: the binary-import **and** batch-import nullable branches
  now emit `X.Value.Value` (the Guid/long) for nullable `RecordID<T>?`/`UserRights?` instead
  of `X.Value` (the struct). Plain nullable value types are unchanged (`X.Value`).

- [x] **`SyntaxNodeExtensions.cs` — `IsUserRights` had no namespace check. FIXED.**
  Now requires the type to be in a `Jakar.*` namespace (a prefix check, since `UserRights`
  lives in `Jakar.Extensions`, not `Jakar.Database`), matching the spirit of `IsRecordId`.

- [x] **`SyntaxNodeExtensions.cs` — `Encoding.Default` for generated source. FIXED.**
  Now emits with `Encoding.UTF8`.

- [~] **`SyntaxNodeExtensions.cs` — `Collect()` + `HashSet` dedup. LEFT AS-IS (intentional).**
  This is **not** a defect: the `HashSet<GenerationCandidate>` (keyed by `HintName`) dedups
  the multiple candidate nodes a *partial* type produces across files — without it, two
  partial declarations of the same record would emit the same hint name twice and fail.
  Removing `Collect()` would require a different dedup strategy; the caching trade-off is
  deliberate. No change.
