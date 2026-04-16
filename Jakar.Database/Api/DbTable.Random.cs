// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:14 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask<ErrorOrResult<TSelf>> Random( CancellationToken token                                                   = default ) => this.Call(Random, token);
    public IAsyncEnumerable<TSelf>         Random( int               count, [EnumeratorCancellation] CancellationToken token = default ) => this.Call(Random, count, token);


    [MethodImpl(MethodImplOptions.AggressiveOptimization)] public virtual async ValueTask<ErrorOrResult<TSelf>> Random( DbConnectionContext context, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetRandom<TSelf>();

        try { return await context.FirstAsync<TSelf>(command, token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }


    [MethodImpl(MethodImplOptions.AggressiveOptimization)] public virtual IAsyncEnumerable<TSelf> Random( DbConnectionContext context, int count, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand sql = SqlCommand.GetRandom<TSelf>(count);
        return Where(context, sql, token);
    }
}
