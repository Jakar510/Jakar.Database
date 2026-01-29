// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using ZLinq.Linq;



namespace Jakar.Database;


[DefaultMember(nameof(Empty))]
public readonly struct PostgresParameters( TableMetaData table ) : IEquatable<PostgresParameters>
{
    public static readonly PostgresParameters    Empty    = new(TableMetaData.Empty);
    private readonly       List<NpgsqlParameter> __buffer = new(Math.Max(table.Count, DEFAULT_CAPACITY));


    public int                           Count    => __buffer.Count;
    public int                           Capacity => __buffer.Capacity;
    public ReadOnlySpan<NpgsqlParameter> Values   => __buffer.AsSpan();
    public ValueEnumerable<Select<FromSpan<NpgsqlParameter>, NpgsqlParameter, string>, string> ParameterNames
    {
        [Pure] get => Values.AsValueEnumerable()
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
            int           length = table.Properties.Values.Sum(static x => x.ColumnName.Length) + ( table.Count - 1 ) * SPACER.Length;
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
            int           length = table.Properties.Values.Sum(static x => x.VariableName.Length) + ( table.Count - 1 ) * SPACER.Length;
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


    [Obsolete("For serialization only", true)] public PostgresParameters() : this(TableMetaData.Empty) => throw new NotSupportedException();

    public static PostgresParameters Create<TSelf>()
        where TSelf : ITableRecord<TSelf> => new(TSelf.PropertyMetaData);


    public PostgresParameters With( in PostgresParameters parameters )
    {
        __buffer.AddRange(parameters.Values);
        return this;
    }


    /*
    public PostgresParameters Add<T>( T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        PrecisionInfo precision = PrecisionInfo.Default;
        PostgresType  pgType    = PostgresTypes.GetType<T>(out bool isNullable, out bool isEnum, ref precision);

        NpgsqlParameter parameter = new NpgsqlParameter(parameterName, value)
                                    {
                                        Direction     = direction,
                                        SourceVersion = sourceVersion,
                                        NpgsqlDbType  = pgType.ToNpgsqlDbType(),
                                        IsNullable    = isNullable
                                    };

        return Add(parameter);
    }
    */
    public PostgresParameters Add<T>( string propertyName, T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        ColumnMetaData meta = table[propertyName];
        return Add(meta, value, parameterName, direction, sourceVersion);
    }
    public PostgresParameters Add<T>( ColumnMetaData meta, T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        NpgsqlParameter parameter = new(parameterName, meta.DbType.ToNpgsqlDbType())
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
        int           length = table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + ( table.Count - 1 ) * match.Length;
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


    private string GetColumnName( string   propertyName ) => table[propertyName].ColumnName;
    private string GetVariableName( string propertyName ) => table[propertyName].VariableName;
    private string GetKeyValuePair( string propertyName ) => table[propertyName].KeyValuePair;


    public override int GetHashCode() => HashCode.Combine(__buffer);
    public ulong GetHash64()
    {
        ReadOnlySpan<string> names = ParameterNames.ToArray();
        return Hashes.Hash(in names);
    }
    public UInt128 GetHash128()
    {
        ReadOnlySpan<string> names = ParameterNames.ToArray();
        return Hashes.Hash128(in names);
    }


    public          bool Equals( PostgresParameters      other )                          => __buffer.Equals(other.__buffer);
    public override bool Equals( object?                 obj )                            => obj is PostgresParameters other && Equals(other);
    public static   bool operator ==( PostgresParameters left, PostgresParameters right ) => left.Equals(right);
    public static   bool operator !=( PostgresParameters left, PostgresParameters right ) => !left.Equals(right);
}
