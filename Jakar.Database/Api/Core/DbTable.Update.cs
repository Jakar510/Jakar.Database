// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:09 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask Update( TSelf                   record,  CancellationToken token = default ) => this.TryCall(Update, record,  token);
    public ValueTask Update( IEnumerable<TSelf>      records, CancellationToken token = default ) => this.TryCall(Update, records, token);
    public ValueTask Update( ImmutableArray<TSelf>   records, CancellationToken token = default ) => this.TryCall(Update, records, token);
    public ValueTask Update( IAsyncEnumerable<TSelf> records, CancellationToken token = default ) => this.TryCall(Update, records, token);


    public virtual async ValueTask Update( DbConnectionContext context, ImmutableArray<TSelf> records, CancellationToken token = default )
    {
        foreach ( TSelf record in records ) { await Update(context, record, token); }
    }
    public virtual async ValueTask Update( DbConnectionContext context, IEnumerable<TSelf> records, CancellationToken token = default )
    {
        foreach ( TSelf record in records ) { await Update(context, record, token); }
    }


    public virtual async ValueTask Update( DbConnectionContext context, IAsyncEnumerable<TSelf> records, CancellationToken token = default )
    {
        await foreach ( TSelf record in records.WithCancellation(token) ) { await Update(context, record, token); }
    }


    [MethodImpl(MethodImplOptions.AggressiveOptimization)] public virtual async ValueTask Update( DbConnectionContext context, TSelf record, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetUpdate(record);

        try
        {
            await using DbCommand cmd = command.ToCommand(context);
            await cmd.ExecuteNonQueryAsync(token);
        }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }
}
