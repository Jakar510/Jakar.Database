// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Tunable behavior for the fluent builder. Resolved from the host (e.g. Jakar.Database <c>DbOptions.SqlBuilder</c>). </summary>
public sealed record SqlBuilderOptions
{
    public const int DEFAULT_BUFFER_SIZE = 1024;

    /// <summary> Typed comparisons (<c>Where&lt;T&gt;(...)</c>) check the value against the column's CLR type, and <c>IS NULL</c> against its nullability (<see cref="SqlBuildError.TypeMismatch"/>). </summary>
    public bool StrictTypes { get; init; } = true;

    /// <summary>
    ///     Typed projections (<c>Select&lt;T&gt;</c>, <c>Column&lt;T&gt;</c>, typed aggregates) are written before <c>FROM</c>; they're qualified with the alias (or table name, once the
    ///     query joins tables) declared later, by patching the buffer. This is always on; the property is kept for configuration compatibility.
    /// </summary>
    public bool AliasProjections { get; init; } = true;

    public int  InitialBufferSize         { get; init; } = DEFAULT_BUFFER_SIZE;
    public bool AppendStatementTerminator { get; init; } = true;

    /// <summary> PostgreSQL parameters as <c>@p0</c> (Npgsql named binding, Dapper) instead of the positional <c>$1</c>. </summary>
    public bool NamedPostgresParameters { get; init; }

    /// <summary> <see cref="SqlLayout.Readable"/> (the default) lays each statement out for people to read (one clause per line, lists one item per line); <see cref="SqlLayout.Compact"/> writes one line. </summary>
    public SqlLayout Layout { get; init; } = SqlLayout.Readable;

    public static SqlBuilderOptions Default  { get; } = new();
    public static SqlBuilderOptions Readable { get; } = new() { Layout = SqlLayout.Readable };
    public static SqlBuilderOptions Compact  { get; } = new() { Layout = SqlLayout.Compact };
}
