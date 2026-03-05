// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using ZLinq.Linq;



namespace Jakar.Database;


public readonly struct PostgresParameters : IEquatable<PostgresParameters>
{
    public readonly   ITableMetaData                         Table;
    internal readonly Dictionary<int, List<NpgsqlParameter>> Groups = new() { [0] = [] };


    public   PostgresParameters    ColumnNames   => this;
    public   PostgresParameters    VariableNames => this;
    public   PostgresParameters    KeyValuePairs => this;
    internal List<NpgsqlParameter> Params        => Groups[0];
    public   int                   Count         => Groups.Sum(static x => x.Value.Count);

    public ValueEnumerable<SelectMany<FromDictionary<int, List<NpgsqlParameter>>, KeyValuePair<int, List<NpgsqlParameter>>, NpgsqlParameter>, NpgsqlParameter> Parameters => Groups.AsValueEnumerable().SelectMany(static x => x.Value);

    public int                                                                                              ParameterCount     => Parameters.Count();
    public int                                                                                              Capacity           => Groups.Capacity;
    public bool                                                                                             IsGrouped          => Groups.Count > 1;
    public PooledArray<string>                                                                              ParameterNameArray { [Pure] [MustDisposeResource] get => Parameters.Select(static x => x.ParameterName).ToArrayPool(); }
    public ValueEnumerable<FromList<NpgsqlParameter>, NpgsqlParameter>                                      Values             { [Pure] get => Params.AsValueEnumerable(); }
    public ValueEnumerable<ListSelect<NpgsqlParameter, string>, string>                                     ParameterNames     { [Pure] get => Values.Select(static x => x.ParameterName); }
    public ValueEnumerable<DistinctBy<FromList<NpgsqlParameter>, NpgsqlParameter, string>, NpgsqlParameter> SourceProperties   { [Pure] get => Values.DistinctBy(static x => x.SourceColumn); }
    public int                                                                                              SpacerCount        => Math.Max(Params.Count, Table.Count) - 1;
    public ReadOnlySpan<NpgsqlParameter>                                                                    Span               => Params.AsSpan();


    public PostgresParameters() => throw new InvalidOperationException($"Use {nameof(PostgresParameters)}.{nameof(Create)} instead.");
    internal PostgresParameters( ITableMetaData table )
    {
        Table = table;
        Params.EnsureCapacity(table.Count);
    }


    /*
    internal StringBuilder GetParameterString( ReadOnlySpan<char> format ) => GetParameterString(int.TryParse(format, out int indentLevel)
                                                                                                     ? indentLevel
                                                                                                     : 1);
    internal StringBuilder GetParameterString( int indentLevel )
    {
        int           length = ParameterNames.Sum(static x => x.Length + 10) + Params.Count * indentLevel * 4;
        StringBuilder sb     = new(length);
        int           index  = 0;
        int           count  = Count;

        foreach ( string value in ParameterNames )
        {
            sb.Append(' ', indentLevel * 4).Append('@').Append(value);

            if ( index++ < count - 1 ) { sb.Append(",\n"); }
        }

        return sb;
    }
    */

    internal StringBuilder GetKeyValuePairs( ReadOnlySpan<char> format ) => GetKeyValuePairs(int.TryParse(format, out int indentLevel)
                                                                                                 ? indentLevel
                                                                                                 : 1);
    internal StringBuilder GetKeyValuePairs( int indentLevel )
    {
        int           length = Table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + Params.Count * ( indentLevel * 4 + 3 );
        StringBuilder sb     = new(length);
        int           index  = 0;
        int           count  = Count;

        foreach ( NpgsqlParameter parameter in SourceProperties )
        {
            sb.Append(' ', indentLevel * 4).Append($" {parameter.SourceColumn} = @{parameter.ParameterName} ").Append(",\n");

            if ( index++ < count - 1 ) { sb.Append("AND"); }
        }

        return sb;
    }

    internal StringBuilder GetVariableNames( ReadOnlySpan<char> format ) => GetVariableNames(int.TryParse(format, out int indentLevel)
                                                                                                 ? indentLevel
                                                                                                 : 1);
    internal StringBuilder GetVariableNames( int indentLevel )
    {
        if ( !IsGrouped ) { return Table.VariableNames(indentLevel); }

        int           length = Table.MaxLength_ColumnName * Table.Count + Parameters.Sum(static x => x.ParameterName.Length + 10);
        StringBuilder sb     = new(length);
        int           index  = 0;
        int           count  = Count;

        for ( int i = 0; i < Groups.Count; i++ )
        {
            indentLevel--;
            sb.Append(' ', indentLevel * 4).Append("(");
            indentLevel++;

            foreach ( NpgsqlParameter column in Groups[i] )
            {
                sb.Append(' ', indentLevel * 4).Append('@').Append(column.ParameterName).Append('_').Append(i);

                if ( index++ < count - 1 ) { sb.Append(",\n"); }
            }

            sb.Append(")");
            if ( i < Groups.Count ) { sb.Append(",\n"); }
        }

        return sb;
    }

    public StringBuilder GetColumnNames( ReadOnlySpan<char> format ) => GetColumnNames(int.TryParse(format, out int indentLevel)
                                                                                           ? indentLevel
                                                                                           : 1);
    public StringBuilder GetColumnNames( int indentLevel ) => Table.ColumnNames(indentLevel);


    public static PostgresParameters Create<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new(TSelf.MetaData);
    public static PostgresParameters Create<TSelf>( TSelf _ )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new(TSelf.MetaData);
    public static PostgresParameters Create<TSelf>( IEnumerable<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = new(TSelf.MetaData);
        foreach ( TSelf record in records ) { parameters.With(record.ToDynamicParameters()); }

        return parameters;
    }
    public static PostgresParameters Create<TSelf>( params ReadOnlySpan<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = Create<TSelf>(records.Length);
        foreach ( TSelf record in records ) { parameters.With(record.ToDynamicParameters()); }

        return parameters;
    }
    public static PostgresParameters Create<TSelf>( params ReadOnlySpan<NpgsqlParameter> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = Create<TSelf>(records.Length);
        foreach ( NpgsqlParameter record in records ) { parameters.Add(record); }

        return parameters;
    }
    public static PostgresParameters Create<TSelf>( int capacity )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = new(TSelf.MetaData);
        parameters.Params.EnsureCapacity(capacity);
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
        where T : struct, Enum => Add(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
    public PostgresParameters Add( string propertyName, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default ) =>
        Add(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
    public PostgresParameters Add( NpgsqlParameter parameter )
    {
        Params.Add(parameter);
        return this;
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
