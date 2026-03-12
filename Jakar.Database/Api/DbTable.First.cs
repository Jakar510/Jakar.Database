// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:11 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask<ErrorOrResult<TSelf>> First( CancellationToken          token = default ) => this.Call(First,          token);
    public ValueTask<ErrorOrResult<TSelf>> FirstOrDefault( CancellationToken token = default ) => this.Call(FirstOrDefault, token);


    public virtual async ValueTask<ErrorOrResult<TSelf>> First( DbConnectionContext context, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetFirst<TSelf>();

        try
        {
            await using DbCommand    cmd    = command.ToCommand(context);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            ErrorOrResult<TSelf>         record = await reader.FirstAsync<TSelf>(token);
            return record;
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }
    public virtual async ValueTask<ErrorOrResult<TSelf>> FirstOrDefault( DbConnectionContext context, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetFirst<TSelf>();

        try
        {
            await using DbCommand    cmd    = command.ToCommand(context);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            ErrorOrResult<TSelf>         record = await reader.FirstOrDefaultAsync<TSelf>(token);
            return record;
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }
}
