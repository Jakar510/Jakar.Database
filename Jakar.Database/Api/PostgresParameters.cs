// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using ZLinq.Linq;



namespace Jakar.Database;


public readonly struct PostgresParameters() : IEquatable<PostgresParameters>
{
    internal readonly List<NpgsqlParameter>                 parameters = [];
    internal readonly List<ImmutableArray<NpgsqlParameter>> Extras     = [];


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
    public   ValueEnumerable<FromList<NpgsqlParameter>, NpgsqlParameter>                                                                                                                                  Values                                => parameters.AsValueEnumerable();
    public   int                                                                                                                                                                                          Count                                 => parameters.Count + Extras.AsValueEnumerable().Sum(static x => x.Length);
    public   ValueEnumerable<Union<FromList<NpgsqlParameter>, SelectMany<FromList<ImmutableArray<NpgsqlParameter>>, ImmutableArray<NpgsqlParameter>, NpgsqlParameter>, NpgsqlParameter>, NpgsqlParameter> Parameters                            { [Pure] get => parameters.AsValueEnumerable().Union(Extras.AsValueEnumerable().SelectMany(static x => x)); }
    public   int                                                                                                                                                                                          ParameterCount                        => Parameters.Count();
    public   int                                                                                                                                                                                          Capacity                              => Extras.Capacity;
    public   bool                                                                                                                                                                                         IsGrouped                             => Extras.Count > 0;
    public   ValueEnumerable<ParameterNames, string>                                                                                                                                                      ParameterNames                        { [Pure] get => new(new ParameterNames(this)); }
    public   int                                                                                                                                                                                          SpacerCount                           => Math.Max(parameters.Count, Table.Count)  - 1;
    internal int                                                                                                                                                                                          VariableNameLength                    => Table.MaxLength_ColumnName * Table.Count + Parameters.Sum(static x => x.ParameterName.Length + 10);
    public   IndexedEnumerator                                                                                                                                                                            IndexedParameters                     => new(this);
    internal int                                                                                                                                                                                          KeyValuePairLength( int indentLevel ) => Table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + Count * ( indentLevel * 4 + 3 );


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

    public PostgresParameters With( in PostgresParameters other ) => With([..other.parameters]);
    public PostgresParameters With( in ImmutableArray<NpgsqlParameter> other )
    {
        int i = Extras.Count;
        foreach ( NpgsqlParameter parameter in other ) { parameter.ParameterName = $"{parameter.ParameterName}_{i}"; }

        Extras.Add(other);
        return this;
    }


    public PostgresParameters Add<TSelf>( string propertyName, IRecordID value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Add(Table[propertyName].ToParameter(value.ID, parameterName, direction, sourceVersion));
    public PostgresParameters Add<TSelf>( string propertyName, RecordID<TSelf> value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Add(Table[propertyName].ToParameter(value.Value, parameterName, direction, sourceVersion));
    public PostgresParameters Add<T>( string propertyName, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : struct, Enum => Add(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
    public PostgresParameters Add( string propertyName, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default ) => Add(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
    public PostgresParameters Add( NpgsqlParameter? parameter )
    {
        if ( parameter is null || Values.Where(x => x.ParameterName == parameter.ParameterName).Any(x => Equals(x.Value, parameter.Value)) ) { return this; }

        parameters.Add(parameter);
        return this;
    }


    public override int GetHashCode() => Extras.GetHashCode();
    public ulong GetHash64()
    {
        using ParameterNames array = new(this);
        ReadOnlySpan<string> names = array.Span;
        return Hashes.Hash(in names);
    }
    public UInt128 GetHash128()
    {
        using ParameterNames array = new(this);
        ReadOnlySpan<string> names = array.Span;
        return Hashes.Hash128(in names);
    }


    public          bool Equals( PostgresParameters      other )                          => parameters.Equals(other.parameters);
    public override bool Equals( object?                 obj )                            => obj is PostgresParameters other && Equals(other);
    public static   bool operator ==( PostgresParameters left, PostgresParameters right ) => left.Equals(right);
    public static   bool operator !=( PostgresParameters left, PostgresParameters right ) => !left.Equals(right);
}
