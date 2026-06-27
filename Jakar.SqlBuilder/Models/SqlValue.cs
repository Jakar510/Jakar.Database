// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


internal enum SqlValueKind
{
    Bound,
    InlineNumber,
    InlineBoolean,
    InlineString,
    Null,
    Raw
}



/// <summary>
///     A value to emit: a bound parameter, an escaped inline literal, <c>NULL</c>, or a trusted raw fragment.
///     A normal <see langword="readonly"/> <see langword="struct"/> (not a ref struct) so it can be passed as
///     <c>params ReadOnlySpan&lt;SqlValue&gt;</c> for multi-column INSERT rows.
/// </summary>
public readonly struct SqlValue
{
    private readonly string?      __text;
    private readonly object?      __bound;
    private readonly long         __number;
    private readonly bool         __boolean;
    private readonly SqlValueKind __kind;

    private SqlValue( SqlValueKind kind, string? text = null, object? bound = null, long number = 0, bool boolean = false )
    {
        __kind    = kind;
        __text    = text;
        __bound   = bound;
        __number  = number;
        __boolean = boolean;
    }

    public static SqlValue Param( object? value ) => value is null ? Null : new SqlValue(SqlValueKind.Bound, bound: value);

    public static SqlValue Inline( string             value ) => new(SqlValueKind.InlineString, value);
    public static SqlValue Inline( ReadOnlySpan<char> value ) => new(SqlValueKind.InlineString, value.ToString());

    public static SqlValue Of( long value ) => new(SqlValueKind.InlineNumber, number: value);
    public static SqlValue Of( bool value ) => new(SqlValueKind.InlineBoolean, boolean: value);

    public static SqlValue Null => new(SqlValueKind.Null);

    public static SqlValue Raw( string             sql ) => new(SqlValueKind.Raw, sql);
    public static SqlValue Raw( ReadOnlySpan<char> sql ) => new(SqlValueKind.Raw, sql.ToString());

    internal readonly void Write( scoped ref SqlWriter writer )
    {
        switch ( __kind )
        {
            case SqlValueKind.Bound:         writer.Parameter(__bound);                  return;
            case SqlValueKind.InlineString:  writer.InlineString(__text ?? string.Empty); return;
            case SqlValueKind.InlineNumber:  writer.InlineNumber(__number);              return;
            case SqlValueKind.InlineBoolean: writer.InlineBoolean(__boolean);            return;
            case SqlValueKind.Null:          writer.InlineNull();                        return;
            case SqlValueKind.Raw:           writer.InlineRaw(__text ?? string.Empty);   return;
            default:                         throw new ArgumentOutOfRangeException(nameof(SqlValue), __kind, "Unknown value kind.");
        }
    }
}
