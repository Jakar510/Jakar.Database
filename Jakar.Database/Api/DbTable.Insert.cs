// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:09 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public partial class DbTable<TSelf>
{
    protected static readonly string _copySQL = SqlCommand<TSelf>.GetCopy()
                                                                 .SQL;


    public ValueTask<ImmutableArray<TSelf>> Import( ReadOnlyMemory<TSelf> records, CancellationToken token = default ) => this.TryCall(Import, records, token);
    public ValueTask<ImmutableArray<TSelf>> Import( ImmutableArray<TSelf> records, CancellationToken token = default ) => this.TryCall(Import, records, token);
    public ValueTask<ImmutableArray<TSelf>> Import( IEnumerable<TSelf>    records, CancellationToken token = default ) => this.TryCall(Import, records, token);
    public virtual async ValueTask<ImmutableArray<TSelf>> Import( NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using NpgsqlBinaryImporter import = await connection.BeginBinaryImportAsync(_copySQL, token);
        ImmutableArray<TSelf>            array  = [..records];
        foreach ( TSelf record in array ) { await record.Import(import, token); }

        await import.CompleteAsync(token);
        return array;
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> Import( NpgsqlConnection connection, NpgsqlTransaction transaction, ReadOnlyMemory<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using NpgsqlBinaryImporter import = await connection.BeginBinaryImportAsync(_copySQL, token);

        for ( int i = 0; i < records.Length; i++ )
        {
            TSelf record = records.Span[i];
            await record.Import(import, token);
        }

        await import.CompleteAsync(token);
        return [..records.Span];
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> Import( NpgsqlConnection connection, NpgsqlTransaction transaction, ImmutableArray<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using NpgsqlBinaryImporter import = await connection.BeginBinaryImportAsync(_copySQL, token);
        foreach ( TSelf record in records ) { await record.Import(import, token); }

        await import.CompleteAsync(token);
        return records;
    }


    public ValueTask<ImmutableArray<TSelf>> Insert( ReadOnlyMemory<TSelf>   records, CancellationToken token = default ) => this.TryCall(Insert, records, token);
    public ValueTask<ImmutableArray<TSelf>> Insert( ImmutableArray<TSelf>   records, CancellationToken token = default ) => this.TryCall(Insert, records, token);
    public ValueTask<ImmutableArray<TSelf>> Insert( IEnumerable<TSelf>      records, CancellationToken token = default ) => this.TryCall(Insert, records, token);
    public IAsyncEnumerable<TSelf>          Insert( IAsyncEnumerable<TSelf> records, CancellationToken token = default ) => this.TryCall(Insert, records, token);
    public ValueTask<TSelf>                 Insert( TSelf                   record,  CancellationToken token = default ) => this.TryCall(Insert, record,  token);
    public virtual async ValueTask<ImmutableArray<TSelf>> Insert( NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using NpgsqlBinaryImporter import = await connection.BeginBinaryImportAsync(_copySQL, token);
        ImmutableArray<TSelf>            array  = [..records];
        foreach ( TSelf record in array ) { await record.Import(import, token); }

        await import.CompleteAsync(token);
        return array;
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> Insert( NpgsqlConnection connection, NpgsqlTransaction transaction, ReadOnlyMemory<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        await using NpgsqlBinaryImporter import = await connection.BeginBinaryImportAsync(_copySQL, token);

        for ( int i = 0; i < records.Length; i++ )
        {
            TSelf record = records.Span[i];
            await record.Import(import, token);
        }

        await import.CompleteAsync(token);
        return [..records.Span];
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> Insert( NpgsqlConnection connection, NpgsqlTransaction transaction, ImmutableArray<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        SqlCommand<TSelf> command = SqlCommand<TSelf>.GetInsert(records);
        TSelf[]           results = GC.AllocateUninitializedArray<TSelf>(records.Length);
        for ( int i = 0; i < records.Length; i++ ) { results[i] = await Insert(connection, transaction, records[i], token); }

        return records;
    }
    public virtual async IAsyncEnumerable<TSelf> Insert( NpgsqlConnection connection, NpgsqlTransaction transaction, IAsyncEnumerable<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
    {
        await foreach ( TSelf record in records.WithCancellation(token) ) { yield return await Insert(connection, transaction, record, token); }
    }


    public virtual async ValueTask<TSelf> Insert( NpgsqlConnection connection, NpgsqlTransaction transaction, TSelf record, CancellationToken token = default )
    {
        SqlCommand<TSelf> command = SqlCommand<TSelf>.GetInsert(record);

        try
        {
            await using NpgsqlCommand    cmd    = command.ToCommand(connection, transaction);
            await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token);
            RecordID<TSelf>              id;

            if ( await reader.ReadAsync(token) ) { id = RecordID<TSelf>.Create(reader.GetGuid(0)); }
            else { throw new InvalidOperationException($"Insert command did not return the new ID for type {typeof(TSelf).FullName}"); }

            return record.NewID(id);
        }
        catch ( Exception e ) { throw new SqlException<TSelf>(command, e); }
    }

    public virtual async ValueTask<ErrorOrResult<TSelf>> TryInsert( NpgsqlConnection connection, NpgsqlTransaction transaction, TSelf record, bool matchAll, PostgresParameters parameters, CancellationToken token = default )
    {
        SqlCommand<TSelf> command = SqlCommand<TSelf>.GetTryInsert(record, matchAll, parameters);

        try
        {
            await using NpgsqlCommand    cmd    = command.ToCommand(connection, transaction);
            await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token);
            RecordID<TSelf>              id;

            if ( await reader.ReadAsync(token) ) { id = RecordID<TSelf>.Create(reader.GetGuid(0)); }
            else { throw new InvalidOperationException($"Insert command did not return the new ID for type {typeof(TSelf).FullName}"); }

            return record.NewID(id);
        }
        catch ( Exception e ) { throw new SqlException<TSelf>(command, e); }
    }


    public virtual async ValueTask<ErrorOrResult<TSelf>> InsertOrUpdate( NpgsqlConnection connection, NpgsqlTransaction transaction, TSelf record, bool matchAll, PostgresParameters parameters, CancellationToken token = default )
    {
        SqlCommand<TSelf> command = SqlCommand<TSelf>.InsertOrUpdate(record, matchAll, parameters);

        try
        {
            await using NpgsqlCommand    cmd    = command.ToCommand(connection, transaction);
            await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token);
            RecordID<TSelf>              id;

            if ( await reader.ReadAsync(token) ) { id = RecordID<TSelf>.Create(reader.GetGuid(0)); }
            else { throw new InvalidOperationException($"Insert command did not return the new ID for type {typeof(TSelf).FullName}"); }

            return record.NewID(id);
        }
        catch ( Exception e ) { throw new SqlException<TSelf>(command, e); }
    }
}
