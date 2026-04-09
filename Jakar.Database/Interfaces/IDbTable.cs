// Jakar.Database :: Jakar.Database
// 02/03/2026  14:11

namespace Jakar.Database;


public interface IDbTable : IConnectableDb
{
    public ITableMetaData    MetaData  { [Pure] get; }
    FusionCacheEntryOptions? Options   { get; set; }
    public string            TableName { [Pure] get; }
}



public interface IDbTable<TSelf> : IDbTable
    where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
{
    RecordGenerator<TSelf> Records { get; }


    public IAsyncEnumerable<TSelf>                   All( CancellationToken                    token                                                                                                                                                                                                                                                        = default );
    public IAsyncEnumerable<TSelf>                   All( DbConnectionContext                  context, [EnumeratorCancellation] CancellationToken                token                                                                                                                                                                                     = default );
    public ValueTask<TResult>                        Call<TResult>( string                     sql,     CommandParameters                                         parameters, Func<SqlMapper.GridReader, CancellationToken, ValueTask<TResult>> func,       CancellationToken                                                 token                         = default );
    public ValueTask<TResult>                        Call<TResult>( DbConnectionContext        context, string                                                    sql,        CommandParameters                                                 parameters, Func<SqlMapper.GridReader, CancellationToken, ValueTask<TResult>> func, CancellationToken token = default );
    public ValueTask<TResult>                        Call<TResult>( SqlCommand                 sql,     Func<DbDataReader, CancellationToken, ValueTask<TResult>> func,       CancellationToken                                                 token                         = default );
    public ValueTask<TResult>                        Call<TResult>( DbConnectionContext        context, SqlCommand                                                command,    Func<DbDataReader, CancellationToken, ValueTask<TResult>>         func, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           Last( CancellationToken                   token                            = default );
    public ValueTask<ErrorOrResult<TSelf>>           Last( DbConnectionContext                 context, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           LastOrDefault( CancellationToken          token                                                                                                                                                                      = default );
    public ValueTask<ErrorOrResult<TSelf>>           LastOrDefault( DbConnectionContext        context,    CancellationToken                          token                                                                                                               = default );
    public IAsyncEnumerable<TSelf>                   Where( CommandParameters                  parameters, [EnumeratorCancellation] CancellationToken token                                                                                                               = default );
    public IAsyncEnumerable<TSelf>                   Where( string                             sql,        CommandParameters                          parameters, [EnumeratorCancellation] CancellationToken token                                                        = default );
    public IAsyncEnumerable<TSelf>                   Where<TValue>( string                     columnName, TValue?                                    value,      [EnumeratorCancellation] CancellationToken token                                                        = default );
    public IAsyncEnumerable<TSelf>                   Where( DbConnectionContext                context,    string                                     sql,        CommandParameters                          parameters, [EnumeratorCancellation] CancellationToken token = default );
    public IAsyncEnumerable<TSelf>                   Where( DbConnectionContext                context,    CommandParameters                          parameters, [EnumeratorCancellation] CancellationToken token = default );
    public IAsyncEnumerable<TSelf>                   Where( DbConnectionContext                context,    SqlCommand                                 command,    [EnumeratorCancellation] CancellationToken token = default );
    public IAsyncEnumerable<TSelf>                   Where( SqlCommand                         command,    [EnumeratorCancellation] CancellationToken token                                                                       = default );
    public IAsyncEnumerable<TSelf>                   Where<TValue>( DbConnectionContext        context,    string                                     columnName, TValue? value, [EnumeratorCancellation] CancellationToken token = default );
    public IAsyncEnumerable<RecordID<TSelf>>         WhereID( CommandParameters                parameters, [EnumeratorCancellation] CancellationToken token                                                                                                               = default );
    public IAsyncEnumerable<RecordID<TSelf>>         WhereID( string                           sql,        CommandParameters                          parameters, [EnumeratorCancellation] CancellationToken token                                                        = default );
    public IAsyncEnumerable<RecordID<TSelf>>         WhereID<TValue>( string                   columnName, TValue?                                    value,      [EnumeratorCancellation] CancellationToken token                                                        = default );
    public IAsyncEnumerable<RecordID<TSelf>>         WhereID( DbConnectionContext              context,    string                                     sql,        CommandParameters                          parameters, [EnumeratorCancellation] CancellationToken token = default );
    public IAsyncEnumerable<RecordID<TSelf>>         WhereID( DbConnectionContext              context,    CommandParameters                          parameters, [EnumeratorCancellation] CancellationToken token = default );
    public IAsyncEnumerable<RecordID<TSelf>>         WhereID( DbConnectionContext              context,    SqlCommand                                 command,    [EnumeratorCancellation] CancellationToken token = default );
    public IAsyncEnumerable<RecordID<TSelf>>         WhereID( SqlCommand                       command,    [EnumeratorCancellation] CancellationToken token                                                                       = default );
    public IAsyncEnumerable<RecordID<TSelf>>         WhereID<TValue>( DbConnectionContext      context,    string                                     columnName, TValue? value, [EnumeratorCancellation] CancellationToken token = default );
    public ValueTask<long>                           Count( CancellationToken                  token                                               = default );
    public ValueTask<long>                           Count( DbConnectionContext                context,    CancellationToken                 token = default );
    public ValueTask<bool>                           Exists( CommandParameters                 parameters, CancellationToken                 token );
    public ValueTask<bool>                           Exists( DbConnectionContext               context,    CommandParameters                 parameters, CancellationToken token );
    public IAsyncEnumerable<TSelf>                   Get( IEnumerable<RecordID<TSelf>>         ids,        CancellationToken                 token                          = default );
    public IAsyncEnumerable<TSelf>                   Get( IAsyncEnumerable<RecordID<TSelf>>    ids,        CancellationToken                 token                          = default );
    public ValueTask<ErrorOrResult<TSelf>>           Get( CommandParameters                    parameters, CancellationToken                 token                          = default );
    public ValueTask<ErrorOrResult<TSelf>>           Get( string                               columnName, object?                           value, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           Get( RecordID<TSelf>                      id,         CancellationToken                 token                                                                                 = default );
    public ValueTask<ErrorOrResult<TSelf>>           Get( RecordID<TSelf>?                     id,         CancellationToken                 token                                                                                 = default );
    public IAsyncEnumerable<TSelf>                   Get( DbConnectionContext                  context,    IAsyncEnumerable<RecordID<TSelf>> ids,        [EnumeratorCancellation] CancellationToken token                          = default );
    public IAsyncEnumerable<TSelf>                   Get( DbConnectionContext                  context,    IEnumerable<RecordID<TSelf>>      ids,        [EnumeratorCancellation] CancellationToken token                          = default );
    public ValueTask<ErrorOrResult<TSelf>>           Get( DbConnectionContext                  context,    RecordID<TSelf>?                  id,         CancellationToken                          token                          = default );
    public ValueTask<ErrorOrResult<TSelf>>           Get( DbConnectionContext                  context,    RecordID<TSelf>                   id,         CancellationToken                          token                          = default );
    public ValueTask<ErrorOrResult<TSelf>>           Get<T>( DbConnectionContext               context,    string                            columnName, T?                                         value, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           Get( DbConnectionContext                  context,    CommandParameters                 parameters, CancellationToken                          token = default );
    public ValueTask                                 Update( TSelf                             record,     CancellationToken                 token                            = default );
    public ValueTask                                 Update( IEnumerable<TSelf>                records,    CancellationToken                 token                            = default );
    public ValueTask                                 Update( ImmutableArray<TSelf>             records,    CancellationToken                 token                            = default );
    public ValueTask                                 Update( IAsyncEnumerable<TSelf>           records,    CancellationToken                 token                            = default );
    public ValueTask                                 Update( DbConnectionContext               context,    ImmutableArray<TSelf>             records, CancellationToken token = default );
    public ValueTask                                 Update( DbConnectionContext               context,    IEnumerable<TSelf>                records, CancellationToken token = default );
    public ValueTask                                 Update( DbConnectionContext               context,    IAsyncEnumerable<TSelf>           records, CancellationToken token = default );
    public ValueTask                                 Update( DbConnectionContext               context,    TSelf                             record,  CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           Next( RecordPair<TSelf>                   pair,       CancellationToken                 token                         = default );
    public ValueTask<ErrorOrResult<TSelf>>           Next( DbConnectionContext                 context,    RecordPair<TSelf>                 pair, CancellationToken token = default );
    public ValueTask<Guid?>                          NextID( RecordPair<TSelf>                 pair,       CancellationToken                 token                         = default );
    public ValueTask<Guid?>                          NextID( DbConnectionContext               context,    RecordPair<TSelf>                 pair, CancellationToken token = default );
    public ValueTask<IEnumerable<RecordPair<TSelf>>> SortedIDs( CancellationToken              token                            = default );
    public ValueTask<IEnumerable<RecordPair<TSelf>>> SortedIDs( DbConnectionContext            context, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           First( CancellationToken                  token                            = default );
    public ValueTask<ErrorOrResult<TSelf>>           First( DbConnectionContext                context, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           FirstOrDefault( CancellationToken         token                                                                                           = default );
    public ValueTask<ErrorOrResult<TSelf>>           FirstOrDefault( DbConnectionContext       context,    CancellationToken                               token                               = default );
    public ValueTask                                 Delete( TSelf                             record,     CancellationToken                               token                               = default );
    public ValueTask                                 Delete( IEnumerable<TSelf>                records,    CancellationToken                               token                               = default );
    public ValueTask                                 Delete( IAsyncEnumerable<TSelf>           records,    CancellationToken                               token                               = default );
    public ValueTask                                 Delete( RecordID<TSelf>                   id,         CancellationToken                               token                               = default );
    public ValueTask                                 Delete( IEnumerable<RecordID<TSelf>>      ids,        CancellationToken                               token                               = default );
    public ValueTask                                 Delete( IAsyncEnumerable<RecordID<TSelf>> ids,        CancellationToken                               token                               = default );
    public ValueTask                                 Delete( CommandParameters                 parameters, CancellationToken                               token                               = default );
    public ValueTask                                 Delete( DbConnectionContext               context,    TSelf                                           record,     CancellationToken token = default );
    public ValueTask                                 Delete( DbConnectionContext               context,    IEnumerable<TSelf>                              records,    CancellationToken token = default );
    public ValueTask                                 Delete( DbConnectionContext               context,    IAsyncEnumerable<TSelf>                         records,    CancellationToken token = default );
    public ValueTask                                 Delete( DbConnectionContext               context,    IAsyncEnumerable<RecordID<TSelf>>               ids,        CancellationToken token = default );
    public ValueTask                                 Delete( DbConnectionContext               context,    RecordID<TSelf>                                 id,         CancellationToken token = default );
    public ValueTask                                 Delete( DbConnectionContext               context,    IEnumerable<RecordID<TSelf>>                    ids,        CancellationToken token = default );
    public ValueTask                                 Delete( DbConnectionContext               context,    CommandParameters                               parameters, CancellationToken token );
    public ValueTask<ErrorOrResult<TSelf>>           Single( RecordID<TSelf>                   id,         CancellationToken                               token                                                             = default );
    public ValueTask<ErrorOrResult<TSelf>>           Single( string                            sql,        CommandParameters                               parameters, CancellationToken token                               = default );
    public ValueTask<ErrorOrResult<TSelf>>           Single( DbConnectionContext               context,    RecordID<TSelf>                                 id,         CancellationToken token                               = default );
    public ValueTask<ErrorOrResult<TSelf>>           Single( DbConnectionContext               context,    string                                          sql,        CommandParameters parameters, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           Single( DbConnectionContext               context,    SqlCommand                                      command,    CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           SingleOrDefault( RecordID<TSelf>          id,         CancellationToken                               token                                                             = default );
    public ValueTask<ErrorOrResult<TSelf>>           SingleOrDefault( string                   sql,        CommandParameters                               parameters, CancellationToken token                               = default );
    public ValueTask<ErrorOrResult<TSelf>>           SingleOrDefault( DbConnectionContext      context,    RecordID<TSelf>                                 id,         CancellationToken token                               = default );
    public ValueTask<ErrorOrResult<TSelf>>           SingleOrDefault( DbConnectionContext      context,    string                                          sql,        CommandParameters parameters, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           SingleOrDefault( DbConnectionContext      context,    SqlCommand                                      command,    CancellationToken token = default );
    public ValueTask<ImmutableArray<TSelf>>          Import( ReadOnlyMemory<TSelf>             records,    CancellationToken                               token                                                     = default );
    public ValueTask<ImmutableArray<TSelf>>          Import( ArrayBuffer<TSelf>                records,    CancellationToken                               token                                                     = default );
    public ValueTask<ImmutableArray<TSelf>>          Import( IEnumerable<TSelf>                records,    CancellationToken                               token                                                     = default );
    public ValueTask<ImmutableArray<TSelf>>          Import( DbConnectionContext               context,    IEnumerable<TSelf>                              records, [EnumeratorCancellation] CancellationToken token = default );
    public ValueTask<ImmutableArray<TSelf>>          Import( DbConnectionContext               context,    [HandlesResourceDisposal] ReadOnlyMemory<TSelf> records, [EnumeratorCancellation] CancellationToken token = default );
    public ValueTask<ImmutableArray<TSelf>>          Import( DbConnectionContext               context,    [HandlesResourceDisposal] ArrayBuffer<TSelf>    records, [EnumeratorCancellation] CancellationToken token = default );
    public ValueTask<ImmutableArray<TSelf>>          Insert( ReadOnlyMemory<TSelf>             records,    CancellationToken                               token                                                                                   = default );
    public ValueTask<ImmutableArray<TSelf>>          Insert( ImmutableArray<TSelf>             records,    CancellationToken                               token                                                                                   = default );
    public ValueTask<ImmutableArray<TSelf>>          Insert( IEnumerable<TSelf>                records,    CancellationToken                               token                                                                                   = default );
    public IAsyncEnumerable<TSelf>                   Insert( IAsyncEnumerable<TSelf>           records,    CancellationToken                               token                                                                                   = default );
    public ValueTask<TSelf>                          Insert( TSelf                             record,     CancellationToken                               token                                                                                   = default );
    public ValueTask<ImmutableArray<TSelf>>          Insert( DbConnectionContext               context,    IEnumerable<TSelf>                              records, [EnumeratorCancellation] CancellationToken token                               = default );
    public ValueTask<ImmutableArray<TSelf>>          Insert( DbConnectionContext               context,    ReadOnlyMemory<TSelf>                           records, [EnumeratorCancellation] CancellationToken token                               = default );
    public ValueTask<ImmutableArray<TSelf>>          Insert( DbConnectionContext               context,    ImmutableArray<TSelf>                           records, [EnumeratorCancellation] CancellationToken token                               = default );
    public IAsyncEnumerable<TSelf>                   Insert( DbConnectionContext               context,    IAsyncEnumerable<TSelf>                         records, [EnumeratorCancellation] CancellationToken token                               = default );
    public ValueTask<TSelf>                          Insert( DbConnectionContext               context,    TSelf                                           record,  CancellationToken                          token                               = default );
    public ValueTask<ErrorOrResult<TSelf>>           TryInsert( DbConnectionContext            context,    TSelf                                           record,  CommandParameters                          parameters, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           InsertOrUpdate( DbConnectionContext       context,    TSelf                                           record,  CommandParameters                          parameters, CancellationToken token = default );
    public ValueTask<ErrorOrResult<TSelf>>           Random( CancellationToken                 token                                                                                                       = default );
    public IAsyncEnumerable<TSelf>                   Random( int                               count,   [EnumeratorCancellation] CancellationToken token                                                   = default );
    public ValueTask<ErrorOrResult<TSelf>>           Random( DbConnectionContext               context, CancellationToken                          token                                                   = default );
    public IAsyncEnumerable<TSelf>                   Random( DbConnectionContext               context, int                                        count, [EnumeratorCancellation] CancellationToken token = default );
}
