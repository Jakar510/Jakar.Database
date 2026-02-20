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



public struct TableMetaDataEnumerator( FrozenDictionary<string, ColumnMetaData> dictionary ) : IValueEnumerator<PropertyColumn>, IEnumerator<PropertyColumn>
{
    private PropertyColumn[]? __buffers;
    private int               __index = 0;


    public PropertyColumn Current => EnsureBuffered()[__index];
    object IEnumerator.   Current => Current;


    public bool MoveNext()
    {
        __index++;
        if ( (uint)__index < (uint)dictionary.Count ) { return true; }

        __index = dictionary.Count;
        return false;
    }
    public ValueEnumerable<TableMetaDataEnumerator, PropertyColumn> GetEnumerator() => new(this);
    public void                                                     Reset()         => __index = -1;
    public bool TryGetNext( out PropertyColumn current )
    {
        ReadOnlySpan<PropertyColumn> span = EnsureBuffered();

        if ( __index < dictionary.Count )
        {
            current = span[__index++];
            return true;
        }

        current = default;
        return false;
    }
    public bool TryGetNonEnumeratedCount( out int count )
    {
        count = dictionary.Count;
        return true;
    }
    public bool TryGetSpan( out ReadOnlySpan<PropertyColumn> span )
    {
        span = EnsureBuffered()[..dictionary.Count];
        return true;
    }
    public bool TryCopyTo( scoped Span<PropertyColumn> destination, Index offset )
    {
        ReadOnlySpan<PropertyColumn> source = EnsureBuffered();

        int start = offset.GetOffset(dictionary.Count);
        if ( start >= dictionary.Count ) { return false; }

        source = source.Slice(start, dictionary.Count - start);
        int toCopy = Math.Min(destination.Length, source.Length);
        source[..toCopy].CopyTo(destination);

        return true;
    }
    private ReadOnlySpan<PropertyColumn> EnsureBuffered()
    {
        if ( dictionary.Keys.IsDefaultOrEmpty ) { return ReadOnlySpan<PropertyColumn>.Empty; }

        if ( dictionary.Count == 0 )
        {
            __buffers = [];
            return __buffers;
        }

        __buffers = ArrayPool<PropertyColumn>.Shared.Rent(dictionary.Count);
        int i = 0;
        foreach ( KeyValuePair<string, ColumnMetaData> kvp in dictionary ) { __buffers[i++] = kvp; }

        return __buffers;
    }
    public void Dispose()
    {
        if ( __buffers is null || __buffers.Length == 0 ) { return; }

        ArrayPool<PropertyColumn>.Shared.Return(__buffers);
        __buffers = null;
    }
}
