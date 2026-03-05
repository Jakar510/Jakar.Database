// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:07 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public ValueTask<long>                 Count( CancellationToken               token = default )                                                        => this.Call(Count, token);
    public ValueTask<bool>                 Exists( PostgresParameters             parameters, CancellationToken token )                                    => this.TryCall(Exists, parameters, token);
    public IAsyncEnumerable<TSelf>         Get( IEnumerable<RecordID<TSelf>>      ids,        CancellationToken token                          = default ) => this.Call(Get, ids,        token);
    public IAsyncEnumerable<TSelf>         Get( IAsyncEnumerable<RecordID<TSelf>> ids,        CancellationToken token                          = default ) => this.Call(Get, ids,        token);
    public ValueTask<ErrorOrResult<TSelf>> Get( PostgresParameters                parameters, CancellationToken token                          = default ) => this.Call(Get, parameters, token);
    public ValueTask<ErrorOrResult<TSelf>> Get( string                            columnName, object?           value, CancellationToken token = default ) => this.Call(Get, columnName, value, token);
    public ValueTask<ErrorOrResult<TSelf>> Get( RecordID<TSelf>                   id,         CancellationToken token = default ) => this.Call(Get, id, token);
    public ValueTask<ErrorOrResult<TSelf>> Get( RecordID<TSelf>?                  id,         CancellationToken token = default ) => this.Call(Get, id, token);


    public virtual async ValueTask<long> Count( NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetCount<TSelf>();

        try { return await connection.QueryFirstAsync<long>(command.SQL, command.Parameters, transaction); }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }


    public virtual async ValueTask<bool> Exists( NpgsqlConnection connection, NpgsqlTransaction? transaction, PostgresParameters parameters, CancellationToken token )
    {
        SqlCommand sql = SqlCommand.GetExists<TSelf>(parameters);

        try
        {
            CommandDefinition   command = new(sql.SQL, transaction, cancellationToken: token);
            IEnumerable<string> results = await connection.QueryAsync<string>(command);
            return results.Any();
        }
        catch ( Exception e ) { throw new DbSqlException(sql.SQL, e, sql.Parameters); }
    }


    public virtual async IAsyncEnumerable<TSelf> Get( NpgsqlConnection connection, NpgsqlTransaction? transaction, IAsyncEnumerable<RecordID<TSelf>> ids, [EnumeratorCancellation] CancellationToken token = default )
    {
        HashSet<RecordID<TSelf>> set = await ids.ToHashSet(token);
        await foreach ( TSelf record in Get(connection, transaction, set, token) ) { yield return record; }
    }


    public virtual IAsyncEnumerable<TSelf> Get( NpgsqlConnection connection, NpgsqlTransaction? transaction, IEnumerable<RecordID<TSelf>> ids, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand sql = SqlCommand.Get<TSelf>(ids);
        return Where(connection, transaction, sql, token);
    }


    public async ValueTask<ErrorOrResult<TSelf>> Get( NpgsqlConnection connection, NpgsqlTransaction? transaction, RecordID<TSelf>? id, CancellationToken token = default ) => id.HasValue
                                                                                                                                                                                   ? await Get(connection, transaction, id.Value, token)
                                                                                                                                                                                   : Error.NotFound();
    public async ValueTask<ErrorOrResult<TSelf>> Get( NpgsqlConnection connection, NpgsqlTransaction? transaction, RecordID<TSelf> id, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.Get<TSelf>(in id);
        return await _cache.GetOrCreateAsync(id.key, ( this, connection, transaction, command ), factory, Options, token);

        static async ValueTask<ErrorOrResult<TSelf>> factory( (DbTable<TSelf> table, NpgsqlConnection connection, NpgsqlTransaction? transaction, SqlCommand command) values, CancellationToken cancellationToken )
        {
            ( DbTable<TSelf> table, NpgsqlConnection connection, NpgsqlTransaction? transaction, SqlCommand command ) = values;

            try
            {
                TSelf? result = null;

                await foreach ( TSelf record in table.Where(connection, transaction, command, cancellationToken) )
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


    public virtual async ValueTask<ErrorOrResult<TSelf>> Get<T>( NpgsqlConnection connection, NpgsqlTransaction? transaction, string columnName, T? value, CancellationToken token = default ) =>
        await Get(connection, transaction, PostgresParameters.Create<TSelf>().Add(columnName, value), token);


    public virtual async ValueTask<ErrorOrResult<TSelf>> Get( NpgsqlConnection connection, NpgsqlTransaction? transaction, PostgresParameters parameters, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.Get<TSelf>(parameters);

        try
        {
            TSelf? result = null;

            await foreach ( TSelf record in Where(connection, transaction, command, token) )
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
