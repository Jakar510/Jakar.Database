// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

using System.Data;



namespace Jakar.SqlBuilder;


/// <summary> A single bound parameter: its dialect-rendered name (<c>$1</c> / <c>@p0</c> / <c>:p0</c>), CLR value, and optional explicit <see cref="System.Data.DbType"/>. </summary>
public readonly record struct SqlParameter( string Name, object? Value, DbType? DbType = null )
{
    public override string ToString() => $"{Name} = {Value ?? "NULL"}";
}
