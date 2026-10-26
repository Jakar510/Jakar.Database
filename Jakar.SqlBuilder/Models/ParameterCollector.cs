// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

using System.Buffers;



namespace Jakar.SqlBuilder;


/// <summary> Collects bound parameter values and assigns sequential ordinals into a pooled array. A mutable struct carried by value inside <see cref="SqlWriter"/>. </summary>
internal struct ParameterCollector()
{
    private object?[]? __values = null;
    private int        __count  = 0;
    private int        __next   = 0;

    /// <summary> Every ordinal handed out was stored, in order (<c>0 .. n-1</c> with none skipped). </summary>
    public readonly bool IsContiguous => __count == __next;
    public readonly int  Count        => __count;

    public int Add( object? value )
    {
        __values ??= ArrayPool<object?>.Shared.Rent(4);
        if ( __count >= __values.Length )
        {
            object?[] grown = ArrayPool<object?>.Shared.Rent(__values.Length * 2);
            Array.Copy(__values, grown, __count);
            ArrayPool<object?>.Shared.Return(__values, true);
            __values = grown;
        }

        int ordinal = __next++;
        __values[__count++] = value;
        return ordinal;
    }

    public readonly SqlParameterSet Build( SqlDialectKind dialect, bool namedPostgresParameters )
    {
        if ( __count is 0 ) { return SqlParameterSet.Empty; }

        SqlParameter[] result = new SqlParameter[__count];
        for ( int ordinal = 0; ordinal < __count; ordinal++ ) { result[ordinal] = new SqlParameter(dialect.ParameterName(ordinal, namedPostgresParameters), __values![ordinal]); }

        return new SqlParameterSet(result, dialect);
    }

    public void Release()
    {
        if ( __values is not null ) { ArrayPool<object?>.Shared.Return(__values, true); }

        __values = null;
    }
}
