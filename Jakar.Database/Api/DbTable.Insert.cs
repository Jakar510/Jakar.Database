// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:09 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    protected static readonly string _copySQL = SqlCommand.GetCopy<TSelf>().SQL;


    public ValueTask<ImmutableArray<TSelf>> Import( ReadOnlyMemory<TSelf> records, CancellationToken token = default ) => this.TryCall(Import, records, token);
    public ValueTask<ImmutableArray<TSelf>> Import( ArrayBuffer<TSelf>    records, CancellationToken token = default ) => this.TryCall(Import, records, token);
    public ValueTask<ImmutableArray<TSelf>> Import( IEnumerable<TSelf>    records, CancellationToken token = default ) => this.TryCall(Import, records, token);
    public virtual async ValueTask<ImmutableArray<TSelf>> Import( DbConnectionContext context, IEnumerable<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        ArrayBuffer<TSelf> array = [..records];
        return await context.ImportAsync(array, token);
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> Import( DbConnectionContext context, [HandlesResourceDisposal] ReadOnlyMemory<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        ArrayBuffer<TSelf> array = records;
        return await context.ImportAsync(array, token);
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> Import( DbConnectionContext context, [HandlesResourceDisposal] ArrayBuffer<TSelf> records, [EnumeratorCancellation] CancellationToken token = default ) => await context.ImportAsync(records, token);


    public ValueTask<ImmutableArray<TSelf>> Insert( ReadOnlyMemory<TSelf>   records, CancellationToken token = default ) => this.TryCall(Insert, records, token);
    public ValueTask<ImmutableArray<TSelf>> Insert( ImmutableArray<TSelf>   records, CancellationToken token = default ) => this.TryCall(Insert, records, token);
    public ValueTask<ImmutableArray<TSelf>> Insert( IEnumerable<TSelf>      records, CancellationToken token = default ) => this.TryCall(Insert, records, token);
    public IAsyncEnumerable<TSelf>          Insert( IAsyncEnumerable<TSelf> records, CancellationToken token = default ) => this.TryCall(Insert, records, token);
    public ValueTask<TSelf>                 Insert( TSelf                   record,  CancellationToken token = default ) => this.TryCall(Insert, record,  token);
    public virtual async ValueTask<ImmutableArray<TSelf>> Insert( DbConnectionContext context, IEnumerable<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        ReadOnlySpan<TSelf> array   = [..records];
        SqlCommand          command = SqlCommand.GetInsert<TSelf>(array);
        return await context.ExecuteAsync<TSelf>(command, token).ToImmutableArray(array.Length, token);
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> Insert( DbConnectionContext context, ReadOnlyMemory<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetInsert<TSelf>(records.Span);
        return await context.ExecuteAsync<TSelf>(command, token).ToImmutableArray(records.Length, token);
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> Insert( DbConnectionContext context, ImmutableArray<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetInsert<TSelf>(records);
        return await context.ExecuteAsync<TSelf>(command, token).ToImmutableArray(records.Length, token);
    }
    public virtual async IAsyncEnumerable<TSelf> Insert( DbConnectionContext context, IAsyncEnumerable<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        await foreach ( TSelf record in records.WithCancellation(token) ) { yield return await Insert(context, record, token); }
    }


    public virtual async ValueTask<TSelf> Insert( DbConnectionContext context, TSelf record, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetInsert(record);

        try
        {
            await using DbCommand    cmd    = command.ToCommand(context);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            RecordID<TSelf>          id;

            if ( await reader.ReadAsync(token) ) { id = RecordID<TSelf>.Create(reader.GetGuid(0)); }
            else { throw new InvalidOperationException($"Insert command did not return the new ID for type {typeof(TSelf).FullName}"); }

            return record.With(id);
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }

    public virtual async ValueTask<ErrorOrResult<TSelf>> TryInsert( DbConnectionContext context, TSelf record, CommandParameters parameters, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.GetTryInsert(record, in parameters);

        try
        {
            await using DbCommand    cmd    = command.ToCommand(context);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            RecordID<TSelf>          id;

            if ( await reader.ReadAsync(token) ) { id = RecordID<TSelf>.Create(reader.GetGuid(0)); }
            else { throw new InvalidOperationException($"Insert command did not return the new ID for type {typeof(TSelf).FullName}"); }

            return record.With(id);
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }


    public virtual async ValueTask<ErrorOrResult<TSelf>> InsertOrUpdate( DbConnectionContext context, TSelf record, CommandParameters parameters, CancellationToken token = default )
    {
        SqlCommand command = SqlCommand.InsertOrUpdate(record, in parameters);

        try
        {
            await using DbCommand    cmd    = command.ToCommand(context);
            await using DbDataReader reader = await cmd.ExecuteReaderAsync(token);
            RecordID<TSelf>          id;

            if ( await reader.ReadAsync(token) ) { id = RecordID<TSelf>.Create(reader.GetGuid(0)); }
            else { throw new InvalidOperationException($"Insert command did not return the new ID for type {typeof(TSelf).FullName}"); }

            return record.With(id);
        }
        catch ( Exception e ) { throw new DbSqlException(command.SQL, e, command.Parameters); }
    }
}
