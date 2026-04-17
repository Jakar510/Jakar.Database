// Jakar.Extensions :: Jakar.Database
// 10/16/2022  4:54 PM


namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf> : IDbTable<TSelf>
    where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
{
    protected readonly IConnectableDbRoot    _database;
    protected readonly IFusionCache          _cache;
    public static      TSelf[]               Empty                     => [];
    public static      ImmutableArray<TSelf> EmptyArray                => [];
    public static      TableMetaData<TSelf>  MetaData                  { [Pure] get => TSelf.MetaData; }
    public static      FrozenSet<TSelf>      Set                       => FrozenSet<TSelf>.Empty;
    ITableMetaData IDbTable.                 MetaData                  { [Pure] get => MetaData; }
    public FusionCacheEntryOptions?          Options                   { get; set; }
    public RecordGenerator<TSelf>            Records                   => new(this);
    public SqlName                           TableName                 { [Pure] get => TSelf.TableName; }
    public IsolationLevel                    TransactionIsolationLevel => _database.TransactionIsolationLevel;


    public DbTable( IConnectableDbRoot database, IFusionCache cache )
    {
        _database = database;
        _cache    = cache;
        if ( TSelf.TableName != typeof(TSelf).GetTableName() ) { throw new InvalidOperationException($"{TSelf.TableName} != {typeof(TSelf).GetTableName()}"); }
    }
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return default;
    }


    public ValueTask<DbConnectionContext> ConnectAsync( CancellationToken token, IsolationLevel? level = null ) => _database.ConnectAsync(token, level);


    public IAsyncEnumerable<TSelf> All( CancellationToken token = default ) => this.Call(All, token);
    public virtual async IAsyncEnumerable<TSelf> All( DbConnectionContext context, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand               command = SqlCommand.GetAll<TSelf>();
        await using DbCommand    cmd     = command.ToCommand(context);
        await using DbDataReader reader  = await cmd.ExecuteReaderAsync(token);
        await foreach ( TSelf record in reader.CreateAsync<TSelf>(token) ) { yield return record; }
    }


    public ValueTask<TResult> Call<TResult>( string sql, CommandParameters parameters, Func<SqlMapper.GridReader, CancellationToken, ValueTask<TResult>> func, CancellationToken token = default ) => this.TryCall(Call, sql, parameters, func, token);
    public virtual async ValueTask<TResult> Call<TResult>( DbConnectionContext context, string sql, CommandParameters parameters, Func<SqlMapper.GridReader, CancellationToken, ValueTask<TResult>> func, CancellationToken token = default )
    {
        try
        {
            DbConnection                     connection = await context.EnsureConnection(token);
            await using SqlMapper.GridReader reader     = await connection.QueryMultipleAsync(sql, parameters, context.Transaction);
            return await func(reader, token);
        }
        catch ( Exception e ) { throw new DbSqlException(sql, e, parameters); }
    }


    public ValueTask<TResult> Call<TResult>( SqlCommand sql, Func<DbDataReader, CancellationToken, ValueTask<TResult>> func, CancellationToken token = default ) => this.TryCall(Call, sql, func, token);
    public virtual async ValueTask<TResult> Call<TResult>( DbConnectionContext context, SqlCommand command, Func<DbDataReader, CancellationToken, ValueTask<TResult>> func, CancellationToken token = default )
    {
        try
        {
            await using DbCommand    cmd    = command.ToCommand(context);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            return await func(reader, token);
        }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }
}
