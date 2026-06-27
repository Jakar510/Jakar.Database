// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Tunable behavior for the fluent builder. Resolved from the host (e.g. Jakar.Database <c>DbOptions.SqlBuilder</c>). </summary>
public sealed record SqlBuilderOptions
{
    public const int DEFAULT_BUFFER_SIZE = 1024;

    public bool StrictTypes               { get; init; }
    public bool AliasProjections          { get; init; }
    public int  InitialBufferSize         { get; init; } = DEFAULT_BUFFER_SIZE;
    public bool AppendStatementTerminator { get; init; } = true;

    public static SqlBuilderOptions Default { get; } = new();
}
