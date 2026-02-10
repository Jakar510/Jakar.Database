// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using ZLinq.Linq;



namespace Jakar.Database;


public readonly struct PostgresParameters : IEquatable<PostgresParameters>
{
    public readonly   ITableMetaData                         Table;
    internal readonly Dictionary<int, List<NpgsqlParameter>> Groups;


    internal List<NpgsqlParameter> Params => Groups[0];
    public   int                   Count  => Groups.Count;

    public ValueEnumerable<SelectMany<FromDictionary<int, List<NpgsqlParameter>>, KeyValuePair<int, List<NpgsqlParameter>>, NpgsqlParameter>, NpgsqlParameter> Parameters => Groups.AsValueEnumerable()
                                                                                                                                                                                   .SelectMany(static x => x.Value);

    public int  ParameterCount => Parameters.Count();
    public int  Capacity       => Groups.Capacity;
    public bool IsGrouped      => Groups.Count > 1;
    public PooledArray<string> ParameterNameArray
    {
        [Pure] [MustDisposeResource] get => Parameters.Select(x => x.ParameterName)
                                                      .ToArrayPool();
    }
    public ValueEnumerable<FromList<NpgsqlParameter>, NpgsqlParameter>                                      Values           { [Pure] get => Params.AsValueEnumerable(); }
    public ValueEnumerable<ListSelect<NpgsqlParameter, string>, string>                                     ParameterNames   { [Pure] get => Values.Select(static x => x.ParameterName); }
    public ValueEnumerable<DistinctBy<FromList<NpgsqlParameter>, NpgsqlParameter, string>, NpgsqlParameter> SourceProperties { [Pure] get => Values.DistinctBy(static x => x.SourceColumn); }
    public int                                                                                              SpacerCount      => Math.Max(Params.Count, Table.Count) - 1;


    public ReadOnlySpan<NpgsqlParameter> Span => Params.AsSpan();


    public PostgresParameters() => throw new InvalidOperationException($"Use {nameof(PostgresParameters)}.{nameof(Create)} instead.");
    internal PostgresParameters( ITableMetaData table )
    {
        Table  = table;
        Groups = new Dictionary<int, List<NpgsqlParameter>> { [0] = new(Math.Max(table.Count, DEFAULT_CAPACITY)) };
    }


    public static PostgresParameters Create<TSelf>()
        where TSelf : class, ITableRecord<TSelf> => new(TSelf.PropertyMetaData);
    public static PostgresParameters Create<TSelf>( TSelf self )
        where TSelf : class, ITableRecord<TSelf> => new(TSelf.PropertyMetaData);
    public static PostgresParameters Create<TSelf>( IEnumerable<TSelf> records )
        where TSelf : class, ITableRecord<TSelf>
    {
        PostgresParameters parameters = new(TSelf.PropertyMetaData);
        foreach ( TSelf record in records ) { parameters.With(record.ToDynamicParameters()); }

        return parameters;
    }
    public static PostgresParameters Create<TSelf>( params ReadOnlySpan<TSelf> records )
        where TSelf : class, ITableRecord<TSelf>
    {
        PostgresParameters parameters = new(TSelf.PropertyMetaData);
        foreach ( TSelf record in records ) { parameters.With(record.ToDynamicParameters()); }

        return parameters;
    }


    public ValueEnumerable<ListWhere<NpgsqlParameter>, NpgsqlParameter> ColumnsFor( string propertyName ) => Values.Where(x => string.Equals(x.SourceColumn, propertyName, StringComparison.InvariantCulture));


    public PostgresParameters With( in PostgresParameters parameters )
    {
        if ( !ReferenceEquals(Table, parameters.Table) ) { throw new InvalidOperationException($"Parameter Tables is not matched. Original Table: '{Table.TableName}'  Other Table: '{parameters.Table.TableName}' "); }

        Groups.Add(Groups.Count, [..parameters.Span]);
        return this;
    }


    public PostgresParameters Add<T>( string propertyName, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : struct, Enum => Add(Table[propertyName]
                                         .ToParameter(value, parameterName, direction, sourceVersion));
    public PostgresParameters Add<T>( string propertyName, T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default ) =>
        Add(Table[propertyName]
               .ToParameter(value, parameterName, direction, sourceVersion));

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public PostgresParameters Add( NpgsqlParameter parameter )
    {
        Params.Add(parameter);
        return this;
    }


    public StringBuilder KeyValuePairs( bool matchAll, int indentLevel )
    {
        string        match  = matchAll.GetAndOr();
        int           count  = Count;
        int           length = Table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + Params.Count * ( indentLevel * 4 + 3 );
        StringBuilder sb     = new(length);
        int           index  = 0;

        foreach ( NpgsqlParameter parameter in SourceProperties )
        {
            sb.Append($" {parameter.SourceColumn} = @{parameter.ParameterName} ")
              .Append(",\n")
              .Append(' ', indentLevel * 4);

            ;
            if ( index++ < count - 1 ) { sb.Append(match); }
        }

        return sb;
    }
    public StringBuilder GetParameterString( int indentLevel )
    {
        int           length = ParameterNames.Sum(static x => x.Length + 10) + Params.Count * indentLevel * 4;
        StringBuilder sb     = new(length);
        int           count  = Count;
        int           index  = 0;

        foreach ( string value in ParameterNames )
        {
            sb.Append('@')
              .Append(value);

            if ( index++ < count - 1 )
            {
                sb.Append(",\n")
                  .Append(' ', indentLevel * 4);
            }
        }

        return sb;
    }
    public StringBuilder GetVariableNames( int indentLevel )
    {
        int           length = Table.MaxLength_ColumnName * Table.Count + Parameters.Sum(static x => x.ParameterName.Length + 10);
        StringBuilder sb     = new(length);
        int           count  = Count;
        int           index  = 0;

        if ( IsGrouped )
        {
            for ( int i = 0; i < Groups.Count; i++ )
            {
                foreach ( NpgsqlParameter column in Groups[i] )
                {
                    sb.Append('@')
                      .Append(column.ParameterName)
                      .Append('_')
                      .Append(i);

                    if ( index++ < count - 1 )
                    {
                        sb.Append(",\n")
                          .Append(' ', indentLevel * 4);
                    }
                }

                if ( i < Groups.Count )
                {
                    sb.Append(',')
                      .Append('\n');
                }
            }
        }
        else
        {
            foreach ( NpgsqlParameter column in Values )
            {
                sb.Append('@')
                  .Append(column.ParameterName);

                if ( index++ < count - 1 )
                {
                    sb.Append(",\n")
                      .Append(' ', indentLevel * 4);
                }
            }
        }

        return sb;
    }


    public override int GetHashCode() => Groups.GetHashCode();
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


    public          bool Equals( PostgresParameters      other )                          => Params.Equals(other.Params);
    public override bool Equals( object?                 obj )                            => obj is PostgresParameters other && Equals(other);
    public static   bool operator ==( PostgresParameters left, PostgresParameters right ) => left.Equals(right);
    public static   bool operator !=( PostgresParameters left, PostgresParameters right ) => !left.Equals(right);
}
