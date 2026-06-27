// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> A single bound parameter: its dialect-rendered name (<c>$1</c> / <c>@p0</c>) and CLR value. </summary>
public readonly record struct SqlParameter( string Name, object? Value )
{
    public override string ToString() => $"{Name} = {Value ?? "NULL"}";
}
