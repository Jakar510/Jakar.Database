// Jakar.Extensions :: Jakar.Database
// 09/29/2022  8:03 PM

namespace Jakar.Database;


public class DbSqlException( string sql, Exception? inner = null, PostgresParameters? parameters = null, string? message = null ) : Exception(message, inner)
{
    public readonly PostgresParameters? Parameters = parameters;
    public readonly string?             SQL        = sql;
    public          string?             RollbackID { get; init; }
    public override string              Message    => field ??= GetMessage(base.Message, SQL, Parameters);


    public DbSqlException( SqlCommand command, Exception? inner = null ) : this(command.SQL, inner, command.Parameters) { }


    public static string GetMessage( string? title, string? sql, in PostgresParameters? dynamicParameters )
    {
        title ??= "An error occurred with the following sql statement";
        string parameters;

        if ( dynamicParameters is null || dynamicParameters.Value.Count == 0 ) { parameters = "NONE"; }
        else
        {
            using PooledArray<string> buffer = dynamicParameters.Value.ParameterNameArray;
            parameters = string.Join(",\n        ", buffer.Span);
        }

        return $"""
                {title}
                  
                    {nameof(SQL)}:    
                {sql}

                
                    {nameof(Parameters)}:   
                        {parameters}
                """;
    }
}
