// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:08 PM


namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    public IAsyncEnumerable<TSelf> Where( CommandParameters parameters, [EnumeratorCancellation] CancellationToken token                                                        = default ) => this.TryCall(Where, parameters,                         token);
    public IAsyncEnumerable<TSelf> Where( string             sql,        CommandParameters                         parameters, [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(Where, SqlCommand.Create(sql, parameters), token);
    public IAsyncEnumerable<TSelf> Where<TValue>( string     columnName, TValue?                                    value,      [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(Where, columnName,                         value, token);


    public virtual IAsyncEnumerable<TSelf> Where( DbConnectionContext context, string sql, CommandParameters parameters, [EnumeratorCancellation] CancellationToken token = default ) => Where(context, SqlCommand.Create(sql, parameters), token);
    public virtual IAsyncEnumerable<TSelf> Where( DbConnectionContext context, CommandParameters parameters, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.Get<TSelf>(parameters);
        return Where(context, command, token);
    }
    public virtual async IAsyncEnumerable<TSelf> Where( DbConnectionContext context, SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using DbCommand    cmd    = command.ToCommand(context);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
        await foreach ( TSelf record in reader.CreateAsync<TSelf>(token) ) { yield return record; }
    }
    public virtual IAsyncEnumerable<TSelf> Where( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(Where, command, token);
    public virtual IAsyncEnumerable<TSelf> Where<TValue>( DbConnectionContext context, string columnName, TValue? value, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand sql = SqlCommand.Parse<TSelf>($"SELECT * FROM {TSelf.TableName} WHERE @{columnName} = @{value};");
        return Where(context, sql, token);
    }


    public IAsyncEnumerable<RecordID<TSelf>> WhereID( CommandParameters parameters, [EnumeratorCancellation] CancellationToken token                                                        = default ) => this.TryCall(WhereID, parameters,                         token);
    public IAsyncEnumerable<RecordID<TSelf>> WhereID( string             sql,        CommandParameters                         parameters, [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(WhereID, SqlCommand.Create(sql, parameters), token);
    public IAsyncEnumerable<RecordID<TSelf>> WhereID<TValue>( string     columnName, TValue?                                    value,      [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(WhereID, columnName,                         value, token);


    public virtual IAsyncEnumerable<RecordID<TSelf>> WhereID( DbConnectionContext context, string sql, CommandParameters parameters, [EnumeratorCancellation] CancellationToken token = default ) => WhereID(context, SqlCommand.Create(sql, parameters), token);
    public virtual IAsyncEnumerable<RecordID<TSelf>> WhereID( DbConnectionContext context, CommandParameters parameters, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.Get<TSelf>(parameters);
        return WhereID(context, command, token);
    }
    public virtual async IAsyncEnumerable<RecordID<TSelf>> WhereID( DbConnectionContext context, SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using DbCommand    cmd    = command.ToCommand(context);
        await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
        await foreach ( TSelf record in reader.CreateAsync<TSelf>(token) ) { yield return record; }
    }
    public virtual IAsyncEnumerable<RecordID<TSelf>> WhereID( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default ) => this.TryCall(WhereID, command, token);
    public virtual IAsyncEnumerable<RecordID<TSelf>> WhereID<TValue>( DbConnectionContext context, string columnName, TValue? value, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand sql = SqlCommand.Parse<TSelf>($"SELECT {nameof(IUniqueID.ID)} FROM {TSelf.TableName} WHERE @{columnName} = @{value};");
        return WhereID(context, sql, token);
    }
}
