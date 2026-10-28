// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

using System.Globalization;
using System.Runtime.CompilerServices;



namespace Jakar.SqlBuilder;


internal enum SqlValueKind
{
    Bound,
    InlineNumber,
    InlineBoolean,
    InlineString,
    Null,
    Raw,
    InlineTyped,
    Default
}



/// <summary>
///     A value to emit: a bound parameter, an escaped inline literal, <c>NULL</c>, <c>DEFAULT</c>, or a trusted raw fragment.
///     A normal <see langword="readonly"/> <see langword="struct"/> (not a ref struct) so it can be passed as
///     <c>params ReadOnlySpan&lt;SqlValue&gt;</c> for multi-column INSERT rows.
///     <para> Inline literals are culture-invariant and written in each dialect's literal syntax (e.g. a <see cref="System.Guid"/> is <c>HEXTORAW('...')</c> on Oracle, <c>byte[]</c> is <c>0x...</c> on SQL Server). </para>
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

    internal bool    IsBound => __kind is SqlValueKind.Bound;
    internal object? Bound   => __bound;

    /// <summary> The value for <see cref="SqlBuilderOptions.StrictTypes"/> checks; <see langword="null"/> for raw fragments and <c>DEFAULT</c>. </summary>
    internal object? ComparableValue => __kind switch
                                        {
                                            SqlValueKind.Bound or SqlValueKind.InlineTyped => __bound,
                                            SqlValueKind.InlineNumber                      => __number,
                                            SqlValueKind.InlineBoolean                     => __boolean,
                                            SqlValueKind.InlineString                      => __text,
                                            _                                              => null
                                        };

    public static SqlValue Param( object? value ) => value is null
                                                         ? Null
                                                         : new SqlValue(SqlValueKind.Bound, bound: value);

    public static SqlValue Inline( string             value ) => new(SqlValueKind.InlineString, value ?? throw new ArgumentNullException(nameof(value)));
    public static SqlValue Inline( ReadOnlySpan<char> value ) => new(SqlValueKind.InlineString, value.ToString());
    public static SqlValue Inline( char               value ) => new(SqlValueKind.InlineString, value.ToString());
    public static SqlValue Inline( Guid               value ) => new(SqlValueKind.InlineTyped, bound: value);
    public static SqlValue Inline( DateTime           value ) => new(SqlValueKind.InlineTyped, bound: value);
    public static SqlValue Inline( DateTimeOffset     value ) => new(SqlValueKind.InlineTyped, bound: value);
    public static SqlValue Inline( DateOnly           value ) => new(SqlValueKind.InlineTyped, bound: value);
    public static SqlValue Inline( TimeOnly           value ) => new(SqlValueKind.InlineTyped, bound: value);
    public static SqlValue Inline( byte[]             value ) => new(SqlValueKind.InlineTyped, bound: value ?? throw new ArgumentNullException(nameof(value)));

    public static SqlValue Of( long    value ) => new(SqlValueKind.InlineNumber, number: value);
    public static SqlValue Of( bool    value ) => new(SqlValueKind.InlineBoolean, boolean: value);
    public static SqlValue Of( decimal value ) => new(SqlValueKind.InlineTyped, bound: value);

    /// <exception cref="ArgumentException"> <paramref name="value"/> is NaN or infinite: SQL has no literal for it. </exception>
    public static SqlValue Of( double value ) => double.IsFinite(value)
                                                     ? new SqlValue(SqlValueKind.InlineTyped, bound: value)
                                                     : throw new ArgumentException("NaN and infinities have no SQL literal; bind them as a parameter.", nameof(value));

    public static SqlValue Null => new(SqlValueKind.Null);

    /// <summary> The <c>DEFAULT</c> keyword: the column's default, in <c>INSERT ... VALUES</c> and <c>UPDATE ... SET</c>. </summary>
    public static SqlValue Default => new(SqlValueKind.Default);

    public static SqlValue Raw( string             sql ) => new(SqlValueKind.Raw, sql ?? throw new ArgumentNullException(nameof(sql)));
    public static SqlValue Raw( ReadOnlySpan<char> sql ) => new(SqlValueKind.Raw, sql.ToString());

    internal readonly void Write( scoped ref SqlWriter writer )
    {
        switch ( __kind )
        {
            case SqlValueKind.Bound:
                writer.Parameter(__bound);
                return;

            case SqlValueKind.InlineString:
                writer.InlineString(__text ?? string.Empty);
                return;

            case SqlValueKind.InlineNumber:
                writer.InlineNumber(__number);
                return;

            case SqlValueKind.InlineBoolean:
                writer.InlineBoolean(__boolean);
                return;

            case SqlValueKind.Null:
                writer.InlineNull();
                return;

            case SqlValueKind.Raw:
                writer.InlineRaw(__text ?? string.Empty);
                return;

            case SqlValueKind.InlineTyped:
                SqlLiterals.Write(ref writer, __bound!);
                return;

            case SqlValueKind.Default:
                writer.Require(writer.Dialect is not SqlDialectKind.Sqlite, "DEFAULT in VALUES / SET");
                writer.Word("DEFAULT");
                return;

            default: throw new ArgumentOutOfRangeException(nameof(SqlValue), __kind, "Unknown value kind.");
        }
    }
}



/// <summary> Per-dialect literal syntax for the typed inline values. </summary>
internal static class SqlLiterals
{
    public static void Write<T>( scoped ref SqlWriter writer, T value )
    {
        switch ( value )
        {
            case decimal number:
                writer.Word(number.ToString(CultureInfo.InvariantCulture));
                return;

            case double number:
                writer.Word(number.ToString("R", CultureInfo.InvariantCulture));
                return;

            case Guid guid:
                Write(ref writer, guid);
                return;

            case DateTime time:
                Write(ref writer, time);
                return;

            case DateTimeOffset time:
                Write(ref writer, time);
                return;

            case DateOnly date:
                Write(ref writer, date);
                return;

            case TimeOnly time:
                Write(ref writer, time);
                return;

            case byte[] bytes:
                string hex = Convert.ToHexString(bytes);

                writer.Word(writer.Dialect switch
                            {
                                SqlDialectKind.PostgreSql => $"decode('{hex}', 'hex')",
                                SqlDialectKind.SqlServer  => $"0x{hex}",
                                SqlDialectKind.Oracle     => $"HEXTORAW('{hex}')",
                                _                         => $"X'{hex}'"
                            });

                return;

            default: throw writer.Error(SqlBuildError.InvalidLiteral, $"{value.GetType().Name} has no inline literal; bind it as a parameter.");
        }
    }

    /// <summary>
    ///     One value of an <c>IN</c> list. Numbers, <see cref="Guid"/>s and dates can't carry SQL, so they're written inline in the dialect's literal syntax; anything else (a
    ///     <see cref="char"/>, which is text, or a type with no SQL literal such as <see cref="TimeSpan"/>, an enum or a record id) binds as a parameter. Each <see langword="typeof"/> test
    ///     is a JIT-time constant, so for a value type this compiles to the one branch it takes, with no boxing.
    /// </summary>
    public static void WriteValue<T>( scoped ref SqlWriter writer, T value )
        where T : ISpanFormattable
    {
        if ( typeof(T)      == typeof(Guid) ) { Write(ref writer,           Unsafe.As<T, Guid>(ref value)); }
        else if ( typeof(T) == typeof(DateTime) ) { Write(ref writer,       Unsafe.As<T, DateTime>(ref value)); }
        else if ( typeof(T) == typeof(DateTimeOffset) ) { Write(ref writer, Unsafe.As<T, DateTimeOffset>(ref value)); }
        else if ( typeof(T) == typeof(DateOnly) ) { Write(ref writer,       Unsafe.As<T, DateOnly>(ref value)); }
        else if ( typeof(T) == typeof(TimeOnly) ) { Write(ref writer,       Unsafe.As<T, TimeOnly>(ref value)); }
        else if ( typeof(T) == typeof(double) )
        {
            // NaN and infinities have no SQL literal.
            if ( double.IsFinite(Unsafe.As<T, double>(ref value)) ) { Number(ref writer, value, "R"); }
            else { writer.Parameter(value); }
        }
        else if ( typeof(T) == typeof(float) )
        {
            if ( float.IsFinite(Unsafe.As<T, float>(ref value)) ) { Number(ref writer, value, "R"); }
            else { writer.Parameter(value); }
        }
        else if ( typeof(T) == typeof(byte)    ||
                  typeof(T) == typeof(sbyte)   ||
                  typeof(T) == typeof(short)   ||
                  typeof(T) == typeof(ushort)  ||
                  typeof(T) == typeof(int)     ||
                  typeof(T) == typeof(uint)    ||
                  typeof(T) == typeof(long)    ||
                  typeof(T) == typeof(ulong)   ||
                  typeof(T) == typeof(nint)    ||
                  typeof(T) == typeof(nuint)   ||
                  typeof(T) == typeof(Int128)  ||
                  typeof(T) == typeof(UInt128) ||
                  typeof(T) == typeof(decimal) ) { Number(ref writer, value, default); }
        else { writer.Parameter(value); }
    }

    /// <summary> A number, formatted culture-invariantly straight into the statement. </summary>
    private static void Number<T>( scoped ref SqlWriter writer, T value, scoped ReadOnlySpan<char> format )
        where T : ISpanFormattable
    {
        Span<char> buffer = stackalloc char[64];

        if ( value.TryFormat(buffer, out int written, format, CultureInfo.InvariantCulture) ) { writer.Word(buffer[..written]); }
        else { writer.Word(value.ToString(format.ToString(), CultureInfo.InvariantCulture)); }
    }

    private static void Write( scoped ref SqlWriter writer, Guid guid )
    {
        switch ( writer.Dialect )
        {
            case SqlDialectKind.Oracle:
                writer.Word($"HEXTORAW('{Convert.ToHexString(guid.ToByteArray())}')");
                return;

            case SqlDialectKind.Firebird:
                writer.Word($"x'{Convert.ToHexString(guid.ToByteArray())}'");
                return;

            default:
                writer.InlineString(guid.ToString("D"));
                return;
        }
    }

    private static void Write( scoped ref SqlWriter writer, DateTime time )
    {
        SqlDialectKind dialect = writer.Dialect;

        Quoted(ref writer,
               dialect switch
               {
                   SqlDialectKind.SqlServer => time.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture),
                   SqlDialectKind.Sqlite    => time.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
                   SqlDialectKind.Firebird  => time.ToString("yyyy-MM-dd HH:mm:ss.ffff",    CultureInfo.InvariantCulture),
                   _                        => time.ToString("yyyy-MM-dd HH:mm:ss.ffffff",  CultureInfo.InvariantCulture)
               },
               dialect is SqlDialectKind.Oracle or SqlDialectKind.Firebird
                   ? "TIMESTAMP"
                   : null);
    }

    private static void Write( scoped ref SqlWriter writer, DateTimeOffset time )
    {
        SqlDialectKind dialect = writer.Dialect;

        if ( dialect is SqlDialectKind.MySql )
        {
            // MySQL has no offset type; its DATETIME holds the UTC value (as MySqlConnector binds it).
            Quoted(ref writer, time.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture), null);
            return;
        }

        Quoted(ref writer,
               dialect switch
               {
                   SqlDialectKind.SqlServer => time.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture),
                   SqlDialectKind.Sqlite    => time.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture),
                   SqlDialectKind.Oracle    => time.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz", CultureInfo.InvariantCulture),
                   SqlDialectKind.Firebird  => time.ToString("yyyy-MM-dd HH:mm:ss.ffff zzz",   CultureInfo.InvariantCulture),
                   _                        => time.ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz",  CultureInfo.InvariantCulture)
               },
               dialect is SqlDialectKind.Oracle or SqlDialectKind.Firebird
                   ? "TIMESTAMP"
                   : null);
    }

    private static void Write( scoped ref SqlWriter writer, DateOnly date ) => Quoted(ref writer,
                                                                                      date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                                                                      writer.Dialect is SqlDialectKind.SqlServer or SqlDialectKind.Sqlite
                                                                                          ? null
                                                                                          : "DATE");

    private static void Write( scoped ref SqlWriter writer, TimeOnly time )
    {
        SqlDialectKind dialect = writer.Dialect;
        writer.Require(dialect is not SqlDialectKind.Oracle, "A TIME literal (Oracle has no time-of-day type)");

        Quoted(ref writer,
               time.ToString(dialect is SqlDialectKind.Firebird
                                 ? "HH:mm:ss.ffff"
                                 : "HH:mm:ss.fffffff",
                             CultureInfo.InvariantCulture),
               dialect is SqlDialectKind.SqlServer or SqlDialectKind.Sqlite
                   ? null
                   : "TIME");
    }

    private static void Quoted( scoped ref SqlWriter writer, string text, string? keyword )
    {
        if ( keyword is not null ) { writer.Word(keyword); }

        writer.InlineString(text);
    }
}
