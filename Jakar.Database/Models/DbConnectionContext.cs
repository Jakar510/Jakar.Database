// Jakar.Extensions :: Jakar.Database
// 05/31/2024  23:05


using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;



namespace Jakar.Database;


// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DbConnectionContext : IAsyncDisposable
{
    protected readonly ConcurrentStack<string> _rollbackIDs = [];
    protected readonly Database                _database;
    protected          DbConnection?           _connection;


    public virtual bool HasTransaction { [MemberNotNullWhen(true, nameof(Transaction))] get => Transaction is not null; }

    public virtual string?         ServerVersion => _connection?.ServerVersion;
    public virtual ConnectionState State         => _connection?.State ?? ConnectionState.Closed;

    public virtual DbTransaction? Transaction
    {
        [HandlesResourceDisposal] get;
        protected set
        {
            field?.Dispose();
            field = value;
        }
    }

    public virtual DatabaseType Type => _connection switch
                                        {
                                            null             => DatabaseType.NotSet,
                                            NpgsqlConnection => DatabaseType.PostgreSQL,
                                            SqlConnection    => DatabaseType.MicrosoftSqlServer,
                                            _                => throw new ExpectedValueTypeException(_connection, typeof(NpgsqlConnection), typeof(SqlConnection))
                                        };


    [MustDisposeResource] internal DbConnectionContext( Database database ) => _database = database;
    public virtual async ValueTask DisposeAsync()
    {
        if ( _connection is not null )
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        if ( Transaction is not null ) { await Transaction.DisposeAsync(); }
    }


    [MustDisposeResource] public static async ValueTask<DbConnectionContext> CreateAsync( Database database, CancellationToken token, IsolationLevel? transactionIsolationLevel = null )
    {
        DbConnectionContext context = new(database);
        await context.EnsureConnection(token);
        if ( transactionIsolationLevel.HasValue ) { await context.StartTransactionAsync(transactionIsolationLevel.Value, token); }

        return context;
    }


    public virtual bool TryAs( [NotNullWhen(true)] out NpgsqlConnection? connection, out NpgsqlTransaction? transaction )
    {
        connection = _connection is NpgsqlConnection x
                         ? x
                         : null;

        transaction = Transaction as NpgsqlTransaction;
        return connection is not null;
    }
    public virtual bool TryAs( [NotNullWhen(true)] out SqlConnection? connection, out SqlTransaction? transaction )
    {
        connection = _connection is SqlConnection x
                         ? x
                         : null;

        transaction = Transaction as SqlTransaction;
        return connection is not null;
    }


    public virtual async ValueTask<DbConnection> EnsureConnection( CancellationToken token )
    {
        DbConnection connection = _connection ??= await _database.CreateConnection(token);
        if ( connection.State is ConnectionState.Closed ) { await connection.OpenAsync(token); }

        return connection;
    }
    public virtual async ValueTask StartTransactionAsync( IsolationLevel transactionIsolationLevel, CancellationToken token )
    {
        DbConnection connection = await EnsureConnection(token);
        Transaction = await connection.BeginTransactionAsync(transactionIsolationLevel, token);
    }


    public virtual async ValueTask CompleteAsync( bool wasSuccessful, CancellationToken token )
    {
        if ( wasSuccessful ) { await CommitAsync(token); }
        else { await RollbackAsync(token); }
    }
    public virtual async ValueTask CompleteAsync( bool wasSuccessful, string savePoint, CancellationToken token )
    {
        if ( wasSuccessful ) { await CommitAsync(token); }
        else { await RollbackAsync(savePoint, token); }
    }
    public virtual async ValueTask CommitAsync( CancellationToken token )
    {
        if ( Transaction is not null ) { await Transaction.CommitAsync(token); }
    }
    public virtual async ValueTask RollbackAsync( CancellationToken token )
    {
        string? savePoint = _rollbackIDs.TryPop(out string? rollbackID)
                                ? rollbackID
                                : null;

        await RollbackAsync(savePoint, token);
    }
    public virtual async ValueTask RollbackAsync( string? savePoint, CancellationToken token )
    {
        if ( Transaction is not null )
        {
            if ( !string.IsNullOrWhiteSpace(savePoint) ) { await Transaction.RollbackAsync(savePoint, token); }
            else { await Transaction.RollbackAsync(token); }
        }
    }
    public virtual async ValueTask SaveAsync( string rollbackID, CancellationToken token )
    {
        if ( Transaction is not null )
        {
            _rollbackIDs.Push(rollbackID);
            await Transaction.SaveAsync(rollbackID, token);
        }
    }


    public virtual async IAsyncEnumerable<TValue> QueryAsync<TValue>( SqlCommand command, [EnumeratorCancellation] CancellationToken token )
    {
        await using DbCommand    dbCommand = command.ToCommand(this);
        await using DbDataReader reader    = await dbCommand.ExecuteReaderAsync(token);
        while ( await reader.ReadAsync(token) ) { yield return reader.GetFieldValue<TValue>(0); }
    }


    public virtual async ValueTask ExecuteNonQueryAsync( string sql, CommandParameters parameters, CancellationToken token ) => await ExecuteNonQueryAsync(SqlCommand.Create(sql, parameters), token);
    public virtual async ValueTask ExecuteNonQueryAsync( string sql, CancellationToken token )
    {
        DbConnection          connection = await EnsureConnection(token);
        await using DbCommand command    = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        await command.ExecuteNonQueryAsync(token);
    }
    public virtual async ValueTask ExecuteNonQueryAsync( SqlCommand sql, CancellationToken token )
    {
        await using DbCommand command = sql.ToCommand(this);
        await command.ExecuteNonQueryAsync(token);
    }
    public virtual async ValueTask<T?> ExecuteScalarAsync<T>( SqlCommand sql, CancellationToken token )
    {
        await using DbCommand command = sql.ToCommand(this);
        object?               value   = await command.ExecuteScalarAsync(token).ConfigureAwait(false);

        Console.WriteLine($"ExecuteScalarAsync value: {value}");

        return value is T result
                   ? result
                   : default;
    }
    public virtual async IAsyncEnumerable<TSelf> ExecuteAsync<TSelf>( SqlCommand sql, [EnumeratorCancellation] CancellationToken token )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        await using DbCommand    command = sql.ToCommand(this);
        await using DbDataReader reader  = await command.ExecuteReaderAsync(token);
        while ( await reader.ReadAsync(token) ) { yield return TSelf.Create(reader); }
    }
    public virtual async ValueTask<ImmutableArray<TSelf>> ExecuteAsync<TSelf>( SqlCommand sql, int initialCapacity, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        List<TSelf> list = new(initialCapacity);
        await foreach ( TSelf record in ExecuteAsync<TSelf>(sql, token) ) { list.Add(record); }

        return [..list];
    }


    public async ValueTask EnsureTableExistsAsync<TSelf>( CancellationToken token )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        if ( !await TableExistsAsync<MigrationRecord>(token) ) { await ExecuteNonQueryAsync(TSelf.MetaData.CreateTableSql(), token); }
    }
    public async ValueTask<bool> TableExistsAsync<TSelf>( CancellationToken token )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        DbConnection connection = await EnsureConnection(token);

        string sql = connection switch
                     {
                         NpgsqlConnection => """
                                             SELECT EXISTS (
                                                 SELECT 1
                                                 FROM information_schema.tables
                                                 WHERE table_name = @name
                                             )
                                             """,

                         SqlConnection => """
                                          SELECT CASE WHEN EXISTS (
                                              SELECT 1
                                              FROM INFORMATION_SCHEMA.TABLES
                                              WHERE TABLE_NAME = @name
                                          ) THEN 1 ELSE 0 END
                                          """,

                         SqliteConnection => """
                                             SELECT EXISTS (
                                                 SELECT 1
                                                 FROM sqlite_master
                                                 WHERE type='table'
                                                 AND name = @name
                                             )
                                             """,

                         _ => throw new NotSupportedException($"Unsupported provider: {connection.GetType().FullName}")
                     };

        await using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        DbParameter p = cmd.CreateParameter();
        p.ParameterName = "@name";
        p.Value         = TSelf.TableName.Value;
        cmd.Parameters.Add(p);

        object? result = await cmd.ExecuteScalarAsync(token);
        return Convert.ToInt32(result) == 1;
    }


    public virtual ValueTask<TSelf> FirstAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => FirstAsync<TSelf>(SqlCommand.GetFirst<TSelf>(), token);
    public virtual ValueTask<TSelf?> FirstOrDefaultAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => FirstOrDefaultAsync<TSelf>(SqlCommand.GetFirst<TSelf>(), token);


    public virtual async ValueTask<TSelf> FirstAsync<TSelf>( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        try { return await ExecuteAsync<TSelf>(command, token).FirstAsync(token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }
    public virtual async ValueTask<TSelf?> FirstOrDefaultAsync<TSelf>( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        try { return await ExecuteAsync<TSelf>(command, token).FirstOrDefaultAsync(token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }


    public virtual async ValueTask<TSelf> SingleAsync<TSelf>( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        try { return await ExecuteAsync<TSelf>(command, token).SingleAsync(token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }
    public virtual async ValueTask<TSelf?> SingleOrDefaultAsync<TSelf>( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        try { return await ExecuteAsync<TSelf>(command, token).SingleOrDefaultAsync(token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }


    public virtual async ValueTask<TSelf> LastAsync<TSelf>( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        try { return await ExecuteAsync<TSelf>(command, token).LastAsync(token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }
    public virtual async ValueTask<TSelf?> LastOrDefaultAsync<TSelf>( SqlCommand command, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        try { return await ExecuteAsync<TSelf>(command, token).LastOrDefaultAsync(token); }
        catch ( Exception e ) { throw new DbSqlException(command, e); }
    }


    public virtual async ValueTask<string?> GetServerVersion( CancellationToken token = default )
    {
        await using DbConnection connection = await EnsureConnection(token);
        return connection.ServerVersion;
    }
    public virtual async ValueTask Schema( Func<DataTable, CancellationToken, ValueTask> func, CancellationToken token = default )
    {
        using DataTable schema = await Schema(token);
        await func(schema, token);
    }
    public virtual async ValueTask Schema( Func<DataTable, CancellationToken, ValueTask> func, PostgresCollectionType collectionName, CancellationToken token = default )
    {
        using DataTable schema = await Schema(collectionName, token);
        await func(schema, token);
    }
    public virtual async ValueTask Schema( Func<DataTable, CancellationToken, ValueTask> func, PostgresCollectionType collectionName, string?[] restrictionValues, CancellationToken token = default )
    {
        using DataTable schema = await Schema(collectionName, restrictionValues, token);
        await func(schema, token);
    }
    public virtual async ValueTask<TResult> Schema<TResult>( Func<DataTable, CancellationToken, ValueTask<TResult>> func, CancellationToken token = default )
    {
        using DataTable schema = await Schema(token);
        return await func(schema, token);
    }
    public virtual async ValueTask<TResult> Schema<TResult>( Func<DataTable, CancellationToken, ValueTask<TResult>> func, PostgresCollectionType collectionName, CancellationToken token = default )
    {
        using DataTable schema = await Schema(collectionName, token);
        return await func(schema, token);
    }
    public virtual async ValueTask<TResult> Schema<TResult>( Func<DataTable, CancellationToken, ValueTask<TResult>> func, PostgresCollectionType collectionName, string?[] restrictionValues, CancellationToken token = default )
    {
        using DataTable schema = await Schema(collectionName, restrictionValues, token);
        return await func(schema, token);
    }
    public virtual async ValueTask<DataTable> Schema( CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        return await connection.GetSchemaAsync(token);
    }
    public virtual async ValueTask<DataTable> Schema( PostgresCollectionType collectionName, CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        return await connection.GetSchemaAsync(collectionName.GetCollectionTypeName(), token);
    }
    public virtual async ValueTask<DataTable> Schema( PostgresCollectionType collectionName, string?[] restrictionValues, CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        return await connection.GetSchemaAsync(collectionName.GetCollectionTypeName(), restrictionValues, token);
    }



    #region Copy

    /// <summary> Begins a binary COPY FROM STDIN operation, a high-performance data import mechanism to a PostgreSQL table. </summary>
    /// <param name="copyFromCommand"> A COPY FROM STDIN SQL command </param>
    /// <param name="token"> An optional token to cancel the asynchronous operation. The default value is None. </param>
    /// <returns> A <see cref="NpgsqlBinaryImporter"/> which can be used to write rows and columns </returns>
    /// <remarks> See https://www.postgresql.org/docs/current/static/sql-copy.html. </remarks>
    public virtual async ValueTask<NpgsqlBinaryImporter> BeginBinaryImportAsync( string copyFromCommand, CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        if ( connection is NpgsqlConnection postgres ) { return await postgres.BeginBinaryImportAsync(copyFromCommand, token); }

        throw new NotSupportedException($"{nameof(BeginBinaryImportAsync)} Only supported with PostgreSql");
    }

    /// <summary> Begins a binary COPY TO STDOUT operation, a high-performance data export mechanism from a PostgreSQL table. </summary>
    /// <param name="copyToCommand"> A COPY TO STDOUT SQL command </param>
    /// <param name="token"> An optional token to cancel the asynchronous operation. The default value is None. </param>
    /// <returns> A <see cref="NpgsqlBinaryExporter"/> which can be used to read rows and columns </returns>
    /// <remarks> See https://www.postgresql.org/docs/current/static/sql-copy.html. </remarks>
    public virtual async ValueTask<NpgsqlBinaryExporter> BeginBinaryExportAsync( string copyToCommand, CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        if ( connection is NpgsqlConnection postgres ) { return await postgres.BeginBinaryExportAsync(copyToCommand, token); }

        throw new NotSupportedException($"{nameof(BeginBinaryExportAsync)} Only supported with PostgreSql");
    }

    /// <summary>
    ///     Begins a textual COPY FROM STDIN operation, a data import mechanism to a PostgreSQL table. It is the user's responsibility to send the textual input according to the format specified in
    ///     <paramref
    ///         name="copyFromCommand"/>
    ///     .
    /// </summary>
    /// <param name="copyFromCommand"> A COPY FROM STDIN SQL command </param>
    /// <param name="token"> An optional token to cancel the asynchronous operation. The default value is None. </param>
    /// <returns> A TextWriter that can be used to send textual data. </returns>
    /// <remarks> See https://www.postgresql.org/docs/current/static/sql-copy.html. </remarks>
    public virtual async ValueTask<NpgsqlCopyTextWriter> BeginTextImportAsync( string copyFromCommand, CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        if ( connection is NpgsqlConnection postgres ) { return await postgres.BeginTextImportAsync(copyFromCommand, token); }

        throw new NotSupportedException($"{nameof(BeginTextImportAsync)} Only supported with PostgreSql");
    }

    /// <summary>
    ///     Begins a textual COPY TO STDOUT operation, a data export mechanism from a PostgreSQL table. It is the user's responsibility to parse the textual input according to the format specified in
    ///     <paramref
    ///         name="copyToCommand"/>
    ///     .
    /// </summary>
    /// <param name="copyToCommand"> A COPY TO STDOUT SQL command </param>
    /// <param name="token"> An optional token to cancel the asynchronous operation. The default value is None. </param>
    /// <returns> A TextReader that can be used to read textual data. </returns>
    /// <remarks> See https://www.postgresql.org/docs/current/static/sql-copy.html. </remarks>
    public virtual async ValueTask<NpgsqlCopyTextReader> BeginTextExportAsync( string copyToCommand, CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        if ( connection is NpgsqlConnection postgres ) { return await postgres.BeginTextExportAsync(copyToCommand, token); }

        throw new NotSupportedException($"{nameof(BeginTextExportAsync)} Only supported with PostgreSql");
    }

    /// <summary>
    ///     Begins a raw binary COPY operation (TO STDOUT or FROM STDIN), a high-performance data export/import mechanism to a PostgreSQL table. Note that unlike the other COPY API methods,
    ///     <see
    ///         cref="BeginRawBinaryCopyAsync(string, CancellationToken)"/>
    ///     doesn't implement any encoding/decoding and is unsuitable for structured import/export operation. It is useful mainly for exporting a table as an opaque blob, for the purpose of importing it back later.
    /// </summary>
    /// <param name="copyCommand"> A COPY TO STDOUT or COPY FROM STDIN SQL command </param>
    /// <param name="token"> An optional token to cancel the asynchronous operation. The default value is None. </param>
    /// <returns> A <see cref="NpgsqlRawCopyStream"/> that can be used to read or write raw binary data. </returns>
    /// <remarks> See https://www.postgresql.org/docs/current/static/sql-copy.html. </remarks>
    public virtual async ValueTask<NpgsqlRawCopyStream> BeginRawBinaryCopyAsync( string copyCommand, CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        if ( connection is NpgsqlConnection postgres ) { return await postgres.BeginRawBinaryCopyAsync(copyCommand, token); }

        throw new NotSupportedException($"{nameof(BeginRawBinaryCopyAsync)} Only supported with PostgreSql");
    }


    public virtual async ValueTask<ImmutableArray<TSelf>> ImportAsync<TSelf>( [HandlesResourceDisposal] ArrayBuffer<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        using ArrayBuffer<TSelf> array      = records;
        DbConnection             connection = await EnsureConnection(token);

        return connection switch
               {
                   SqlConnection sqlServer   => await ImportAsync(sqlServer, array, token),
                   NpgsqlConnection postgres => await ImportAsync(postgres,  array, token),
                   _                         => throw new NotSupportedException("Connection must be to Microsoft Sql Server or PostgreSql")
               };
    }
    protected virtual async ValueTask<ImmutableArray<TSelf>> ImportAsync<TSelf>( NpgsqlConnection connection, ArrayBuffer<TSelf> records, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        await using NpgsqlBinaryImporter import = await connection.BeginBinaryImportAsync(SqlCommand.GetCopy<TSelf>().SQL, token);
        foreach ( TSelf record in records.Array ) { await record.Import(import, token); }

        await import.CompleteAsync(token);
        return [..records.Span];
    }
    protected virtual async ValueTask<ImmutableArray<TSelf>> ImportAsync<TSelf>( SqlConnection connection, ArrayBuffer<TSelf> records, CancellationToken token = default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        using SqlBulkCopy bulk = new(connection, SqlBulkCopyOptions.Default, Transaction as SqlTransaction);

        bulk.DestinationTableName = TSelf.TableName;
        int recordCount = 0;

        using DataTable data = TSelf.MetaData.DataTable;
        data.BeginLoadData();

        foreach ( TSelf record in records.Array )
        {
            DataRow row = data.NewRow();
            await record.Import(row, token);
            recordCount++;
        }

        bulk.BatchSize = recordCount;
        data.EndLoadData();

        await bulk.WriteToServerAsync(data, token);
        return [..records.Span];
    }

    #endregion



    #region Wait

    /// <summary>
    ///     Waits asynchronously until an asynchronous PostgreSQL messages (e.g. a notification) arrives, and exits immediately. The asynchronous message is delivered via the normal events (
    ///     <see
    ///         cref="NpgsqlConnection.Notification"/>
    ///     ,
    ///     <see
    ///         cref="NpgsqlConnection.Notice"/>
    ///     ).
    /// </summary>
    /// <param name="timeout"> The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period. </param>
    /// <param name="token"> An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None"/>. </param>
    /// <returns> true if an asynchronous message was received, false if timed out. </returns>
    public virtual async ValueTask<bool> WaitAsync( int timeout, CancellationToken token = default )
    {
        DbConnection connection = await EnsureConnection(token);
        if ( connection is NpgsqlConnection x ) { return await x.WaitAsync(timeout, token); }

        return false;
    }

    /// <summary>
    ///     Waits asynchronously until an asynchronous PostgreSQL messages (e.g. a notification) arrives, and exits immediately. The asynchronous message is delivered via the normal events (
    ///     <see
    ///         cref="NpgsqlConnection.Notification"/>
    ///     ,
    ///     <see
    ///         cref="NpgsqlConnection.Notice"/>
    ///     ).
    /// </summary>
    /// <param name="timeout"> The time-out value as <see cref="TimeSpan"/> </param>
    /// <param name="token"> An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None"/>. </param>
    /// <returns> true if an asynchronous message was received, false if timed out. </returns>
    public virtual ValueTask<bool> WaitAsync( TimeSpan timeout, CancellationToken token = default ) => WaitAsync((int)timeout.TotalMilliseconds, token);

    /// <summary>
    ///     Waits asynchronously until an asynchronous PostgreSQL messages (e.g. a notification) arrives, and exits immediately. The asynchronous message is delivered via the normal events (
    ///     <see
    ///         cref="NpgsqlConnection.Notification"/>
    ///     ,
    ///     <see
    ///         cref="NpgsqlConnection.Notice"/>
    ///     ).
    /// </summary>
    /// <param name="token"> An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None"/>. </param>
    public virtual ValueTask<bool> WaitAsync( CancellationToken token = default ) => WaitAsync(0, token);

    #endregion
}
