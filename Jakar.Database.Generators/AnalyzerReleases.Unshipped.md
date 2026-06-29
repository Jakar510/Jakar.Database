; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
JDB001 | Jakar.Database.Generators | Error | ITableRecord&lt;TSelf&gt; implementations must be sealed or abstract
JDB002 | Jakar.Database.Generators | Warning | StringCompare is only valid on string properties
