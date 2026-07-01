// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:11 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask<ErrorOrResult<TSelf>> Single( RecordID<TSelf>          id,  CancellationToken token                               = default ) => this.Call(Single, id,  token);
    public ValueTask<ErrorOrResult<TSelf>> Single( string                   sql, CommandParameters parameters, CancellationToken token = default ) => this.Call(Single, sql, parameters, token);
    public ValueTask<ErrorOrResult<TSelf>> SingleOrDefault( RecordID<TSelf> id,  CancellationToken token                               = default ) => this.Call(SingleOrDefault, id,  token);
    public ValueTask<ErrorOrResult<TSelf>> SingleOrDefault( string          sql, CommandParameters parameters, CancellationToken token = default ) => this.Call(SingleOrDefault, sql, parameters, token);


    public ValueTask<ErrorOrResult<TSelf>> Single( DbConnectionContext context, RecordID<TSelf> id,  CancellationToken token                               = default ) => Single(context, SqlCommand.Get(in id),              token);
    public ValueTask<ErrorOrResult<TSelf>> Single( DbConnectionContext context, string          sql, CommandParameters parameters, CancellationToken token = default ) => Single(context, SqlCommand.Create(sql, parameters), token);
    public virtual async ValueTask<ErrorOrResult<TSelf>> Single( DbConnectionContext context, SqlCommand command, CancellationToken token = default )
    {
        try
        {
            await using DbCommand    cmd    = command.ToCommand(context);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            ErrorOrResult<TSelf>     record = await reader.SingleAsync<TSelf>(token);
            return record;
        }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }


    public ValueTask<ErrorOrResult<TSelf>> SingleOrDefault( DbConnectionContext context, RecordID<TSelf> id,  CancellationToken token                               = default ) => SingleOrDefault(context, SqlCommand.Get(in id),              token);
    public ValueTask<ErrorOrResult<TSelf>> SingleOrDefault( DbConnectionContext context, string          sql, CommandParameters parameters, CancellationToken token = default ) => SingleOrDefault(context, SqlCommand.Create(sql, parameters), token);
    public virtual async ValueTask<ErrorOrResult<TSelf>> SingleOrDefault( DbConnectionContext context, SqlCommand command, CancellationToken token = default )
    {
        try
        {
            await using DbCommand    cmd    = command.ToCommand(context);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            ErrorOrResult<TSelf>     record = await reader.SingleOrDefaultAsync<TSelf>(token);
            return record;
        }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }
}
