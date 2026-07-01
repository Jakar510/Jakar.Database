// Jakar.Database :: Jakar.Database
// 02/20/2026  13:55

namespace Jakar.Database;


public readonly record struct PropertyColumn( string PropertyName, ColumnMetaData Column )
{
    public readonly                 ColumnMetaData Column       = Column;
    public readonly                 string         PropertyName = PropertyName;
    public static implicit operator ColumnMetaData( PropertyColumn                       value ) => value.Column;
    public static implicit operator PropertyColumn( KeyValuePair<string, ColumnMetaData> pair )  => new(pair.Key, pair.Value);
}



public ref struct TableMetaDataEnumerator( ITableMetaData metaData ) : IValueEnumerator<PropertyColumn>, IEnumerator<PropertyColumn>
{
    private readonly int __count = metaData.ColumnCount;
    private          int __index = -1;


    public PropertyColumn Current => metaData[__index];
    object IEnumerator.   Current => Current;


    public bool MoveNext()
    {
        __index++;
        return (uint)__index < (uint)__count;
    }
    public ValueEnumerable<TableMetaDataEnumerator, PropertyColumn> GetEnumerator() => new(this);
    public void                                                     Reset()         => __index = -1;
    public bool TryGetNext( out PropertyColumn current )
    {
        if ( MoveNext() )
        {
            current = Current;
            return true;
        }

        __index = -1;
        current = default;
        return false;
    }
    public bool TryGetNonEnumeratedCount( out int count )
    {
        count = __count;
        return true;
    }
    public bool TryGetSpan( out ReadOnlySpan<PropertyColumn> span )
    {
        span = default;
        return false;
    }
    public bool TryCopyTo( scoped Span<PropertyColumn> destination, Index offset )
    {
        for ( int i = offset.Value; i < __count; i++ ) { destination[i] = metaData[i]; }

        return true;
    }
    public void Dispose() { }
}
