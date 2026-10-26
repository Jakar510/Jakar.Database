// Jakar.Database :: Jakar.Database
// 03/04/2026  17:09

namespace Jakar.Database;


[UseWithCaution]
public readonly record struct SqlRaw( string Value )
{
    public readonly string Value = Value;
    public          bool   IsValid                { [Pure] [MemberNotNullWhen(true, nameof(Value))] get => !string.IsNullOrWhiteSpace(Value); }
    public          int    Length                 => Value.Length;
    public static   SqlRaw Create( string value ) => new(value);
    public override string ToString()             => Value;
}



[UseWithCaution]
public readonly record struct SqlName( string Value )
{
    public static readonly SqlName Empty = new(string.Empty);
    public readonly string Value = string.IsNullOrWhiteSpace(Value)
                                       ? string.Empty
                                       : Value.SqlName();
    public                          bool    IsValid                { [Pure] [MemberNotNullWhen(true, nameof(Value))] get => !string.IsNullOrWhiteSpace(Value); }
    public                          int     Length                 => Value.Length;
    public static                   SqlName Create( string value ) => new(value);
    public static implicit operator SqlName( string        value ) => new(value);
    public static implicit operator string( SqlName        value ) => value.Value;
    public override                 string ToString()              => Value;
}



[InterpolatedStringHandler]
public ref struct SqlInterpolatedStringHandler<TSelf>( int literalLength, int formattedCount )
    where TSelf : class, ITableRecord<TSelf>
{
    // Pooled: ToSqlCommand returns the buffer. A handler that is never finished just leaves its array to the GC.
    internal          ValueStringBuilder Sb         = new(Math.Max(literalLength * 2, 256));
    internal readonly CommandParameters  Parameters = CommandParameters.Create<TSelf>(formattedCount);
    private           string?            __lastColumnName;
    private           string?            __sql; // set once the handler is finished; ToString / ToSqlCommand replay it


    public void AppendNull()                                                                                                                         => AppendLiteral("NULL");
    public void AppendLiteral( ReadOnlySpan<char> value )                                                                                            => Sb.Append(value);
    public void AppendFormatted( StringBuilder?   value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY ) => AppendFragment(value);

    /// <summary> A raw SQL fragment built with a <see cref="ValueStringBuilder"/>, appended verbatim (hang-indented like <see cref="ColumnNames"/>). The caller still owns, and disposes, the builder. </summary>
    public void AppendFormatted( ValueStringBuilder value, string? format = null ) => AppendFragment(value.Values);


    public void AppendFormatted( ReadOnlySpan<string> value, string? format = null )
    {
        ParseFormat(ref format, out ushort indentLevel);
        AppendQuoted(value, indentLevel);
    }


    public void AppendFormatted<TOther>( RecordID<TOther> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TOther : class, IPairRecord<TOther>
    {
        ParseFormat(ref format, out ushort indentLevel);
        if ( !TryBindParameter(value, paramName) ) { AppendSingle(value.Value, format, true); }
    }

    public void AppendFormatted<TOther>( IEnumerable<RecordID<TOther>> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TOther : class, IPairRecord<TOther>
    {
        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, true);
    }

    public void AppendFormatted<TOther>( ReadOnlySpan<RecordID<TOther>> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TOther : class, IPairRecord<TOther>
    {
        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, true);
    }


    public void AppendFormatted( bool value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.Append(value ? bool.TrueString : bool.FalseString); }
    }
    public void AppendFormatted( bool? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( byte value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( byte? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( sbyte value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( sbyte? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( char value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { AppendSingle(value, format, true); }
    }
    public void AppendFormatted( char? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( short value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( short? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( ushort value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( ushort? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( int value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( int? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( uint value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( uint? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( long value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( long? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( ulong value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( ulong? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( float value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( float? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( double value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( double? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( decimal value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { Sb.AppendSpanFormattable(value, default, CultureInfo.InvariantCulture); }
    }
    public void AppendFormatted( decimal? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }


    public void AppendFormatted( Guid value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { AppendSingle(value, format, true); }
    }
    public void AppendFormatted( Guid? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( DateOnly value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { AppendSingle(value, format, true); }
    }
    public void AppendFormatted( DateOnly? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( DateTime value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { AppendSingle(value, format, true); }
    }
    public void AppendFormatted( DateTime? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( DateTimeOffset value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { AppendSingle(value, format, true); }
    }
    public void AppendFormatted( DateTimeOffset? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( TimeOnly value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { AppendSingle(value, format, true); }
    }
    public void AppendFormatted( TimeOnly? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }

    public void AppendFormatted( TimeSpan value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( !TryBindParameter(value, paramName) ) { AppendSingle(value, format, true); }
    }
    public void AppendFormatted( TimeSpan? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else if ( !TryBindParameter<object?>(null, paramName) ) { AppendNull(); }
    }


    public void AppendFormatted<TValue>( TValue value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        // A value type with its own overload takes it, so a generic caller (e.g. DbTable.Where<TValue>) writes the same SQL as a typed one: quoted, round-trip, bound after '@'.
        if ( typeof(TValue).IsValueType && TryAppendTyped(value, format, paramName) ) { return; }

        char lastChar = Sb.Length > 0
                            ? Sb[^1]
                            : '\0';

        bool isParameter = lastChar == '@';

        // Any value after '@' binds as a parameter of the last named column; strings decide below (a nameof(...) after '@' names the column), and SQL fragments are written as they are.
        if ( isParameter && value is not (string or SqlRaw or SqlName or ColumnNames or VariableNames or KeyValuePairs or CommandParameters or StringBuilder) )
        {
            BindParameter(value, paramName, lastChar);
            return;
        }

        ParseFormat(ref format, out ushort indentLevel);

        switch ( value )
        {
            case null:
            case DBNull:
                AppendNull();
                return;

            case string n:
            {
                bool isColumName = paramName.StartsWith("nameof(", StringComparison.Ordinal) || paramName == "columnName";

                if ( isColumName )
                {
                    // if ( __lastColumnName is not null ) { ThrowFormatException($"Missing @parameter for '{paramName}' with a value of '{value}'", isColumName, isParameter, lastChar); }

                    __lastColumnName = n;
                }

                switch ( n.Length )
                {
                    case > 0 when isColumName:
                        Sb.Append(n.SqlName());
                        return;

                    case > 0 when isParameter:
                        BindParameter(n, paramName, lastChar);
                        return;

                    case > 0:
                        AppendQuoted(n);
                        return;

                    default:
                        AppendQuoted(EMPTY);
                        return;
                }
            }

            case SqlRaw n:
                Sb.Append(n.Value);
                return;

            case SqlName n:
                Sb.Append(n.Value);
                return;

            case ColumnNames n:
                AppendFragment(n.Value);
                return;

            case VariableNames n:
                Parameters.AddRange(n.Values);
                AppendFragment(n.Value);
                return;

            case KeyValuePairs n:
                Parameters.AddRange(n.Values);
                AppendFragment(n.Value);
                return;

            case CommandParameters:
                throw new InvalidOperationException($"""
                                                     Not supported by design. Use any of the following methods instead: 
                                                        • {nameof(CommandParameters)}.{nameof(CommandParameters.VariableNames)}
                                                        • {nameof(CommandParameters)}.{nameof(CommandParameters.KeyValuePairs)}
                                                        • {nameof(CommandParameters)}.{nameof(CommandParameters.ColumnNames)}
                                                     """);

            case Enum n:
                // ReSharper disable once RedundantToStringCall
                if ( string.Equals(format, "str", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "string", StringComparison.OrdinalIgnoreCase) ) { AppendQuoted(n.ToString()); }
                else { Sb.AppendSpanFormattable(Convert.ToInt64(n), default, CultureInfo.InvariantCulture); }

                return;

            case Type n:
                AppendQuoted(n.Name);
                return;

            case StringBuilder n:
                AppendFragment(n);
                return;

            case IEnumerable<string> n:
                AppendQuoted(n, indentLevel);
                return;

            case IEnumerable<Int128> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<UInt128> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<byte> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<sbyte> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<short> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<int> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<long> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<uint> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<ushort> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<ulong> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<float> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<double> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<decimal> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<char> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<IRecordID> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<Guid> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<DateTime> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<DateTimeOffset> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<DateOnly> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<TimeSpan> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<TimeOnly> n:
                AppendMany(n, format, indentLevel, true);
                return;


            case ISpanFormattable x:
                Sb.AppendSpanFormattable(x, format, CultureInfo.InvariantCulture);
                return;

            case IFormattable x:
                Sb.Append(x.ToString(format, CultureInfo.InvariantCulture));
                return;

            default:
                // ReSharper disable once RedundantToStringCallForValueType
                Sb.Append(value.ToString());
                return;
        }
    }


    /// <summary> Binds <paramref name="value"/> as a parameter when the text so far ends with <c>@</c>; returns whether it did. </summary>
    private bool TryBindParameter<TValue>( TValue value, string paramName )
    {
        if ( Sb.Length is 0 || Sb[^1] != '@' ) { return false; }

        BindParameter(value, paramName, '@');
        return true;
    }

    /// <summary> <c>@name</c> for the value, bound to the column named just before it (<c>{nameof(Record.Column)} = @{value}</c>). </summary>
    private void BindParameter<TValue>( TValue value, string paramName, char lastChar )
    {
        string parameterName = paramName.Parameterize();
        Sb.Append(parameterName);

        if ( __lastColumnName is null ) { ThrowFormatException($"Missing parameter (column_name) for '{paramName}' with a value of '{value}'", false, true, lastChar, parameterName); }

        Parameters.Add(__lastColumnName, value, parameterName);
        __lastColumnName = null;
    }

    /// <summary> Routes a value type that has its own overload there. Each <see langword="typeof"/> test is a JIT-time constant, so for a value type this compiles to one call or nothing. </summary>
    private bool TryAppendTyped<TValue>( TValue value, string? format, string paramName )
    {
        if ( typeof(TValue) == typeof(bool) ) { AppendFormatted(Unsafe.As<TValue, bool>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(byte) ) { AppendFormatted(Unsafe.As<TValue, byte>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(sbyte) ) { AppendFormatted(Unsafe.As<TValue, sbyte>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(char) ) { AppendFormatted(Unsafe.As<TValue, char>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(short) ) { AppendFormatted(Unsafe.As<TValue, short>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(ushort) ) { AppendFormatted(Unsafe.As<TValue, ushort>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(int) ) { AppendFormatted(Unsafe.As<TValue, int>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(uint) ) { AppendFormatted(Unsafe.As<TValue, uint>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(long) ) { AppendFormatted(Unsafe.As<TValue, long>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(ulong) ) { AppendFormatted(Unsafe.As<TValue, ulong>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(float) ) { AppendFormatted(Unsafe.As<TValue, float>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(double) ) { AppendFormatted(Unsafe.As<TValue, double>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(decimal) ) { AppendFormatted(Unsafe.As<TValue, decimal>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(Guid) ) { AppendFormatted(Unsafe.As<TValue, Guid>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(DateOnly) ) { AppendFormatted(Unsafe.As<TValue, DateOnly>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(DateTime) ) { AppendFormatted(Unsafe.As<TValue, DateTime>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(DateTimeOffset) ) { AppendFormatted(Unsafe.As<TValue, DateTimeOffset>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(TimeOnly) ) { AppendFormatted(Unsafe.As<TValue, TimeOnly>(ref value), format, paramName); return true; }
        if ( typeof(TValue) == typeof(TimeSpan) ) { AppendFormatted(Unsafe.As<TValue, TimeSpan>(ref value), format, paramName); return true; }

        return false;
    }


    public void AppendFormatted<TValue>( ReadOnlySpan<TValue> value, string? format = null )
        where TValue : ISpanFormattable
    {
        bool needsQuotes = NeedsQuotes<TValue>();
        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, in needsQuotes);
    }


    public void AppendFormatted<TEnumerator, TValue>( ValueEnumerable<TEnumerator, TValue> value, string? format = null )
        where TEnumerator : struct, IValueEnumerator<TValue>, allows ref struct
        where TValue : ISpanFormattable
    {
        bool needsQuotes = NeedsQuotes<TValue>();
        ParseFormat(ref format, out ushort indentLevel);

        using ValueEnumerator<TEnumerator, TValue> enumerator = value.GetEnumerator();

        bool first = true;

        while ( enumerator.MoveNext() )
        {
            if ( !first ) { Sb.Append(",\n"); }

            first = false;
            Sb.Spacer(indentLevel);
            AppendSingle(enumerator.Current, format, needsQuotes);
        }
    }


    /// <summary> Formats <paramref name="value"/> straight into the buffer (culture-invariant), quoted when <paramref name="needsQuotes"/>; dates and times default to the round-trip form. </summary>
    private void AppendSingle<TValue>( TValue value, string? format, bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        // Round-trip form: culture-invariant, and TimeOnly keeps its seconds and fractions (the invariant default is HH:mm). JIT-time constants for a value type.
        if ( typeof(TValue) == typeof(DateTime) || typeof(TValue) == typeof(DateTimeOffset) || typeof(TValue) == typeof(DateOnly) || typeof(TValue) == typeof(TimeOnly) ) { format ??= "o"; }

        if ( needsQuotes ) { Sb.Append('\''); }

        Sb.AppendSpanFormattable(value, format, CultureInfo.InvariantCulture);
        if ( needsQuotes ) { Sb.Append('\''); }
    }


    private void AppendMany<TValue>( IEnumerable<TValue> enumerable, string? format, ushort indentLevel, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        switch ( enumerable )
        {
            case IReadOnlyList<TValue> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    Sb.Spacer(indentLevel);
                    AppendSingle(list[i], format, needsQuotes);
                    if ( i < list.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            case IList<TValue> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    Sb.Spacer(indentLevel);
                    AppendSingle(list[i], format, needsQuotes);
                    if ( i < list.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            default:
            {
                using IEnumerator<TValue> enumerator = enumerable.GetEnumerator();

                bool first = true;

                while ( enumerator.MoveNext() )
                {
                    if ( !first ) { Sb.Append(",\n"); }

                    first = false;
                    Sb.Spacer(indentLevel);
                    AppendSingle(enumerator.Current, format, needsQuotes);
                }

                return;
            }
        }
    }
    private void AppendMany<TValue>( ReadOnlySpan<TValue> array, string? format, ushort indentLevel, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        for ( int i = 0; i < array.Length; i++ )
        {
            Sb.Spacer(indentLevel);
            AppendSingle(array[i], format, needsQuotes);
            if ( i < array.Length - 1 ) { Sb.Append(",\n"); }
        }
    }


    /// <summary>
    ///     Appends a pre-formatted, possibly multi-line fragment (<see cref="ColumnNames"/> , <see cref="VariableNames"/> , <see cref="KeyValuePairs"/>) so that every continuation line is
    ///     hang-indented to the column at which the fragment starts. This keeps the block aligned regardless of where the interpolation placeholder sits within the surrounding statement.
    /// </summary>
    private void AppendFragment( StringBuilder? fragment )
    {
        if ( fragment is null || fragment.Length is 0 ) { return; }

        int hang = CurrentLineIndent();
        foreach ( ReadOnlyMemory<char> chunk in fragment.GetChunks() ) { AppendHung(chunk.Span, hang); }
    }
    private void AppendFragment( scoped ReadOnlySpan<char> fragment )
    {
        if ( fragment.IsEmpty ) { return; }

        AppendHung(fragment, CurrentLineIndent());
    }
    private void AppendHung( scoped ReadOnlySpan<char> fragment, int hang )
    {
        if ( hang is 0 )
        {
            Sb.Append(fragment);
            return;
        }

        // Each line in one copy, then the hang indent after its newline.
        int index;
        while ( ( index = fragment.IndexOf('\n') ) >= 0 )
        {
            Sb.Append(fragment[..( index + 1 )]).Append(' ', hang);
            fragment = fragment[( index + 1 )..];
        }

        Sb.Append(fragment);
    }
    /// <summary> Number of characters written on the current line since the last newline (the column at which the next character would be appended). </summary>
    private readonly int CurrentLineIndent() => Sb.Length - 1 - Sb.Values.LastIndexOf('\n');


    /// <summary> <c>'value'</c>, with each <c>'</c> inside doubled, so inline text can't end the literal early. </summary>
    private void AppendQuoted( scoped ReadOnlySpan<char> value )
    {
        Sb.Append('\'');

        int index;
        while ( ( index = value.IndexOf('\'') ) >= 0 )
        {
            Sb.Append(value[..( index + 1 )]).Append('\'');
            value = value[( index + 1 )..];
        }

        Sb.Append(value).Append('\'');
    }
    private void AppendQuoted( scoped ReadOnlySpan<char> value, ushort indentLevel )
    {
        Sb.Spacer(indentLevel);
        AppendQuoted(value);
    }


    private void AppendQuoted( IEnumerable<string> enumerable, ushort indentLevel )
    {
        switch ( enumerable )
        {
            case IReadOnlyList<string> array:
            {
                for ( int i = 0; i < array.Count; i++ )
                {
                    AppendQuoted(array[i], indentLevel);
                    if ( i < array.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            case IList<string> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    AppendQuoted(list[i], indentLevel);
                    if ( i < list.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            default:
            {
                using IEnumerator<string> enumerator = enumerable.GetEnumerator();

                bool first = true;

                while ( enumerator.MoveNext() )
                {
                    if ( !first ) { Sb.Append(",\n"); }

                    first = false;
                    AppendQuoted(enumerator.Current, indentLevel);
                }

                return;
            }
        }
    }
    private void AppendQuoted( ReadOnlySpan<string> array, ushort indentLevel )
    {
        for ( int i = 0; i < array.Length; i++ )
        {
            AppendQuoted(array[i], indentLevel);
            if ( i < array.Length - 1 ) { Sb.Append(",\n"); }
        }
    }


    public override string ToString() => __sql ?? Clean(Sb.Values);


    /// <summary>
    ///     Normalizes generated SQL for emission: trailing whitespace is stripped from every line, consecutive blank lines are collapsed to a single blank line, and leading/trailing blank lines are removed.
    ///     Indentation and alignment produced by <see cref="AppendFragment"/> are preserved.
    /// </summary>
    public static string Clean( string sql ) => string.IsNullOrEmpty(sql)
                                                    ? sql
                                                    : Clean(sql.AsSpan());

    /// <inheritdoc cref="Clean(string)"/>
    public static string Clean( ReadOnlySpan<char> sql )
    {
        if ( sql.IsEmpty ) { return string.Empty; }

        // Two passes over the lines: measure, then write straight into the result, so nothing is copied twice.
        int length = Clean(sql, Span<char>.Empty);
        // Cleaning only drops characters, keeping the rest in order, so an unchanged length means unchanged text.
        if ( length == sql.Length ) { return sql.ToString(); }

        return length is 0
                   ? string.Empty
                   : string.Create(length, sql, static ( span, source ) => Clean(source, span));
    }

    /// <summary> Writes the cleaned form of <paramref name="sql"/> into <paramref name="destination"/> (when it isn't empty) and returns its length. </summary>
    private static int Clean( ReadOnlySpan<char> sql, Span<char> destination )
    {
        bool write   = !destination.IsEmpty;
        int  written = 0;
        bool hasBody = false;
        bool pending = false;

        while ( true )
        {
            int                newLine = sql.IndexOf('\n');
            ReadOnlySpan<char> line    = newLine < 0 ? sql : sql[..newLine];
            ReadOnlySpan<char> trimmed = line.TrimEnd();

            if ( trimmed.IsEmpty )
            {
                if ( hasBody ) { pending = true; }
            }
            else
            {
                if ( hasBody ) { written = Put(destination, written, '\n', write); }

                if ( pending )
                {
                    written = Put(destination, written, '\n', write);
                    pending = false;
                }

                if ( write ) { trimmed.CopyTo(destination[written..]); }

                written += trimmed.Length;
                hasBody =  true;
            }

            if ( newLine < 0 ) { break; }

            sql = sql[( newLine + 1 )..];
        }

        return written;

        static int Put( Span<char> destination, int index, char c, bool write )
        {
            if ( write ) { destination[index] = c; }

            return index + 1;
        }
    }
    [DoesNotReturn] [MethodImpl(MethodImplOptions.AggressiveInlining)] private void ThrowFormatException( string message, bool isColumName, bool isParameter, char lastChar, string? parameterName = null )
    {
        throw new FormatException($"""
                                   {message}

                                   {nameof(isColumName)}: '{isColumName}'
                                   {nameof(isParameter)}: '{isParameter}'
                                   {nameof(lastChar)}: '{lastChar}'
                                   {nameof(parameterName)}: '{parameterName}'
                                   {nameof(__lastColumnName)}: '{__lastColumnName}'
                                   Current Value of handler: '{ToString()}'
                                   """);
    }


    public (string SQL, ImmutableArray<CommandParameter> Parameters) Build() => new(ToString(), [..Parameters.Values]);


    /// <summary> The finished command. The first call returns the pooled buffer; later calls (and <see cref="ToString"/>) replay the same SQL. </summary>
    public SqlCommand ToSqlCommand( in CommandType commandType = CommandType.Text )
    {
        if ( __sql is null )
        {
            __sql = Clean(Sb.Values);
            Sb.Dispose();
        }

        return SqlCommand.Create(__sql, Parameters, in commandType);
    }


    public static string SanitizeParameterName( ReadOnlySpan<char> name )
    {
        if ( name.IsNullOrWhiteSpace() ) { return "p"; }

        // Keep only the last segment after '.'
        int lastDot = name.LastIndexOf('.');
        if ( lastDot >= 0 && lastDot < name.Length - 1 ) { name = name[( lastDot + 1 )..]; }

        if ( name.IsNullOrWhiteSpace() ) { return "p"; }

        Span<char> buffer = stackalloc char[name.Length];
        int        index  = 0;

        foreach ( char c in name )
        {
            buffer[index++] = char.IsLetterOrDigit(c) || c == '_'
                                  ? c
                                  : '_';
        }

        return buffer[..index].ToString();
    }
    public static void ParseFormat( ref string? format, out ushort indentLevel )
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        if ( ushort.TryParse(span, out indentLevel) )
        {
            // "2|D3": indent level 2, format "D3"; "2": indent only.
            format = index >= 0 && index + 1 < format!.Length
                         ? format[( index + 1 )..]
                         : null;
        }
    }
    public static bool NeedsQuotes<TValue>() => QuotesFor<TValue>.VALUE;



    /// <summary> Whether a <typeparamref name="TValue"/> is written quoted, computed once per type (the check uses reflection). </summary>
    private static class QuotesFor<TValue>
    {
        public static readonly bool VALUE = typeof(TValue).HasInterface<IRecordID>()  ||
                                            typeof(TValue) == typeof(string)          ||
                                            typeof(TValue) == typeof(char)            ||
                                            typeof(TValue) == typeof(Guid)            ||
                                            typeof(TValue) == typeof(DateTime)        ||
                                            typeof(TValue) == typeof(DateTimeOffset)  ||
                                            typeof(TValue) == typeof(DateOnly)        ||
                                            typeof(TValue) == typeof(TimeSpan)        ||
                                            typeof(TValue) == typeof(TimeOnly);
    }
}
