// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> The output of a built statement: the SQL text and its bound parameters. </summary>
public readonly struct SqlResult
{
    public string          Sql        { get; }
    public SqlParameterSet Parameters { get; }
    public SqlDialectKind  Dialect    { get; }

    internal SqlResult( string sql, SqlParameterSet parameters, SqlDialectKind dialect )
    {
        Sql        = sql;
        Parameters = parameters;
        Dialect    = dialect;
    }

    public override string ToString() => Sql;
    public static implicit operator string( SqlResult result ) => result.Sql;

    public void Deconstruct( out string sql, out SqlParameterSet parameters )
    {
        sql        = Sql;
        parameters = Parameters;
    }
}
