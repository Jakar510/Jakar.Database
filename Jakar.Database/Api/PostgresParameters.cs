// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using ZLinq;
using ZLinq.Linq;



namespace Jakar.Database;


public readonly struct PostgresParameters : IEquatable<PostgresParameters>
{
    private readonly  List<NpgsqlParameter> __buffer;
    internal readonly ITableMetaData        Table;


    public int                           Count              => __buffer.Count;
    public int                           Capacity           => __buffer.Capacity;
    public ReadOnlySpan<NpgsqlParameter> Values             => __buffer.AsSpan();
    public PooledArray<string>           ParameterNameArray { [Pure] [MustDisposeResource] get => ParameterNames.ToArrayPool(); }
    public ValueEnumerable<ListSelect<NpgsqlParameter, string>, string> ParameterNames
    {
        [Pure] get => __buffer.AsValueEnumerable()
                              .Select(static x => x.ParameterName);
    }
    public StringBuilder Parameters
    {
        get
        {
            StringBuilder sb = new();

            int count = Count;
            int index = 0;

            foreach ( string pair in ParameterNames.Select(GetColumnName) )
            {
                if ( index++ < count - 1 )
                {
                    sb.Append(pair)
                      .Append(", ");
                }
                else { sb.Append(pair); }
            }

            return sb;
        }
    }
    public StringBuilder ColumnNames
    {
        get
        {
            const string  SPACER = ",\n      ";
            int           length = Table.Properties.Values.Sum(static x => x.ColumnName.Length) + ( Table.Count - 1 ) * SPACER.Length;
            StringBuilder sb     = new(length);
            int           count  = Count;
            int           index  = 0;

            foreach ( string pair in ParameterNames.Select(GetColumnName) )
            {
                if ( index++ < count - 1 )
                {
                    sb.Append(pair)
                      .Append(SPACER);
                }
                else { sb.Append(pair); }
            }

            return sb;
        }
    }
    public StringBuilder VariableNames
    {
        get
        {
            const string  SPACER = ",\n      ";
            int           length = Table.Properties.Values.Sum(static x => x.VariableName.Length) + ( Table.Count - 1 ) * SPACER.Length;
            StringBuilder sb     = new(length);
            int           count  = Count;
            int           index  = 0;

            foreach ( string pair in ParameterNames.Select(GetVariableName) )
            {
                if ( index++ < count - 1 )
                {
                    sb.Append(pair)
                      .Append(SPACER);
                }
                else { sb.Append(pair); }
            }

            return sb;
        }
    }


    public PostgresParameters() => throw new InvalidOperationException($"Use {nameof(PostgresParameters)}.{nameof(Create)} instead.");
    internal PostgresParameters( ITableMetaData table )
    {
        Table    = table;
        __buffer = new List<NpgsqlParameter>(Math.Max(table.Count, DEFAULT_CAPACITY));
    }
    public static PostgresParameters Create<TSelf>()
        where TSelf : class, ITableRecord<TSelf> => new(TSelf.PropertyMetaData);


    public PostgresParameters With( in PostgresParameters parameters )
    {
        __buffer.AddRange(parameters.Values);
        return this;
    }


    public PostgresParameters Add<T>( string propertyName, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : struct, Enum
    {
        ColumnMetaData meta = Table[propertyName];
        return Add(meta, value, parameterName, direction, sourceVersion);
    }
    public PostgresParameters Add<T>( ColumnMetaData meta, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : struct, Enum
    {
        NpgsqlParameter parameter = new(parameterName.SqlColumnName(), meta.DbType.ToNpgsqlDbType())
                                    {
                                        SourceColumn  = meta.ColumnName,
                                        IsNullable    = meta.IsNullable,
                                        SourceVersion = sourceVersion,
                                        Direction     = direction,
                                        Value         = value?.ToString()
                                    };

        return Add(parameter);
    }
    public PostgresParameters Add<T>( string propertyName, T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        ColumnMetaData meta = Table[propertyName];
        return Add(meta, value, parameterName, direction, sourceVersion);
    }
    public PostgresParameters Add<T>( ColumnMetaData meta, T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        NpgsqlParameter parameter = new(parameterName.SqlColumnName(), meta.DbType.ToNpgsqlDbType())
                                    {
                                        SourceColumn  = meta.ColumnName,
                                        IsNullable    = meta.IsNullable,
                                        SourceVersion = sourceVersion,
                                        Direction     = direction,
                                        Value         = value
                                    };

        return Add(parameter);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public PostgresParameters Add( NpgsqlParameter parameter )
    {
        __buffer.Add(parameter);
        return this;
    }


    public StringBuilder KeyValuePairs( bool matchAll )
    {
        string        match  = matchAll.GetAndOr();
        int           count  = Count;
        int           length = Table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + ( Table.Count - 1 ) * match.Length;
        StringBuilder sb     = new(length);
        int           index  = 0;

        foreach ( string pair in ParameterNames.Select(GetKeyValuePair) )
        {
            if ( index++ < count - 1 )
            {
                sb.Append(pair)
                  .Append(match);
            }
            else { sb.Append(pair); }
        }

        return sb;
    }


    private string GetColumnName( string   propertyName ) => Table[propertyName].ColumnName;
    private string GetVariableName( string propertyName ) => Table[propertyName].VariableName;
    private string GetKeyValuePair( string propertyName ) => Table[propertyName].KeyValuePair;


    public override int GetHashCode() => HashCode.Combine(__buffer);
    public ulong GetHash64()
    {
        using PooledArray<string> array = ParameterNameArray;
        ReadOnlySpan<string>      names = array.Span;
        return Hashes.Hash(in names);
    }
    public UInt128 GetHash128()
    {
        using PooledArray<string> array = ParameterNameArray;
        ReadOnlySpan<string>      names = array.Span;
        return Hashes.Hash128(in names);
    }


    public          bool Equals( PostgresParameters      other )                          => __buffer.Equals(other.__buffer);
    public override bool Equals( object?                 obj )                            => obj is PostgresParameters other && Equals(other);
    public static   bool operator ==( PostgresParameters left, PostgresParameters right ) => left.Equals(right);
    public static   bool operator !=( PostgresParameters left, PostgresParameters right ) => !left.Equals(right);
}
