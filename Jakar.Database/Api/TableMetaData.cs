namespace Jakar.Database;


public sealed class TableMetaData( FrozenDictionary<string, ColumnMetaData> dictionary )
{
    public static readonly TableMetaData                  Empty = new(FrozenDictionary<string, ColumnMetaData>.Empty);
    public                 ImmutableArray<string>         Keys   => dictionary.Keys;
    public                 ImmutableArray<ColumnMetaData> Values => dictionary.Values;
    public                 int                            Count  => dictionary.Count;
    public ref readonly ColumnMetaData this[ string key ] => ref dictionary[key.SqlColumnName()];

    public static implicit operator TableMetaData( FrozenDictionary<string, ColumnMetaData> dictionary ) => new(dictionary);

    public FrozenDictionary<string, ColumnMetaData>.Enumerator GetEnumerator()                                                            => dictionary.GetEnumerator();
    public bool                                                ContainsKey( string key )                                                  => dictionary.ContainsKey(key.SqlColumnName());
    public bool                                                TryGetValue( string key, [MaybeNullWhen(false)] out ColumnMetaData value ) => dictionary.TryGetValue(key.SqlColumnName(), out value);
}
