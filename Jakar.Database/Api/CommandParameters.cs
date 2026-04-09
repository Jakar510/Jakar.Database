// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using ZLinq.Linq;



namespace Jakar.Database;


public readonly struct CommandParameters() : IEquatable<CommandParameters>
{
    // internal readonly List<SqlParameter>                    parameters = [];
    // internal readonly List<ImmutableArray<SqlParameter>>    Extras     = [];
    private readonly List<SqlParameter>                 __parameters = [];
    private readonly List<ImmutableArray<SqlParameter>> __extras     = [];
    private readonly UInt128                            __id         = new((ulong)Random.Shared.NextInt64(), (ulong)Random.Shared.NextInt64());


    public required ITableMetaData Table
    {
        get;
        init
        {
            field = value;
            __parameters.EnsureCapacity(value.ColumnCount);
        }
    }
    public ReadOnlySpan<SqlParameter> Values
    {
        get
        {
            __parameters.Sort(Comparer<SqlParameter>.Default);
            return __parameters.AsSpan();
        }
    }
    public ReadOnlySpan<ImmutableArray<SqlParameter>> Extras                   => __extras.AsSpan();
    public ReadOnlySpan<SqlParameter>                 ExtraValues( int index ) => Extras[index].AsSpan();
    public int                                        Count                    => __parameters.Count;
    public int                                        ParameterCount           => __parameters.Count + Extras.Sum(static x => x.Length);
    public ArrayBuffer<SqlParameter> Parameters
    {
        [Pure] [MustDisposeResource] get
        {
            ArrayBuffer<SqlParameter> buffer = new(ParameterCount);
            foreach ( ref readonly SqlParameter parameter in Values ) { buffer.Add(in parameter); }

            foreach ( ref readonly ImmutableArray<SqlParameter> array in Extras )
            {
                foreach ( ref readonly SqlParameter parameter in array.AsSpan() ) { buffer.Add(in parameter); }
            }

            buffer.Span.Sort(Comparer<SqlParameter>.Default);
            return buffer;
        }
    }
    public   int                                     Capacity           => __parameters.Capacity;
    public   bool                                    IsGrouped          => __extras.Count > 0;
    public   ValueEnumerable<ParameterNames, string> ParameterNames     { [Pure] get => new(new ParameterNames(this)); }
    public   int                                     SpacerCount        => Math.Max(__parameters.Count, Table.ColumnCount) - 1;
    internal int                                     VariableNameLength => Table.MaxLength_ColumnName * Table.ColumnCount  + Parameters.Sum(static x => x.ParameterName.Length + 10);
    public   IndexedEnumerator                       IndexedParameters  => new(this);


    internal int KeyValuePairLength( int indentLevel ) => Table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + ParameterCount * ( indentLevel * 4 + 3 );


    public ColumnNames   ColumnNames( int   indentLevel )                                      => new(this, indentLevel);
    public VariableNames VariableNames( int indentLevel )                                      => new(this, indentLevel);
    public KeyValuePairs KeyValuePairs( int indentLevel, params ReadOnlySpan<char> separator ) => new(this, indentLevel, separator);


    public static CommandParameters Create<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new() { Table = TSelf.MetaData };
    public static CommandParameters Create<TSelf>( IEnumerable<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = new() { Table = TSelf.MetaData };
        foreach ( TSelf record in records ) { parameters.With(record.ToDynamicParameters()); }

        return parameters;
    }
    public static CommandParameters Create<TSelf>( params ReadOnlySpan<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = Create<TSelf>(records.Length);
        foreach ( TSelf record in records ) { parameters.With(record.ToDynamicParameters()); }

        return parameters;
    }
    public static CommandParameters Create<TSelf>( params ReadOnlySpan<SqlParameter> parameters )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters result = Create<TSelf>(parameters.Length);
        foreach ( ref readonly SqlParameter record in parameters ) { result.AddInternal(in record); }

        return result;
    }
    public static CommandParameters Create<TSelf>( int capacity )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = new() { Table = TSelf.MetaData };
        parameters.__parameters.EnsureCapacity(capacity);
        return parameters;
    }


    public ValueEnumerable<Where<FromSpan<SqlParameter>, SqlParameter>, SqlParameter> ColumnsFor( string propertyName ) => Values.AsValueEnumerable().Where(x => string.Equals(x.Column.PropertyName, propertyName, StringComparison.InvariantCulture));


    public CommandParameters With( in CommandParameters other )
    {
        SqlParameter[] array = [..other.Values];
        Array.Sort(array, Comparer<SqlParameter>.Default);
        __extras.Add(array.AsImmutableArray());
        return this;
    }


    internal bool AddInternal( in SqlParameter parameter )
    {
        foreach ( ref readonly SqlParameter existing in __parameters.AsSpan() )
        {
            if ( parameter.Equals(in existing) ) { return false; }
        }

        __parameters.Add(parameter);
        return true;
    }
    internal void AddInternal( params ReadOnlySpan<SqlParameter> parameters )
    {
        foreach ( ref readonly SqlParameter record in parameters ) { AddInternal(in record); }
    }


    public CommandParameters Add<TSelf>( string propertyName, IRecordID value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        AddInternal(Table[propertyName].ToParameter(value.ID, parameterName, direction, sourceVersion));
        return this;
    }
    public CommandParameters Add<TSelf>( string propertyName, RecordID<TSelf> value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        AddInternal(Table[propertyName].ToParameter(value.Value, parameterName, direction, sourceVersion));
        return this;
    }
    public CommandParameters Add<T>( string propertyName, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : struct, Enum
    {
        AddInternal(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
        return this;
    }
    public CommandParameters Add( string propertyName, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        AddInternal(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
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

    public override string ToString()                                                             => __parameters.ToJson();
    public          bool   Equals( CommandParameters              other )                         => Equals(in other);
    public          bool   Equals( ref readonly CommandParameters other )                         => __id.Equals(other.__id);
    public override bool   Equals( object?                        obj )                           => obj is CommandParameters other && Equals(other);
    public static   bool operator ==( CommandParameters           left, CommandParameters right ) => left.Equals(right);
    public static   bool operator !=( CommandParameters           left, CommandParameters right ) => !left.Equals(right);
}
