namespace Jakar.Database;


public ref struct ParameterNames( PostgresParameters self ) : IValueEnumerator<ParameterNames, string>
{
    private int                 __index = -1;
    private PooledArray<string> __array;


    public ImmutableArray<string> Array => __array.Array.AsImmutableArray();
    public ReadOnlySpan<string> Span
    {
        get
        {
            if ( __array.Size <= 0 ) { Reset(); }

            return __array.Span;
        }
    }

    public string? Current => __index >= 0 && __index < __array.Size
                                  ? __array.Span[__index]
                                  : null;

    public bool MoveNext()
    {
        if ( __array.Size <= 0 ) { Reset(); }

        int index = Interlocked.Increment(ref __index);
        return index < __array.Size;
    }

    public void Reset()
    {
        __index = 0;
        __array.Dispose();
        __array = self.Parameters.AsValueEnumerable().Select(static x => x.ParameterName).Order().ToArrayPool();
    }
    public void Dispose()
    {
        __array.Dispose();
        __array = default;
    }
    public ParameterNames GetEnumerator() => this;
    public bool TryGetNext( out string current )
    {
        bool result = MoveNext();
        current = Current!;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return result && current is not null;
    }
    public bool TryGetNonEnumeratedCount( out int count )
    {
        count = __array.Size;
        return true;
    }
    public bool TryGetSpan( out ReadOnlySpan<string> span )
    {
        span = __array.Span;
        return true;
    }
    public bool TryCopyTo( scoped Span<string> destination, Index offset ) => __array.Span[offset..].TryCopyTo(destination);
}
