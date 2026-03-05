// Jakar.Database :: Jakar.Database
// 03/04/2026  17:09

namespace Jakar.Database;


public readonly struct SqlRaw( string value )
{
    public readonly string Value = value;
}



[InterpolatedStringHandler]
public readonly ref struct SqlInterpolatedStringHandler<TSelf>( int literalLength, int formattedCount )
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    private readonly StringBuilder      __sb          = new(literalLength);
    private readonly PostgresParameters __parameters  = PostgresParameters.Create<TSelf>(formattedCount);
    private readonly Stack<string>      __columnNames = new(formattedCount);


    public void AppendLiteral( string value ) => __sb.Append(value);
    public void AppendFormatted<T>( T value, ReadOnlySpan<char> format = default, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
    {
        bool isParameter = __sb.Length > 0 && __sb[^1] == '@';
        bool isNameOf    = paramName.StartsWith("nameof(", StringComparison.Ordinal);
        if ( isNameOf && value is string s ) { __columnNames.Push(s); }

        try
        {
            switch ( value )
            {
                case null:
                case DBNull:
                    __sb.Append("NULL");
                    return;

                case bool n:
                    __sb.Append(n
                                    ? "true"
                                    : "false");

                    return;

                case char n:
                    __sb.Append("'").Append(n).Append("'");
                    return;

                case SqlRaw n:
                    __sb.Append(n.Value);
                    return;

                case string n when isNameOf:
                    __sb.Append(n.SqlName());
                    return;

                case string n when isParameter || paramName.Contains(nameof(UserRecord.TABLE_NAME)) || paramName.Contains(nameof(UserRecord.TableName)) || string.Equals(paramName, "columnName", StringComparison.Ordinal):
                    __sb.Append(n);
                    return;

                case string n:
                    __sb.Append("'").Append(n).Append("'");
                    return;

                case IReadOnlyList<string> n:
                    for ( int i = 0; i < n.Count; i++ )
                    {
                        __sb.Append("'").Append(n[i]).Append("'");
                        if ( i < n.Count - 1 ) { __sb.Append(", "); }
                    }

                    return;

                case StringBuilder n:
                    foreach ( ReadOnlyMemory<char> memory in n.GetChunks() ) { __sb.Append(memory.Span); }

                    return;

                case PostgresParameters n when paramName.Contains(nameof(PostgresParameters.ColumnNames)):
                    __parameters.With(n);
                    __sb.Append(n.GetColumnNames(format));
                    return;

                case PostgresParameters n when paramName.Contains(nameof(PostgresParameters.VariableNames)):
                    __parameters.With(n);
                    __sb.Append(n.GetKeyValuePairs(format));
                    return;

                case PostgresParameters n when paramName.Contains(nameof(PostgresParameters.KeyValuePairs)):
                    __parameters.With(n);
                    __sb.Append(n.GetKeyValuePairs(format));
                    return;

                case PostgresParameters n:
                    __parameters.With(n);

                    return;

                case Enum n:
                    // ReSharper disable once RedundantToStringCall
                    // __sb.Append("'").Append(n.ToString()).Append("'");
                    __sb.Append(Convert.ToInt32(n));
                    return;

                case Type n:
                    __sb.Append(n.Name);
                    return;

                case Guid n:
                    Append(n);
                    return;

                case DateTime n:
                    Append(n);
                    return;

                case DateTimeOffset n:
                    Append(n);
                    return;

                case DateOnly n:
                    Append(n);
                    return;

                case TimeOnly n:
                    Append(n);
                    return;

                case TimeSpan n:
                    Append(n);
                    return;

                case IReadOnlyList<Int128> n:
                    __sb.Append(n);
                    return;

                case IReadOnlyList<UInt128> n:
                    __sb.Append(n);
                    return;

                case IReadOnlyList<byte> n:
                    Append(n);
                    return;

                case IReadOnlyList<sbyte> n:
                    Append(n);
                    return;

                case IReadOnlyList<short> n:
                    Append(n);
                    return;

                case IReadOnlyList<int> n:
                    Append(n);
                    return;

                case IReadOnlyList<long> n:
                    Append(n);
                    return;

                case IReadOnlyList<uint> n:
                    Append(n);
                    return;

                case IReadOnlyList<ushort> n:
                    Append(n);
                    return;

                case IReadOnlyList<ulong> n:
                    Append(n);
                    return;

                case IReadOnlyList<float> n:
                    Append(n);
                    return;

                case IReadOnlyList<double> n:
                    Append(n);
                    return;

                case IReadOnlyList<decimal> n:
                    Append(n);
                    return;

                case IReadOnlyList<char> n:
                    Append(n);
                    return;

                case IReadOnlyList<Guid> n:
                    Append(n);
                    return;

                case IReadOnlyList<DateTime> n:
                    Append(n);
                    return;

                case IReadOnlyList<DateTimeOffset> n:
                    Append(n);
                    return;

                case IReadOnlyList<DateOnly> n:
                    Append(n);
                    return;

                case IReadOnlyList<TimeSpan> n:
                    Append(n);
                    return;

                case IReadOnlyList<TimeOnly> n:
                    Append(n);
                    return;

                case ISpanFormattable formattable:
                    Append(formattable);
                    return;

                case IFormattable formattable:
                    __sb.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                    return;

                default:
                    __sb.Append(value);
                    return;
            }
        }
        finally
        {
            if ( isParameter && __columnNames.Count > 0 ) { __parameters.Add(__columnNames.Pop(), value); }
        }
    }


    private void Append<T>( T value )
        where T : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];

        if ( value.TryFormat(destination, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture) ) { __sb.Append("'").Append(destination[..written]).Append("'"); }
        else { __sb.Append("'").Append(value.ToString(null, CultureInfo.InvariantCulture)).Append("'"); }
    }
    private void Append<T>( IReadOnlyList<T> array )
        where T : ISpanFormattable
    {
        for ( int i = 0; i < array.Count; i++ )
        {
            Append(array[i]);
            if ( i < array.Count - 1 ) { __sb.Append(", "); }
        }
    }

    private void Append( DateTimeOffset value )
    {
        Span<char> destination = stackalloc char[256];

        if ( value.TryFormat(destination, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture) ) { __sb.Append("'").Append(destination[..written]).Append("'"); }
        else { __sb.Append("'").Append(value.ToString(null, CultureInfo.InvariantCulture)).Append("'"); }
    }
    private void Append( IReadOnlyList<DateTimeOffset> array )
    {
        for ( int i = 0; i < array.Count; i++ )
        {
            Append(array[i]);
            if ( i < array.Count - 1 ) { __sb.Append(", "); }
        }
    }

    private void Append( DateTime value )
    {
        Span<char> destination = stackalloc char[256];

        if ( value.TryFormat(destination, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture) ) { __sb.Append("'").Append(destination[..written]).Append("'"); }
        else { __sb.Append("'").Append(value.ToString(null, CultureInfo.InvariantCulture)).Append("'"); }
    }
    private void Append( IReadOnlyList<DateTime> array )
    {
        for ( int i = 0; i < array.Count; i++ )
        {
            Append(array[i]);
            if ( i < array.Count - 1 ) { __sb.Append(", "); }
        }
    }

    private void Append( DateOnly value )
    {
        Span<char> destination = stackalloc char[256];

        if ( value.TryFormat(destination, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture) ) { __sb.Append("'").Append(destination[..written]).Append("'"); }
        else { __sb.Append("'").Append(value.ToString(null, CultureInfo.InvariantCulture)).Append("'"); }
    }
    private void Append( IReadOnlyList<DateOnly> array )
    {
        for ( int i = 0; i < array.Count; i++ )
        {
            Append(array[i]);
            if ( i < array.Count - 1 ) { __sb.Append(", "); }
        }
    }

    private void Append( TimeSpan value )
    {
        Span<char> destination = stackalloc char[256];

        if ( value.TryFormat(destination, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture) ) { __sb.Append("'").Append(destination[..written]).Append("'"); }
        else { __sb.Append("'").Append(value.ToString(null, CultureInfo.InvariantCulture)).Append("'"); }
    }
    private void Append( IReadOnlyList<TimeSpan> array )
    {
        for ( int i = 0; i < array.Count; i++ )
        {
            Append(array[i]);
            if ( i < array.Count - 1 ) { __sb.Append(", "); }
        }
    }

    private void Append( TimeOnly value )
    {
        Span<char> destination = stackalloc char[256];

        if ( value.TryFormat(destination, out int written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture) ) { __sb.Append("'").Append(destination[..written]).Append("'"); }
        else { __sb.Append("'").Append(value.ToString(null, CultureInfo.InvariantCulture)).Append("'"); }
    }
    private void Append( IReadOnlyList<TimeOnly> array )
    {
        for ( int i = 0; i < array.Count; i++ )
        {
            Append(array[i]);
            if ( i < array.Count - 1 ) { __sb.Append(", "); }
        }
    }

    private void Append( Guid value )
    {
        Span<char> destination = stackalloc char[256];

        if ( value.TryFormat(destination, out int written) ) { __sb.Append("'").Append(destination[..written]).Append("'"); }
        else { __sb.Append("'").Append(value.ToString(null, CultureInfo.InvariantCulture)).Append("'"); }
    }
    private void Append( IReadOnlyList<Guid> array )
    {
        for ( int i = 0; i < array.Count; i++ )
        {
            Append(array[i]);
            if ( i < array.Count - 1 ) { __sb.Append(", "); }
        }
    }


    public override string ToString() => __sb.ToString();

    public (string SQL, ImmutableArray<NpgsqlParameter> Parameters) Build() => new(ToString(), [..__parameters.Params]);

    public SqlCommand ToSqlCommand( CommandType? commandType = null, CommandFlags flags = CommandFlags.None ) => SqlCommand.Create(ToString(), __parameters, commandType, flags);
}
