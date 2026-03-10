// Jakar.Database :: Jakar.Database
// 03/04/2026  17:09

namespace Jakar.Database;


[UseWithCaution]
public readonly record struct SqlRaw( string Value )
{
    public readonly string Value = Value;
    public static   SqlRaw Create( string value ) => new(value);
}



[InterpolatedStringHandler]
public readonly ref struct SqlInterpolatedStringHandler<TSelf>( int literalLength, int formattedCount )
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    internal readonly StringBuilder      Sb          = new(literalLength);
    internal readonly PostgresParameters Parameters  = PostgresParameters.Create<TSelf>(formattedCount);
    internal readonly Stack<string>      ColumnNames = new(formattedCount);


    public void AppendLiteral( ReadOnlySpan<char> value )                                                                                            => Sb.Append(value);
    public void AppendFormatted( StringBuilder?   value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY ) => Sb.Append(value);


    public void AppendFormatted( string? value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
    {
        bool isParameter = Sb.Length > 0 && Sb[^1] == '@';
        bool isNameOf    = paramName.StartsWith("nameof(", StringComparison.Ordinal);
        if ( isNameOf && value is not null ) { ColumnNames.Push(value); }

        try
        {
            switch ( value?.Length )
            {
                case null:
                    Sb.Append("NULL");
                    return;

                case > 0 when isNameOf:
                    Sb.Append(value.SqlName());
                    return;

                case > 0 when isParameter || paramName.Contains(nameof(UserRecord.TABLE_NAME)) || paramName.Contains(nameof(UserRecord.TableName)) || string.Equals(paramName, "columnName", StringComparison.Ordinal):
                    Sb.Append(value);
                    return;

                case > 0:
                    AppendQuoted(value);
                    return;

                default:
                    AppendQuoted("");
                    return;
            }
        }
        finally
        {
            if ( isParameter && ColumnNames.Count > 0 ) { Parameters.Add(ColumnNames.Pop(), value, paramName); }
        }
    }
    public void AppendFormatted( ReadOnlySpan<string> value, string? format = null )
    {
        ParseFormat(ref format, out ushort indentLevel);
        AppendQuoted(value, indentLevel);
    }


    public void AppendFormatted<TRecord>( RecordID<TRecord> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        ParseFormat(ref format, out ushort indentLevel);
        AppendSingle(value.Value, format, true);
    }

    public void AppendFormatted<TRecord>( IEnumerable<RecordID<TRecord>> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, true);
    }
    public void AppendFormatted<TRecord>( ReadOnlySpan<RecordID<TRecord>> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, true);
    }


    public void AppendFormatted<TValue>( TValue value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
    {
        bool isParameter = Sb.Length > 0 && Sb[^1] == '@';
        bool isNameOf    = paramName.StartsWith("nameof(", StringComparison.Ordinal);
        if ( isNameOf && value is string s ) { ColumnNames.Push(s); }

        ParseFormat(ref format, out ushort indentLevel);

        try
        {
            switch ( value )
            {
                case null:
                case DBNull:
                    Sb.Append("NULL");
                    return;

                case SqlRaw n:
                    Sb.Append(n.Value);
                    return;

                case ColumnNames n:
                    Sb.Append(n.Value);
                    return;

                case VariableNames n:
                    Parameters.With(n.Parameters);
                    Sb.Append(n.Value);
                    return;

                case KeyValuePairs n:
                    Parameters.With(n.Parameters);
                    Sb.Append(n.Value);
                    return;

                case PostgresParameters n:
                    Parameters.With(n);

                    return;

                case Enum n:
                    // ReSharper disable once RedundantToStringCall
                    if ( string.Equals(format, "str", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "string", StringComparison.OrdinalIgnoreCase) ) { Sb.Append("'").Append(n.ToString()).Append("'"); }
                    else { Sb.Append(Convert.ToInt64(n)); }

                    return;

                case bool n:
                    Sb.Append(n.GetString());
                    return;

                case char n:
                    Sb.Append("'").Append(n).Append("'");
                    return;

                case Guid n:
                    AppendSingle(n, format, true);
                    return;

                case DateTime n:
                    AppendSingle(n, format, true);
                    return;

                case DateTimeOffset n:
                    AppendSingle(n, format, true);
                    return;

                case DateOnly n:
                    AppendSingle(n, format, true);
                    return;

                case TimeOnly n:
                    AppendSingle(n, format, true);
                    return;

                case TimeSpan n:
                    AppendSingle(n, format, true);
                    return;

                case Type n:
                    AppendQuoted(n.Name);
                    return;

                case StringBuilder n:
                    Sb.Append(n);
                    return;

                case ISpanFormattable x:
                    AppendSingle(x, format);
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
        finally
        {
            if ( isParameter && ColumnNames.Count > 0 ) { Parameters.Add(ColumnNames.Pop(), value, paramName); }
        }
    }


    public void AppendFormatted<TValue>( IEnumerable<TValue> value, string? format = null )
    {
        ParseFormat(ref format, out ushort indentLevel);

        switch ( value )
        {
            case null:
            case DBNull:
                Sb.Append("NULL");
                return;

            case StringBuilder n:
                Sb.Append(n);
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

            default:
                return;
        }
    }
    public void AppendFormatted<TValue>( ReadOnlySpan<TValue> value, string? format = null )
        where TValue : ISpanFormattable
    {
        bool needsQuotes = NeedsQuotes<TValue>();
        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, in needsQuotes);
    }
    public void AppendFormatted<TEnumerator, TValue>( ValueEnumerable<TEnumerator, TValue> value, string? format = null )
        where TEnumerator : struct, IValueEnumerator<TValue>
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];
        bool       needsQuotes = NeedsQuotes<TValue>();
        ParseFormat(ref format, out ushort indentLevel);

        using ValueEnumerator<TEnumerator, TValue> enumerator = value.GetEnumerator();

        while ( enumerator.MoveNext() )
        {
            Sb.Spacer(indentLevel);
            AppendSingle(in destination, enumerator.Current, format, in needsQuotes);
            Sb.Append(",\n");
        }

        Sb.Length -= 3;
    }


    private void AppendSingle<TValue>( TValue value, string? format, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];
        AppendSingle(in destination, value, format, in needsQuotes);
    }
    private void AppendSingle<TValue>( scoped in Span<char> destination, TValue value, string? format, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        if ( typeof(TValue).IsOneOf(typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly)) ) { format ??= "o"; }

        if ( value.TryFormat(destination, out int charsWritten, format, CultureInfo.InvariantCulture) )
        {
            if ( needsQuotes ) { Sb.Append("'"); }

            Sb.Append(destination[..charsWritten]);
            if ( needsQuotes ) { Sb.Append("'"); }
        }
        else
        {
            if ( needsQuotes ) { Sb.Append("'"); }

            Sb.Append(value.ToString(null, CultureInfo.InvariantCulture));
            if ( needsQuotes ) { Sb.Append("'"); }
        }
    }

    private void AppendMany<TValue>( IEnumerable<TValue> enumerable, string? format, ushort indentLevel, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];

        switch ( enumerable )
        {
            case IReadOnlyList<TValue> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    Sb.Spacer(indentLevel);
                    AppendSingle(in destination, list[i], format, in needsQuotes);
                    if ( i < list.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            case IList<TValue> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    Sb.Spacer(indentLevel);
                    AppendSingle(in destination, list[i], format, in needsQuotes);
                    if ( i < list.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            default:
            {
                using IEnumerator<TValue> enumerator = enumerable.GetEnumerator();

                while ( enumerator.MoveNext() )
                {
                    Sb.Spacer(indentLevel);
                    AppendSingle(in destination, enumerator.Current, format, in needsQuotes);
                    Sb.Append(",\n");
                }

                Sb.Length -= 3;
                return;
            }
        }
    }
    private void AppendMany<TValue>( ReadOnlySpan<TValue> array, string? format, ushort indentLevel, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];

        for ( int i = 0; i < array.Length; i++ )
        {
            Sb.Spacer(indentLevel);
            AppendSingle(in destination, array[i], format, in needsQuotes);
            if ( i < array.Length - 1 ) { Sb.Append(",\n"); }
        }
    }


    private void AppendQuoted( ReadOnlySpan<char> value )                     => Sb.Append("'").Append(value).Append("'");
    private void AppendQuoted( ReadOnlySpan<char> value, ushort indentLevel ) => Sb.Spacer(indentLevel).Append("'").Append(value).Append("'");


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

                while ( enumerator.MoveNext() )
                {
                    AppendQuoted(enumerator.Current, indentLevel);
                    Sb.Append(",\n");
                }

                Sb.Length -= 3;
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


    public override string ToString() => Sb.ToString();

    public (string SQL, ImmutableArray<NpgsqlParameter> Parameters) Build() => new(ToString(), [..Parameters.Values]);

    public SqlCommand ToSqlCommand( CommandType? commandType = null, CommandFlags flags = CommandFlags.None ) => SqlCommand.Create(ToString(), Parameters, commandType, flags);


    public static void ParseFormat( ref string? format, out ushort indentLevel )
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        if ( ushort.TryParse(span, out indentLevel) )
        {
            format = index >= 0
                         ? format?[index..]
                         : null;
        }
    }
    public static bool NeedsQuotes<TValue>()
    {
        if ( typeof(TValue).HasInterface<IRecordID>() ) { return true; }

        ReadOnlySpan<Type> span =
        [
            typeof(string),
            typeof(char),
            typeof(Guid),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(DateOnly),
            typeof(TimeSpan),
            typeof(TimeOnly)
        ];

        foreach ( Type type in span )
        {
            if ( typeof(TValue) == type ) { return true; }
        }

        return false;
    }
}
