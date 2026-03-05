// Jakar.Database :: Jakar.Database
// 03/04/2026  17:09

namespace Jakar.Database;


[InterpolatedStringHandler]
public readonly ref struct SqlInterpolatedStringHandler<TSelf>( int literalLength, int formattedCount )
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    private readonly StringBuilder      __sb          = new(literalLength);
    private readonly PostgresParameters __parameters  = PostgresParameters.Create<TSelf>(formattedCount);
    private readonly Stack<string>      __columnNames = new(formattedCount);


    public void AppendLiteral( string value ) => __sb.Append(value);
    public void AppendFormatted<T>( T value, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
    {
        bool isNameOf = paramName.Contains("nameof");
        if ( isNameOf && value is string s ) { __columnNames.Push(s); }

        bool isParameter = __sb[^1].Equals('@');

        switch ( value )
        {
            case bool n:
                __sb.Append(n);
                break;

            case byte n:
                __sb.Append(n);
                break;

            case sbyte n:
                __sb.Append(n);
                break;

            case short n:
                __sb.Append(n);
                break;

            case int n:
                __sb.Append(n);
                break;

            case long n:
                __sb.Append(n);
                break;

            case uint n:
                __sb.Append(n);
                break;

            case ushort n:
                __sb.Append(n);
                break;

            case ulong n:
                __sb.Append(n);
                break;

            case float n:
                __sb.Append(n);
                break;

            case double n:
                __sb.Append(n);
                break;

            case decimal n:
                __sb.Append(n);
                break;

            case DateTime n:
                __sb.Append(n);
                break;

            case DateTimeOffset n:
                __sb.Append(n);
                break;

            case TimeSpan n:
                __sb.Append(n);
                break;

            case TimeOnly n:
                __sb.Append(n);
                break;

            case DateOnly n:
                __sb.Append(n);
                break;

            case char n:
                __sb.Append(n);
                break;

            case char[] n:
                __sb.Append(n);
                break;

            case string n when isNameOf:
                __sb.Append(n.SqlName());
                break;

            case string n:
                __sb.Append(n);
                break;

            case Guid n:
            {
                Span<char> destination = stackalloc char[100];
                n.TryFormat(destination, out int charsWritten);
                __sb.Append(destination[..charsWritten]);
                break;
            }

            case Guid[] n:
            {
                Span<char> destination = stackalloc char[100];

                for ( int i = 0; i < n.Length; i++ )
                {
                    n[i].TryFormat(destination, out int charsWritten);
                    __sb.Append('\'').Append(destination[..charsWritten]).Append('\'');

                    if ( i < ( n.Length - 1 ) ) { __sb.Append(", "); }
                }

                break;
            }

            case Type n:
                __sb.Append(n.Name);
                break;

            case StringBuilder n:
                foreach ( ReadOnlyMemory<char> memory in n.GetChunks() ) { __sb.Append(memory.Span); }

                break;

            default:
                __sb.Append(value);
                break;
        }

        if ( isParameter ) { __parameters.Add(__columnNames.Pop(), value); }
    }


    public override string                                                   ToString()                                                                              => __sb.ToString();
    public          (string SQL, ImmutableArray<NpgsqlParameter> Parameters) Build()                                                                                 => new(ToString(), [..__parameters.Params]);
    public          SqlCommand<TSelf>                                        ToSqlCommand( CommandType? commandType = null, CommandFlags flags = CommandFlags.None ) => new(ToString(), __parameters, commandType, flags);
}
