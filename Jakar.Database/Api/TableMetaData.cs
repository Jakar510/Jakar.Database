namespace Jakar.Database;


public interface ITableMetaData
{
    public abstract static ITableMetaData    Default                  { get; }
    public                 string            TableName                { [Pure] get; }
    FrozenDictionary<string, int>            Indexes                  { get; }
    FrozenDictionary<string, ColumnMetaData> Properties               { get; }
    int                                      MaxIndexColumnNameLength { get; }
    int                                      MaxColumnNameLength      { get; }
    int                                      MaxDataTypeLength        { get; }
    int                                      Count                    { get; }
    ColumnMetaData this[ string propertyName ] { get; }
    SortedDictionary<string, ColumnMetaData>.Enumerator GetEnumerator();
    bool                                                ContainsKey( string propertyName );
    bool                                                TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value );
}



public sealed class TableMetaData<TSelf>( SortedDictionary<string, ColumnMetaData> dictionary ) : ITableMetaData
    where TSelf : class, ITableRecord<TSelf>
{
    public static readonly TableMetaData<TSelf> Current = Create();


    public static ITableMetaData                           Default                  => Current;
    public        FrozenDictionary<string, int>            Indexes                  { get; } = CreateIndexes(dictionary);
    public        FrozenDictionary<string, ColumnMetaData> Properties               { get; } = dictionary.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase);
    public        int                                      MaxIndexColumnNameLength { get; } = dictionary.Values.Max(static x => x.IndexColumnName?.Length ?? 0);
    public        int                                      MaxColumnNameLength      { get; } = dictionary.Keys.Max(static x => x.Length);
    public        int                                      MaxDataTypeLength        { get; } = dictionary.Values.Max(static x => x.DataType.Length);
    public        int                                      Count                    { get; } = dictionary.Count;
    public        string                                   TableName                { [Pure] get => TSelf.TableName; }
    public ColumnMetaData this[ string propertyName ] => Properties[propertyName.SqlColumnName()];


    public static implicit operator TableMetaData<TSelf>( SortedDictionary<string, ColumnMetaData> dictionary ) => new(dictionary);


    public SortedDictionary<string, ColumnMetaData>.Enumerator GetEnumerator()                                                                     => dictionary.GetEnumerator();
    public bool                                                ContainsKey( string propertyName )                                                  => Properties.ContainsKey(propertyName.SqlColumnName());
    public bool                                                TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value ) => Properties.TryGetValue(propertyName.SqlColumnName(), out value);


    private static FrozenDictionary<string, int> CreateIndexes( SortedDictionary<string, ColumnMetaData> dictionary )
    {
        if ( dictionary.Count <= 0 ) { return FrozenDictionary<string, int>.Empty; }

        Dictionary<string, int> indexes = new(StringComparer.InvariantCultureIgnoreCase);
        int                     i       = 0;
        foreach ( string propertyName in dictionary.Keys ) { indexes[propertyName] = i++; }

        return indexes.ToFrozenDictionary();
    }
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
        foreach ( PropertyInfo property in properties ) { dictionary[property.Name.SqlColumnName()] = ColumnMetaData.Create(property); }

        return new TableMetaData<TSelf>(dictionary);
    }
}
