﻿// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:14 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask<ErrorOrResult<TSelf>> Random( CancellationToken token                                                                                                     = default ) => this.Call(Random, token);
    public IAsyncEnumerable<TSelf>         Random( int               count, [EnumeratorCancellation] CancellationToken token                                                   = default ) => this.Call(Random, count, token);
    public IAsyncEnumerable<TSelf>         Random( UserRecord        user,  int                                        count, [EnumeratorCancellation] CancellationToken token = default ) => this.Call(Random, user,  count, token);


    [MethodImpl(MethodImplOptions.AggressiveOptimization)] public virtual async ValueTask<ErrorOrResult<TSelf>> Random( NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken token = default )
    {
        SqlCommand<TSelf> command = SqlCommand<TSelf>.GetRandom();

        try
        {
            await using NpgsqlCommand    cmd    = command.ToCommand(connection, transaction);
            await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token);
            return await reader.FirstAsync<TSelf>(token);
        }
        catch ( Exception e ) { throw new SqlException<TSelf>(command, e); }
    }


    [MethodImpl(MethodImplOptions.AggressiveOptimization)] public virtual IAsyncEnumerable<TSelf> Random( NpgsqlConnection connection, NpgsqlTransaction? transaction, UserRecord user, int count, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand<TSelf> sql = SqlCommand<TSelf>.GetRandom(user, count);
        return Where(connection, transaction, sql, token);
    }


    [MethodImpl(MethodImplOptions.AggressiveOptimization)] public virtual IAsyncEnumerable<TSelf> Random( NpgsqlConnection connection, NpgsqlTransaction? transaction, int count, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand<TSelf> sql = SqlCommand<TSelf>.GetRandom(count);
        return Where(connection, transaction, sql, token);
    }
}
