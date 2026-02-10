// Jakar.Extensions :: Jakar.Database
// 09/29/2022  8:03 PM

namespace Jakar.Database;


public sealed class SqlException<TSelf> : Exception
    where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
{
    public PostgresParameters Parameters { get; init; }
    public string             SQL        { get; init; }

    // [JsonProperty] public string Value => base.ToString();


    public SqlException( string sql, string message ) : this(sql, PostgresParameters.Create<TSelf>(), message) { }
    public SqlException( string sql, in PostgresParameters parameters, string? message = null ) : base(message ?? GetMessage(sql, in parameters))
    {
        SQL        = sql;
        Parameters = parameters;
    }
    public SqlException( string               sql, Exception? inner ) : this(sql, PostgresParameters.Create<TSelf>(), inner) => SQL = sql;
    public SqlException( string               sql, string     message, Exception? inner ) : this(sql, PostgresParameters.Create<TSelf>(), message, inner) { }
    public SqlException( in SqlCommand<TSelf> sql ) : this(sql.SQL, sql.Parameters, GetMessage(sql.SQL,                                         in sql.Parameters)) { }
    public SqlException( in SqlCommand<TSelf> sql, Exception?            inner ) : this(sql.SQL, sql.Parameters, GetMessage(sql.SQL,            in sql.Parameters), inner) { }
    public SqlException( string               sql, in PostgresParameters parameters, Exception? inner ) : this(sql, parameters, GetMessage(sql, in parameters), inner) { }
    public SqlException( string sql, in PostgresParameters parameters, string message, Exception? inner ) : base(message, inner)
    {
        SQL        = sql;
        Parameters = parameters;
    }


    public static string GetMessage( string sql, in PostgresParameters dynamicParameters )
    {
        string parameters;

        if ( dynamicParameters.Count == 0 ) { parameters = "NONE"; }
        else
        {
            StringBuilder sb = new(dynamicParameters.Parameters.Sum(static x => x.ParameterName.Length));

            foreach ( string name in dynamicParameters.Parameters.Select(static x => x.ParameterName) )
            {
                sb.Append(name)
                  .Append(',');
            }

            parameters = sb.ToString();
        }

        return $"""
                An error occurred with the following sql statement

                {nameof(SQL)}:    {sql}

                {nameof(Parameters)}:   {parameters}
                """;
    }
}
