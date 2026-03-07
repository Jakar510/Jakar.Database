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
    private readonly StringBuilder      __sb          = new(literalLength);
    private readonly PostgresParameters __parameters  = PostgresParameters.Create<TSelf>(formattedCount);
    private readonly Stack<string>      __columnNames = new(formattedCount);


    public void AppendLiteral( string value ) => __sb.Append(value);


    public void AppendFormatted( StringBuilder? value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY ) => __sb.Append(value);
    /*
    public void AppendFormatted( ReadOnlySpan<char> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
    {
        bool isParameter = __sb.Length > 0 && __sb[^1] == '@';
        bool isNameOf    = paramName.StartsWith("nameof(", StringComparison.Ordinal);
        if ( isNameOf && value is not null ) { __columnNames.Push(value); }

        try
        {
            switch ( value )
            {
                case null:
                    __sb.Append("NULL");
                    return;

                case var n when isNameOf:
                    __sb.Append(n.SqlName());
                    return;

                case var n when isParameter || paramName.Contains(nameof(UserRecord.TABLE_NAME)) || paramName.Contains(nameof(UserRecord.TableName)) || string.Equals(paramName, "columnName", StringComparison.Ordinal):
                    __sb.Append(n);
                    return;

                case var n:
                    AppendQuoted(n);
                    return;
            }
        }
        finally
        {
            if ( isParameter && __columnNames.Count > 0 ) { __parameters.Add(__columnNames.Pop(), value); }
        }
    }
    */
    public void AppendFormatted( string? value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
    {
        bool isParameter = __sb.Length > 0 && __sb[^1] == '@';
        bool isNameOf    = paramName.StartsWith("nameof(", StringComparison.Ordinal);
        if ( isNameOf && value is not null ) { __columnNames.Push(value); }

        try
        {
            switch ( value )
            {
                case null:
                    __sb.Append("NULL");
                    return;

                case var n when isNameOf:
                    __sb.Append(n.SqlName());
                    return;

                case var n when isParameter || paramName.Contains(nameof(UserRecord.TABLE_NAME)) || paramName.Contains(nameof(UserRecord.TableName)) || string.Equals(paramName, "columnName", StringComparison.Ordinal):
                    __sb.Append(n);
                    return;

                case var n:
                    AppendQuoted(n);
                    return;
            }
        }
        finally
        {
            if ( isParameter && __columnNames.Count > 0 ) { __parameters.Add(__columnNames.Pop(), value); }
        }
    }


    public void AppendFormatted<TValue>( TValue value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
    {
        bool isParameter = __sb.Length > 0 && __sb[^1] == '@';
        bool isNameOf    = paramName.StartsWith("nameof(", StringComparison.Ordinal);
        if ( isNameOf && value is string s ) { __columnNames.Push(s); }

        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        if ( ushort.TryParse(span, out ushort indentLevel) )
        {
            format = index >= 0
                         ? format?[index..]
                         : null;
        }

        try
        {
            switch ( value )
            {
                case null:
                case DBNull:
                    __sb.Append("NULL");
                    return;

                case SqlRaw n:
                    __sb.Append(n.Value);
                    return;

                case ColumnNames n:
                    __sb.Append(n.Value);
                    return;

                case VariableNames n:
                    __parameters.With(n.Parameters);
                    __sb.Append(n.Value);
                    return;

                case KeyValuePairs n:
                    __parameters.With(n.Parameters);
                    __sb.Append(n.Value);
                    return;

                case PostgresParameters n:
                    __parameters.With(n);

                    return;

                case Enum n:
                    // ReSharper disable once RedundantToStringCall
                    if ( string.Equals(format, "str", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "string", StringComparison.OrdinalIgnoreCase) ) { __sb.Append("'").Append(n.ToString()).Append("'"); }
                    else { __sb.Append(Convert.ToInt64(n)); }

                    return;

                case bool n:
                    __sb.Append(n.GetString());
                    return;

                case char n:
                    __sb.Append("'").Append(n).Append("'");
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
                    __sb.Append(n);
                    return;

                case ISpanFormattable x:
                    AppendSingle(x, format);
                    return;

                case IFormattable x:
                    __sb.Append(x.ToString(format, CultureInfo.InvariantCulture));
                    return;

                default:
                    // ReSharper disable once RedundantToStringCallForValueType
                    __sb.Append(value.ToString());
                    return;
            }
        }
        finally
        {
            if ( isParameter && __columnNames.Count > 0 ) { __parameters.Add(__columnNames.Pop(), value); }
        }
    }
    public void AppendFormatted<TValue>( IEnumerable<TValue> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
    {
        bool isParameter = __sb.Length > 0 && __sb[^1] == '@';
        bool isNameOf    = paramName.StartsWith("nameof(", StringComparison.Ordinal);
        if ( isNameOf && value is string s ) { __columnNames.Push(s); }

        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        if ( ushort.TryParse(span, out ushort indentLevel) )
        {
            format = index >= 0
                         ? format?[index..]
                         : null;
        }

        try
        {
            switch ( value )
            {
                case null:
                case DBNull:
                    __sb.Append("NULL");
                    return;

                case StringBuilder n:
                    __sb.Append(n);
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
        finally
        {
            if ( isParameter && __columnNames.Count > 0 ) { __parameters.Add(__columnNames.Pop(), value); }
        }
    }


    private void AppendSingle<TValue>( TValue value, string? format, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];

        if ( value.TryFormat(destination, out int written, format, CultureInfo.InvariantCulture) )
        {
            if ( needsQuotes ) { __sb.Append("'"); }

            __sb.Append(destination[..written]);
            if ( needsQuotes ) { __sb.Append("'"); }
        }
        else
        {
            if ( needsQuotes ) { __sb.Append("'"); }

            __sb.Append(value.ToString(null, CultureInfo.InvariantCulture));
            if ( needsQuotes ) { __sb.Append("'"); }
        }
    }
    private void AppendMany<TValue>( IEnumerable<TValue> enumerable, string? format, ushort indentLevel, bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        switch ( enumerable )
        {
            case IReadOnlyList<TValue> array:
            {
                for ( int i = 0; i < array.Count; i++ )
                {
                    AppendSingle(array[i], format, in needsQuotes);
                    if ( i < array.Count - 1 ) { __sb.Append(",\n ").AppendJoin(' ', indentLevel * 4); }
                }

                return;
            }

            case IList<TValue> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    AppendSingle(list[i], format, in needsQuotes);
                    if ( i < list.Count - 1 ) { __sb.Append(",\n ").AppendJoin(' ', indentLevel * 4); }
                }

                return;
            }

            default:
            {
                using IEnumerator<TValue> enumerator = enumerable.GetEnumerator();

                while ( enumerator.MoveNext() )
                {
                    AppendSingle(enumerator.Current, format, in needsQuotes);
                    __sb.Append(",\n ").AppendJoin(' ', indentLevel * 4);
                }

                __sb.Length -= 3;
                return;
            }
        }
    }


    private void AppendQuoted( string value )                     => __sb.Append("'").Append(value).Append("'");
    private void AppendQuoted( string value, ushort indentLevel ) => __sb.AppendJoin(' ', indentLevel * 4).Append("'").Append(value).Append("'");
    private void AppendQuoted( IEnumerable<string> enumerable, ushort indentLevel )
    {
        switch ( enumerable )
        {
            case IReadOnlyList<string> array:
            {
                for ( int i = 0; i < array.Count; i++ )
                {
                    AppendQuoted(array[i], indentLevel);
                    if ( i < array.Count - 1 ) { __sb.Append(",\n "); }
                }

                return;
            }

            case IList<string> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    AppendQuoted(list[i], indentLevel);
                    if ( i < list.Count - 1 ) { __sb.Append(",\n "); }
                }

                return;
            }

            default:
            {
                using IEnumerator<string> enumerator = enumerable.GetEnumerator();

                while ( enumerator.MoveNext() )
                {
                    AppendQuoted(enumerator.Current, indentLevel);
                    __sb.Append(",\n ");
                }

                __sb.Length -= 3;
                return;
            }
        }
    }


    public override string ToString() => __sb.ToString();

    public (string SQL, ImmutableArray<NpgsqlParameter> Parameters) Build() => new(ToString(), [..__parameters.Params]);

    public SqlCommand ToSqlCommand( CommandType? commandType = null, CommandFlags flags = CommandFlags.None ) => SqlCommand.Create(ToString(), __parameters, commandType, flags);
}
