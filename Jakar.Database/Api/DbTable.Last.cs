// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:13 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask<ErrorOrResult<TSelf>> Last( CancellationToken token = default ) => this.Call(Last, token);


    public virtual async ValueTask<ErrorOrResult<TSelf>> Last( NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetLast<TSelf>();

        try
        {
            await using NpgsqlCommand    cmd    = command.ToCommand(connection, transaction);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            return await reader.LastAsync<TSelf>(token);
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }


    public ValueTask<ErrorOrResult<TSelf>> LastOrDefault( CancellationToken token = default ) => this.Call(LastOrDefault, token);


    public virtual async ValueTask<ErrorOrResult<TSelf>> LastOrDefault( NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetLast<TSelf>();

        try
        {
            await using NpgsqlCommand    cmd    = command.ToCommand(connection, transaction);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            return await reader.LastOrDefaultAsync<TSelf>(token);
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }
}
