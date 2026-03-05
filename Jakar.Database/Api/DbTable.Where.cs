// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:08 PM


namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public IAsyncEnumerable<TSelf> Where( PostgresParameters parameters, [EnumeratorCancellation] CancellationToken token                                                        = default ) => this.TryCall(Where, parameters,                         token);
    public IAsyncEnumerable<TSelf> Where( string             sql,        PostgresParameters                         parameters, [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(Where, SqlCommand.Create(sql, parameters), token);
    public IAsyncEnumerable<TSelf> Where<TValue>( string     columnName, TValue?                                    value,      [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(Where, columnName,                         value, token);


    public virtual IAsyncEnumerable<TSelf> Where( NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, PostgresParameters parameters, [EnumeratorCancellation] CancellationToken token = default ) => Where(connection, transaction, SqlCommand.Create(sql, parameters), token);
    public virtual IAsyncEnumerable<TSelf> Where( NpgsqlConnection connection, NpgsqlTransaction? transaction, PostgresParameters parameters, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.Get<TSelf>(parameters);
        return Where(connection, transaction, command, token);
    }
    public virtual async IAsyncEnumerable<TSelf> Where( NpgsqlConnection connection, NpgsqlTransaction? transaction, SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using NpgsqlCommand    cmd    = command.ToCommand(connection, transaction);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token);
        await foreach ( TSelf record in reader.CreateAsync<TSelf>(token) ) { yield return record; }
    }
    public virtual IAsyncEnumerable<TSelf> Where( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(Where, command, token);
    public virtual IAsyncEnumerable<TSelf> Where<TValue>( NpgsqlConnection connection, NpgsqlTransaction? transaction, string columnName, TValue? value, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand sql = SqlCommand.Parse<TSelf>($"SELECT * FROM {TSelf.TableName} WHERE @{columnName} = @{value};");
        return Where(connection, transaction, sql, token);
    }


    public IAsyncEnumerable<RecordID<TSelf>> WhereID( PostgresParameters parameters, [EnumeratorCancellation] CancellationToken token                                                        = default ) => this.TryCall(WhereID, parameters,                         token);
    public IAsyncEnumerable<RecordID<TSelf>> WhereID( string             sql,        PostgresParameters                         parameters, [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(WhereID, SqlCommand.Create(sql, parameters), token);
    public IAsyncEnumerable<RecordID<TSelf>> WhereID<TValue>( string     columnName, TValue?                                    value,      [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(WhereID, columnName,                         value, token);


    public virtual IAsyncEnumerable<RecordID<TSelf>> WhereID( NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, PostgresParameters parameters, [EnumeratorCancellation] CancellationToken token = default ) => WhereID(connection, transaction, SqlCommand.Create(sql, parameters), token);
    public virtual IAsyncEnumerable<RecordID<TSelf>> WhereID( NpgsqlConnection connection, NpgsqlTransaction? transaction, PostgresParameters parameters, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.Get<TSelf>(parameters);
        return WhereID(connection, transaction, command, token);
    }
    public virtual async IAsyncEnumerable<RecordID<TSelf>> WhereID( NpgsqlConnection connection, NpgsqlTransaction? transaction, SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using NpgsqlCommand    cmd    = command.ToCommand(connection, transaction);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token);
        await foreach ( TSelf record in reader.CreateAsync<TSelf>(token) ) { yield return record; }
    }
    public virtual IAsyncEnumerable<RecordID<TSelf>> WhereID( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(WhereID, command, token);
    public virtual IAsyncEnumerable<RecordID<TSelf>> WhereID<TValue>( NpgsqlConnection connection, NpgsqlTransaction? transaction, string columnName, TValue? value, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand sql = SqlCommand.Parse<TSelf>($"SELECT {nameof(IUniqueID.ID)} FROM {TSelf.TableName} WHERE @{columnName} = @{value};");
        return WhereID(connection, transaction, sql, token);
    }
}
