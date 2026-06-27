// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

using System.Collections;



namespace Jakar.SqlBuilder;


/// <summary> The ordered set of bound parameters produced alongside a built statement. </summary>
public sealed class SqlParameterSet : IReadOnlyList<SqlParameter>
{
    private readonly SqlParameter[] __parameters;

    public static SqlParameterSet Empty { get; } = new([]);

    public int          Count             => __parameters.Length;
    public bool         IsEmpty           => __parameters.Length is 0;
    public SqlParameter this[ int index ] => __parameters[index];

    internal SqlParameterSet( SqlParameter[] parameters ) => __parameters = parameters;

    public IEnumerator<SqlParameter> GetEnumerator()
    {
        foreach ( SqlParameter parameter in __parameters ) { yield return parameter; }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Dictionary<string, object?> ToDictionary()
    {
        Dictionary<string, object?> result = new(__parameters.Length);
        foreach ( SqlParameter parameter in __parameters ) { result[parameter.Name] = parameter.Value; }

        return result;
    }
}
