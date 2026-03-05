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
    public ValueTask Delete( PostgresParameters                parameters, CancellationToken token = default ) => this.TryCall(Delete, parameters, token);


    public virtual ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction? transaction, TSelf              record,  CancellationToken token = default ) => Delete(connection, transaction, record.ID,                        token);
    public virtual ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction? transaction, IEnumerable<TSelf> records, CancellationToken token = default ) => Delete(connection, transaction, records.Select(static x => x.ID), token);
    public async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction? transaction, IAsyncEnumerable<TSelf> records, CancellationToken token = default )
    {
        HashSet<TSelf> ids = await records.ToHashSet(token);
        await Delete(connection, transaction, ids, token);
    }
    public virtual async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction? transaction, IAsyncEnumerable<RecordID<TSelf>> ids, CancellationToken token = default )
    {
        HashSet<RecordID<TSelf>> records = await ids.ToHashSet(token);
        await Delete(connection, transaction, records, token);
    }
    public virtual async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction? transaction, RecordID<TSelf> id, CancellationToken token = default )
    {
        SqlCommand                command = SqlCommand.GetDeleteID<TSelf>(in id);
        await using NpgsqlCommand cmd     = command.ToCommand(connection, transaction);
        await cmd.ExecuteScalarAsync(token);
    }


    public virtual async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction? transaction, IEnumerable<RecordID<TSelf>> ids, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetDelete<TSelf>(ids);

        try
        {
            await using NpgsqlCommand cmd = command.ToCommand(connection, transaction);
            await cmd.ExecuteScalarAsync(token);
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }
    public async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction? transaction, PostgresParameters parameters, CancellationToken token )
    {
        SqlCommand                command = SqlCommand.GetDelete<TSelf>(parameters);
        await using NpgsqlCommand cmd     = command.ToCommand(connection, transaction);
        await cmd.ExecuteScalarAsync(token);
    }
}
