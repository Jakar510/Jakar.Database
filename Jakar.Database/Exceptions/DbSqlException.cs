// Jakar.Extensions :: Jakar.Database
// 09/29/2022  8:03 PM

namespace Jakar.Database;


public class DbSqlException( string sql, Exception? inner = null, PostgresParameters? parameters = null, string? message = null ) : Exception(message, inner)
{
    public readonly PostgresParameters? Parameters = parameters;
    public readonly string?             SQL        = sql;
    public          string?             RollbackID { get; init; }
    public override string              Message    => field ??= GetMessage(SQL, Parameters);


    public DbSqlException( SqlCommand command, Exception? inner = null ) : this(command.SQL, inner, command.Parameters) { }


    public string GetMessage( string? sql, in PostgresParameters? dynamicParameters )
    {
        string parameters;

        if ( dynamicParameters is null || dynamicParameters.Value.Count == 0 ) { parameters = "NONE"; }
        else
        {
            StringBuilder sb = new(dynamicParameters.Value.Parameters.Sum(static x => x.ParameterName.Length + 1));
            foreach ( string name in dynamicParameters.Value.Parameters.Select(static x => x.ParameterName) ) { sb.Append(name).Append(','); }

            sb.Length--; // remove trailing ','
            parameters = sb.ToString();
        }

        return $"""
                {base.Message}
                  An error occurred with the following sql statement
                    {nameof(SQL)}:    
                {sql}

                    {nameof(Parameters)}:   
                {parameters}
                """;
    }
}
