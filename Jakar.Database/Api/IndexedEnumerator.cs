namespace Jakar.Database;


public ref struct IndexedEnumerator( PostgresParameters self )
{
    private int                __index      = -2;
    private PostgresParameters __parameters = self;
    public  Set                Current { get; private set; }

    public bool MoveNext()
    {
        int index = Interlocked.Increment(ref __index);

        switch ( index )
        {
            case < -1:
                return false;

            case -1:
                Current = new Set(0, __parameters.parameters.AsSpan());
                return true;

            case >= 0 when index < __parameters.Extras.Count:
                Current = new Set(__index, __parameters.Extras[index].AsSpan());
                return true;

            default:
                Current = default;
                return false;
        }
    }

    public void Reset()
    {
        Current = default;
        Interlocked.Exchange(ref __index, -2);
    }
    public void Dispose()
    {
        __parameters = default;
        Current      = default;
        Interlocked.Exchange(ref __index, -2);
    }
    public IndexedEnumerator GetEnumerator() => this;



    public readonly ref struct Set( int index, ReadOnlySpan<NpgsqlParameter> span )
    {
        public readonly int                           Index = index;
        public readonly ReadOnlySpan<NpgsqlParameter> Span  = span;
        public void Deconstruct( out int index, out ReadOnlySpan<NpgsqlParameter> span )
        {
            index = Index;
            span  = Span;
        }
    }
}