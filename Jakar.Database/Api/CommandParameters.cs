// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using ZLinq.Linq;



namespace Jakar.Database;


public readonly struct CommandParameters() : IEquatable<CommandParameters>
{
    // internal readonly List<SqlParameter>                    parameters = [];
    // internal readonly List<ImmutableArray<SqlParameter>>    Extras     = [];
    private readonly List<SqlParameter>                 __parameters = [];
    private readonly List<ImmutableArray<SqlParameter>> __groups     = [];
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
    private List<SqlParameter> __Params
    {
        get
        {
            __parameters.Sort(Comparer<SqlParameter>.Default);
            return __parameters;
        }
    }
    public ReadOnlySpan<SqlParameter>                 Values => __Params.AsSpan();
    public ReadOnlySpan<ImmutableArray<SqlParameter>> Groups => __groups.AsSpan();
    public ReadOnlySpan<SqlParameter> this[ int                                groupIndex ] => Groups[groupIndex].AsSpan();
    public ValueEnumerable<ListWhere<SqlParameter>, SqlParameter> this[ string propertyName ] => ColumnsFor(propertyName);
    public int Count          => __parameters.Count;
    public int ParameterCount => __parameters.Count + Groups.Sum(static x => x.Length);
    public ArrayBuffer<SqlParameter> Parameters
    {
        [Pure] [MustDisposeResource] get
        {
            ArrayBuffer<SqlParameter> buffer = new(ParameterCount);
            foreach ( ref readonly SqlParameter parameter in Values ) { buffer.Add(in parameter); }

            foreach ( ref readonly ImmutableArray<SqlParameter> array in Groups )
            {
                foreach ( ref readonly SqlParameter parameter in array.AsSpan() ) { buffer.Add(in parameter); }
            }

            buffer.Span.Sort(Comparer<SqlParameter>.Default);
            return buffer;
        }
    }
    public   int                 Capacity            => __parameters.Capacity;
    public   bool                IsGrouped           => __groups.Count > 0;
    public   ParameterNames      ParameterNames      { [Pure] [MustDisposeResource] get => new(this); }
    public   ExtraParameterNames GroupParameterNames { [Pure] [MustDisposeResource] get => new(this); }
    internal int                 VariableNameLength  => Table.MaxLength_ColumnName * Table.ColumnCount + Parameters.Sum(static x => x.ParameterName.Length + 10);
    public   IndexedEnumerator   IndexedParameters   => new(this);


    internal int           KeyValuePairLength( int indentLevel )                                      => Table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + ParameterCount * ( indentLevel * 4 + 3 );
    public   ColumnNames   ColumnNames( int        indentLevel )                                      => new(this, indentLevel);
    public   VariableNames VariableNames( int      indentLevel )                                      => new(this, indentLevel);
    public   KeyValuePairs KeyValuePairs( int      indentLevel, params ReadOnlySpan<char> separator ) => new(this, indentLevel, separator);


    public static CommandParameters Create<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new() { Table = TSelf.MetaData };
    public static CommandParameters Create<TSelf>( IEnumerable<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = Create<TSelf>();
        foreach ( TSelf record in records ) { parameters.AddGroup(record); }

        return parameters;
    }
    public static CommandParameters Create<TSelf>( params ReadOnlySpan<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = Create<TSelf>(records.Length);
        foreach ( TSelf record in records ) { parameters.AddGroup(record); }

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
        CommandParameters parameters = Create<TSelf>();
        parameters.__parameters.EnsureCapacity(capacity);
        return parameters;
    }


    public ValueEnumerable<ListWhere<SqlParameter>, SqlParameter> ColumnsFor( string propertyName ) => __Params.AsValueEnumerable().Where(x => string.Equals(x.Column.PropertyName, propertyName, StringComparison.InvariantCulture));


    public CommandParameters AddGroup<TSelf>( TSelf other )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = other.ToDynamicParameters();
        return AddGroup(in parameters);
    }
    public CommandParameters AddGroup( in CommandParameters other )
    {
        SqlParameter[] array = [..other.Values];
        Array.Sort(array, Comparer<SqlParameter>.Default);
        __groups.Add(array.AsImmutableArray());
        return this;
    }


    internal bool AddInternal( ref readonly SqlParameter parameter )
    {
        foreach ( ref readonly SqlParameter existing in Values )
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
        SqlParameter parameter = Table[propertyName].ToParameter(value.ID, parameterName, direction, sourceVersion);
        AddInternal(in parameter);
        return this;
    }
    public CommandParameters Add<TSelf>( string propertyName, RecordID<TSelf> value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        SqlParameter parameter = Table[propertyName].ToParameter(value.Value, parameterName, direction, sourceVersion);
        AddInternal(in parameter);
        return this;
    }
    public CommandParameters Add<T>( string propertyName, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : struct, Enum
    {
        SqlParameter parameter = Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion);
        AddInternal(in parameter);
        return this;
    }
    public CommandParameters Add( string propertyName, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        SqlParameter parameter = Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion);
        AddInternal(in parameter);
        return this;
    }


    public override int GetHashCode() => __id.GetHashCode();
    public ulong GetHash64()
    {
        using ParameterNames buffer = ParameterNames;
        ReadOnlySpan<string> names  = buffer.Span;
        return Hashes.Hash(in names);
    }
    public UInt128 GetHash128()
    {
        using ParameterNames buffer = ParameterNames;
        ReadOnlySpan<string> names  = buffer.Span;
        return Hashes.Hash128(in names);
    }


    public override string ToString()                                                             => ( __parameters, __groups ).ToJson();
    public          bool   Equals( CommandParameters              other )                         => Equals(in other);
    public          bool   Equals( ref readonly CommandParameters other )                         => __id.Equals(other.__id);
    public override bool   Equals( object?                        obj )                           => obj is CommandParameters other && Equals(other);
    public static   bool operator ==( CommandParameters           left, CommandParameters right ) => left.Equals(right);
    public static   bool operator !=( CommandParameters           left, CommandParameters right ) => !left.Equals(right);
}
