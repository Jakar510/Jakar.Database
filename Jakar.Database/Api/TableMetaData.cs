using System;



namespace Jakar.Database;


public interface ITableMetaData
{
    public abstract static ITableMetaData    Default                  { get; }
    public                 string            TableName                { [Pure] get; }
    FrozenDictionary<int, string>            Indexes                  { get; }
    FrozenDictionary<string, ColumnMetaData> Properties               { get; }
    int                                      MaxIndexColumnNameLength { get; }
    int                                      MaxColumnNameLength      { get; }
    int                                      MaxDataTypeLength        { get; }
    int                                      Count                    { get; }
    ImmutableArray<string>                   Keys                     { get; }
    ImmutableArray<ColumnMetaData>           Values                   { get; }
    ref readonly ColumnMetaData this[ string              propertyName ] { get; }
    public KeyValuePair<string, ColumnMetaData> this[ int index ] { get; }


    TableMetaDataEnumerator GetEnumerator();
    bool                    ContainsKey( string propertyName );
    bool                    TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value );
}



public ref struct TableMetaDataEnumerator : IEnumerator<KeyValuePair<string, ColumnMetaData>>
{
    private readonly ITableMetaData __table;
    private          int            __index;


    internal TableMetaDataEnumerator( ITableMetaData table )
    {
        __table = table;
        __index = -1;
        Debug.Assert(__table.Keys.Length == __table.Values.Length);
    }

    public readonly KeyValuePair<string, ColumnMetaData> Current => __table[__index];
    object IEnumerator.                                  Current => Current;


    public bool MoveNext()
    {
        __index++;
        if ( (uint)__index < (uint)__table.Keys.Length ) { return true; }

        __index = __table.Keys.Length;
        return false;
    }

    void IEnumerator.Reset()   => __index = -1;
    void IDisposable.Dispose() { }
}



public sealed class TableMetaData<TSelf> : ITableMetaData
    where TSelf : class, ITableRecord<TSelf>
{
    public static readonly TableMetaData<TSelf>                     Instance = Create();
    public readonly        FrozenDictionary<int, string>            Indexes;
    public readonly        FrozenDictionary<string, ColumnMetaData> Properties;


    FrozenDictionary<int, string> ITableMetaData.           Indexes                  => Indexes;
    public static ITableMetaData                            Default                  => Instance;
    FrozenDictionary<string, ColumnMetaData> ITableMetaData.Properties               => Properties;
    public int                                              MaxIndexColumnNameLength { get; }
    public int                                              MaxColumnNameLength      { get; }
    public int                                              MaxDataTypeLength        { get; }
    public int                                              Count                    { get; }
    public ImmutableArray<string>                           Keys                     => Properties.Keys;
    public ImmutableArray<ColumnMetaData>                   Values                   => Properties.Values;
    public string                                           TableName                { [Pure] get => TSelf.TableName; }
    public ref readonly ColumnMetaData this[ string propertyName ] => ref Properties[propertyName];
    public KeyValuePair<string, ColumnMetaData> this[ int index ]
    {
        get
        {
            Guard.IsLessThan((uint)index, (uint)Properties.Keys.Length);
            string         propertyName = Indexes[index];
            ColumnMetaData column       = Properties[propertyName];
            return new KeyValuePair<string, ColumnMetaData>(propertyName, column);
        }
    }


    internal TableMetaData( FrozenDictionary<string, ColumnMetaData> dictionary )
    {
        Indexes                  = CreateIndexes(dictionary);
        Properties               = dictionary;
        MaxIndexColumnNameLength = dictionary.Values.Max(static x => x.IndexColumnName?.Length ?? 0);
        MaxColumnNameLength      = dictionary.Keys.Max(static x => x.Length);
        MaxDataTypeLength        = dictionary.Values.Max(static x => x.DataType.Length);
        Count                    = dictionary.Count;
    }
    public static implicit operator TableMetaData<TSelf>( FrozenDictionary<string, ColumnMetaData> dictionary ) => new(dictionary);


    private static FrozenDictionary<int, string> CreateIndexes( FrozenDictionary<string, ColumnMetaData> dictionary )
    {
        if ( dictionary.Count <= 0 ) { return FrozenDictionary<int, string>.Empty; }

        Dictionary<int, string> indexes = new(EqualityComparer<int>.Default);
        int                     i       = 0;

        foreach ( ( string propertyName, ColumnMetaData _ ) in dictionary.OrderBy(static pair => pair.Value.DbType, PostgresTypeComparer.Instance)
                                                                         .ThenBy(static pair => pair.Value.ColumnName, StringComparer.InvariantCultureIgnoreCase) ) { indexes[i++] = propertyName; }

        return indexes.ToFrozenDictionary();
    }


    public TableMetaDataEnumerator GetEnumerator() => new(this);


    public bool ContainsKey( string propertyName )                                                  => Properties.ContainsKey(propertyName);
    public bool TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value ) => Properties.TryGetValue(propertyName, out value);


    public static TableMetaData<TSelf> Create()
    {
        const BindingFlags ATTRIBUTES = BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty;

        PropertyInfo[] properties = typeof(TSelf).GetProperties(ATTRIBUTES)
                                                 .AsValueEnumerable()
                                                 .Where(static x => !x.HasAttribute<DbIgnoreAttribute>())
                                                 .ToArray();

        if ( properties.Length <= 0 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' does not have any public instance properties that are not marked with the '{nameof(DbIgnoreAttribute)}' attribute."); }

        if ( properties.Count(ColumnMetaData.IsDbKey) != 1 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' should only have one property with the '{typeof(System.ComponentModel.DataAnnotations.KeyAttribute).FullName}' or '{typeof(KeyAttribute).FullName}' attribute."); }


        SortedDictionary<string, ColumnMetaData> dictionary = new(StringComparer.InvariantCultureIgnoreCase);
        foreach ( PropertyInfo property in properties ) { dictionary[property.Name] = ColumnMetaData.Create(property); }

        return new TableMetaData<TSelf>(dictionary.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase));
    }
}
