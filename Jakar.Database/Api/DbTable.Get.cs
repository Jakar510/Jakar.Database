// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:07 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask<long>                 Count( CancellationToken               token = default )                                                        => this.Call(Count, token);
    public ValueTask<bool>                 Exists( CommandParameters              parameters, CancellationToken token )                                    => this.TryCall(Exists, parameters, token);
    public IAsyncEnumerable<TSelf>         Get( IEnumerable<RecordID<TSelf>>      ids,        CancellationToken token                          = default ) => this.Call(Get, ids,        token);
    public IAsyncEnumerable<TSelf>         Get( IAsyncEnumerable<RecordID<TSelf>> ids,        CancellationToken token                          = default ) => this.Call(Get, ids,        token);
    public ValueTask<ErrorOrResult<TSelf>> Get( CommandParameters                 parameters, CancellationToken token                          = default ) => this.Call(Get, parameters, token);
    public ValueTask<ErrorOrResult<TSelf>> Get( string                            columnName, object?           value, CancellationToken token = default ) => this.Call(Get, columnName, value, token);
    public ValueTask<ErrorOrResult<TSelf>> Get( RecordID<TSelf>                   id,         CancellationToken token = default ) => this.Call(Get, id, token);
    public ValueTask<ErrorOrResult<TSelf>> Get( RecordID<TSelf>?                  id,         CancellationToken token = default ) => this.Call(Get, id, token);


    public virtual async ValueTask<long> Count( DbConnectionContext context, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetCount<TSelf>();

        try { return await context.QueryAsync<long>(command, token).SingleAsync(token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }


    public virtual async ValueTask<bool> Exists( DbConnectionContext context, CommandParameters parameters, CancellationToken token )
    {
        SqlCommand command = SqlCommand.GetExists<TSelf>(parameters);

        try { return await context.QueryAsync<bool>(command, token).SingleAsync(token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }


    public virtual async IAsyncEnumerable<TSelf> Get( DbConnectionContext context, IAsyncEnumerable<RecordID<TSelf>> ids, [EnumeratorCancellation] CancellationToken token = default )
    {
        HashSet<RecordID<TSelf>> set = await ids.ToHashSet(token);
        await foreach ( TSelf record in Get(context, set, token) ) { yield return record; }
    }


    public virtual IAsyncEnumerable<TSelf> Get( DbConnectionContext context, IEnumerable<RecordID<TSelf>> ids, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand sql = SqlCommand.Get(ids);
        return Where(context, sql, token);
    }


    public async ValueTask<ErrorOrResult<TSelf>> Get( DbConnectionContext context, RecordID<TSelf>? id, CancellationToken token = default ) => id.HasValue
                                                                                                                                                   ? await Get(context, id.Value, token)
                                                                                                                                                   : Error.NotFound();
    public async ValueTask<ErrorOrResult<TSelf>> Get( DbConnectionContext context, RecordID<TSelf> id, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.Get(in id);
        return await _cache.GetOrCreateAsync(id.key, ( this, context, command ), factory, Options, token);

        static async ValueTask<ErrorOrResult<TSelf>> factory( (DbTable<TSelf> table, DbConnectionContext context, SqlCommand command) values, CancellationToken cancellationToken )
        {
            ( DbTable<TSelf> table, DbConnectionContext context, SqlCommand command ) = values;

            try
            {
                TSelf? result = null;

                await foreach ( TSelf record in table.Where(context, command, cancellationToken) )
                {
                    if ( result is not null ) { return Error.Conflict(command.SQL); }

                    result = record;
                }

                return result is null
                           ? Error.NotFound(command.SQL)
                           : result;
            }
            catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
        }
    }


    public virtual async ValueTask<ErrorOrResult<TSelf>> Get<T>( DbConnectionContext context, string columnName, T? value, CancellationToken token = default ) =>
        await Get(context, CommandParameters.Create<TSelf>().Add(columnName, value), token);


    public virtual async ValueTask<ErrorOrResult<TSelf>> Get( DbConnectionContext context, CommandParameters parameters, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.Get<TSelf>(in parameters);

        try
        {
            TSelf? result = null;

            await foreach ( TSelf record in Where(context, command, token) )
            {
                if ( result is not null ) { return Error.Conflict(command.SQL); }

                result = record;
            }

            return result is null
                       ? Error.NotFound(command.SQL)
                       : result;
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }
}
