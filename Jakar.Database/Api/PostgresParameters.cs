// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using ZLinq.Linq;



namespace Jakar.Database;


public readonly struct PostgresParameters() : IEquatable<PostgresParameters>
{
    internal readonly List<NpgsqlParameter>    parameters = [];
    internal readonly List<PostgresParameters> Extras     = [];


    public bool IsEmpty { [MemberNotNullWhen(true, nameof(Table))] get => Table is not null; }
    public required ITableMetaData Table
    {
        get;
        init
        {
            field = value;
            parameters.EnsureCapacity(value.Count);
        }
    }
    public ValueEnumerable<FromList<NpgsqlParameter>, NpgsqlParameter>                                      Values             => parameters.AsValueEnumerable();
    public int                                                                                              Count              => parameters.Count + Extras.Sum(static x => x.Count);
    public ValueEnumerable<FromEnumerable<NpgsqlParameter>, NpgsqlParameter>                                Parameters         { [Pure] get => parameters.Union(Extras.SelectMany(static x => x.parameters)).AsValueEnumerable(); }
    public int                                                                                              ParameterCount     => Parameters.Count();
    public int                                                                                              Capacity           => Extras.Capacity;
    public bool                                                                                             IsGrouped          => Extras.Count > 1;
    public PooledArray<string>                                                                              ParameterNameArray { [Pure] [MustDisposeResource] get => Parameters.Select(static x => x.ParameterName).Order().ToArrayPool(); }
    public ValueEnumerable<ListSelect<NpgsqlParameter, string>, string>                                     ParameterNames     { [Pure] get => Values.Select(static x => x.ParameterName); }
    public ValueEnumerable<DistinctBy<FromList<NpgsqlParameter>, NpgsqlParameter, string>, NpgsqlParameter> SourceProperties   { [Pure] get => Values.DistinctBy(static x => x.SourceColumn); }
    public int                                                                                              SpacerCount        => Math.Max(parameters.Count, Table.Count) - 1;


    public ColumnNames   ColumnNames( int   indentLevel )                           => new(this, indentLevel);
    public VariableNames VariableNames( int indentLevel )                           => new(this, indentLevel);
    public KeyValuePairs KeyValuePairs( int indentLevel, string separator = "AND" ) => new(this, indentLevel, separator);


    public static PostgresParameters Create<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new() { Table = TSelf.MetaData };
    public static PostgresParameters Create<TSelf>( TSelf _ )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new() { Table = TSelf.MetaData };
    public static PostgresParameters Create<TSelf>( IEnumerable<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = new() { Table = TSelf.MetaData };
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
        PostgresParameters parameters = new() { Table = TSelf.MetaData };
        parameters.parameters.EnsureCapacity(capacity);
        return parameters;
    }


    public ValueEnumerable<ListWhere<NpgsqlParameter>, NpgsqlParameter> ColumnsFor( string propertyName ) => Values.Where(x => string.Equals(x.SourceColumn, propertyName.SqlName(), StringComparison.InvariantCulture));


    public PostgresParameters With( in PostgresParameters other )
    {
        Extras.Add(other);
        return this;
    }


    public PostgresParameters Add<T>( string propertyName, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : struct, Enum => Add(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
    public PostgresParameters Add( string propertyName, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default ) =>
        Add(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
    public PostgresParameters Add( NpgsqlParameter? parameter )
    {
        if ( parameter is null || Values.Where(x => x.ParameterName == parameter.ParameterName).Any(x => Equals(x.Value, parameter.Value)) ) { return this; }

        parameters.Add(parameter);
        return this;
    }


    public override int GetHashCode() => Extras.GetHashCode();
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


    public          bool Equals( PostgresParameters      other )                          => parameters.Equals(other.parameters);
    public override bool Equals( object?                 obj )                            => obj is PostgresParameters other && Equals(other);
    public static   bool operator ==( PostgresParameters left, PostgresParameters right ) => left.Equals(right);
    public static   bool operator !=( PostgresParameters left, PostgresParameters right ) => !left.Equals(right);
}
