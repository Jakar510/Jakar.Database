// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Collects bound parameter values and assigns sequential ordinals. A mutable struct carried by value inside <see cref="SqlWriter"/>. </summary>
internal struct ParameterCollector()
{
    private object?[] __values = [];
    private int       __count  = 0;

    public readonly int Count => __count;

    public int Add( object? value )
    {
        if ( __count >= __values.Length )
        {
            int       size  = __values.Length is 0 ? 4 : __values.Length * 2;
            object?[] grown = new object?[size];
            Array.Copy(__values, grown, __count);
            __values = grown;
        }

        __values[__count] = value;
        return __count++;
    }

    public readonly SqlParameterSet Build( SqlDialectKind dialect )
    {
        if ( __count is 0 ) { return SqlParameterSet.Empty; }

        SqlParameter[] result = new SqlParameter[__count];
        for ( int ordinal = 0; ordinal < __count; ordinal++ ) { result[ordinal] = new SqlParameter(SqlDialects.ParameterName(dialect, ordinal), __values[ordinal]); }

        return new SqlParameterSet(result);
    }
}
