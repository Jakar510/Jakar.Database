namespace Jakar.Database;


public sealed class TableMetaData( SortedDictionary<string, ColumnMetaData> dictionary )
{
    public static readonly TableMetaData Empty = new(new SortedDictionary<string, ColumnMetaData>());


    public readonly FrozenDictionary<string, int>            Indexes    = CreateIndexes(dictionary);
    public readonly FrozenDictionary<string, ColumnMetaData> Properties = dictionary.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase);


    public int MaxIndexColumnNameLength => dictionary.Values.Max(static x => x.IndexColumnName?.Length ?? 0);
    public int MaxColumnNameLength      => dictionary.Keys.Max(static x => x.Length);
    public int MaxDataTypeLength        => dictionary.Values.Max(static x => x.DataType.Length);
    public int Count                    => Properties.Count;
    public ColumnMetaData this[ string key ] => Properties[key.SqlColumnName()];


    public static implicit operator TableMetaData( SortedDictionary<string, ColumnMetaData> dictionary ) => new(dictionary);


    public SortedDictionary<string, ColumnMetaData>.Enumerator GetEnumerator()                                                            => dictionary.GetEnumerator();
    public bool                                                ContainsKey( string key )                                                  => Properties.ContainsKey(key.SqlColumnName());
    public bool                                                TryGetValue( string key, [MaybeNullWhen(false)] out ColumnMetaData value ) => Properties.TryGetValue(key.SqlColumnName(), out value);


    private static FrozenDictionary<string, int> CreateIndexes( SortedDictionary<string, ColumnMetaData> dictionary )
    {
        if ( dictionary.Count <= 0 ) { return FrozenDictionary<string, int>.Empty; }

        Dictionary<string, int> indexes = new(StringComparer.InvariantCultureIgnoreCase);
        int                     i       = 0;
        foreach ( string key in dictionary.Keys ) { indexes[key] = i++; }

        return indexes.ToFrozenDictionary();
    }
    public static TableMetaData Create<TSelf>()
        where TSelf : ITableRecord<TSelf>
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

        return dictionary;
    }
}
