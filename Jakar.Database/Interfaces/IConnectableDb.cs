// Jakar.Extensions :: Jakar.Database
// 08/18/2022  10:38 PM


namespace Jakar.Database;


public interface IConnectableDb : IAsyncDisposable
{
    public IsolationLevel TransactionIsolationLevel { get; }


    public ValueTask<DbConnectionContext> ConnectAsync( CancellationToken token, IsolationLevel? level = null );
}



public interface IConnectableDbRoot : IConnectableDb
{
    public ref readonly DbOptions Options { get; }
    public IAsyncEnumerable<TSelf> Where<TSelf>( DbConnectionContext context, string sql, CommandParameters parameters, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>, IDateCreated;
    public IAsyncEnumerable<TValue> Where<TSelf, TValue>( DbConnectionContext context, string sql, CommandParameters parameters, [EnumeratorCancellation] CancellationToken token = default )
        where TValue : struct
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>;
}
