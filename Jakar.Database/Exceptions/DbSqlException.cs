// Jakar.Extensions :: Jakar.Database
// 09/29/2022  8:03 PM

namespace Jakar.Database;


public class DbSqlException( string sql, Exception? inner = null, CommandParameters? parameters = null, string? message = null ) : Exception(message, inner)
{
    public readonly CommandParameters? Parameters = parameters;
    public readonly string?            SQL        = sql;
    public override string             Message    => field ??= GetMessage(base.Message, SQL, Parameters);
    public          string?            RollbackID { get; init; }


    public DbSqlException( SqlCommand command, Exception? inner = null ) : this(command.SQL, inner, command.Parameters) { }


    public static string GetMessage( string? title, string? sql, in CommandParameters? dynamicParameters )
    {
        title ??= "An error occurred with the following sql statement";
        string parameters;
        string extrasParameters;

        if ( dynamicParameters is null )
        {
            parameters       = "NONE";
            extrasParameters = "NONE";
        }
        else
        {
            if ( dynamicParameters.Value.Count == 0 ) { parameters = "NONE"; }
            else
            {
                using ParameterNames buffer = new(dynamicParameters.Value);
                parameters = string.Join(",\n        ", buffer.Span);
            }

            if ( dynamicParameters.Value.Count == 0 ) { extrasParameters = "NONE"; }
            else
            {
                using ArrayBuffer<string> buffer = dynamicParameters.Value.Extras.AsValueEnumerable().SelectMany(static x => x).Select(static x => x.ParameterName).ToArrayBuffer();
                extrasParameters = string.Join(",\n        ", buffer.Span);
            }
        }


        return $"""
                {title}
                  
                    SQL:    
                {sql}


                    Parameters:
                        {parameters}

                    Extras Parameters:
                        {extrasParameters}
                """;
    }
}
