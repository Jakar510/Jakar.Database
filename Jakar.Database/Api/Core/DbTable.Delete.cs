// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:06 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask Delete( TSelf                             record,     CancellationToken token = default ) => this.TryCall(Delete, record,     token);
    public ValueTask Delete( IEnumerable<TSelf>                records,    CancellationToken token = default ) => this.TryCall(Delete, records,    token);
    public ValueTask Delete( IAsyncEnumerable<TSelf>           records,    CancellationToken token = default ) => this.TryCall(Delete, records,    token);
    public ValueTask Delete( RecordID<TSelf>                   id,         CancellationToken token = default ) => this.TryCall(Delete, id,         token);
    public ValueTask Delete( IEnumerable<RecordID<TSelf>>      ids,        CancellationToken token = default ) => this.TryCall(Delete, ids,        token);
    public ValueTask Delete( IAsyncEnumerable<RecordID<TSelf>> ids,        CancellationToken token = default ) => this.TryCall(Delete, ids,        token);
    public ValueTask Delete( CommandParameters                 parameters, CancellationToken token = default ) => this.TryCall(Delete, parameters, token);


    public virtual ValueTask Delete( DbConnectionContext context, TSelf              record,  CancellationToken token = default ) => Delete(context, record.ID,                        token);
    public virtual ValueTask Delete( DbConnectionContext context, IEnumerable<TSelf> records, CancellationToken token = default ) => Delete(context, records.Select(static x => x.ID), token);
    public async ValueTask Delete( DbConnectionContext context, IAsyncEnumerable<TSelf> records, CancellationToken token = default )
    {
        HashSet<TSelf> ids = await records.ToHashSet(token);
        await Delete(context, ids, token);
    }
    public virtual async ValueTask Delete( DbConnectionContext context, IAsyncEnumerable<RecordID<TSelf>> ids, CancellationToken token = default )
    {
        HashSet<RecordID<TSelf>> records = await ids.ToHashSet(token);
        await Delete(context, records, token);
    }
    public virtual async ValueTask Delete( DbConnectionContext context, RecordID<TSelf> id, CancellationToken token = default )
    {
        SqlCommand            command = SqlCommand.GetDelete(in id);
        await using DbCommand cmd     = command.ToCommand(context);
        await cmd.ExecuteScalarAsync(token);
    }


    public virtual async ValueTask Delete( DbConnectionContext context, IEnumerable<RecordID<TSelf>> ids, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetDelete(ids);

        try
        {
            await using DbCommand cmd = command.ToCommand(context);
            await cmd.ExecuteScalarAsync(token);
        }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }
    public async ValueTask Delete( DbConnectionContext context, CommandParameters parameters, CancellationToken token )
    {
        SqlCommand            command = SqlCommand.GetDelete<TSelf>(parameters);
        await using DbCommand cmd     = command.ToCommand(context);
        await cmd.ExecuteScalarAsync(token);
    }
}
