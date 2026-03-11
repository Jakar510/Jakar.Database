// Jakar.Extensions :: Jakar.Database
// 05/31/2024  23:05


using System.Net.Sockets;
using Microsoft.Data.SqlClient;
using Npgsql.Internal;



namespace Jakar.Database;


public class DbConnectionContext( DbConnection connection, DatabaseType type ) : IAsyncDisposable
{
    public readonly DatabaseType      Type       = type;
    public readonly DbConnection      Connection = connection;
    public          DbTransaction?    Transaction;
    public          SqlConnection?    SqlConnection    => Connection as SqlConnection;
    public          NpgsqlConnection? NpgsqlConnection => Connection as NpgsqlConnection;


    public DbConnectionContext( NpgsqlConnection connection ) : this(connection, DatabaseType.PostgreSQL) { }
    public DbConnectionContext( SqlConnection    connection ) : this(connection, DatabaseType.PostgreSQL) { }


    public static implicit operator DbConnectionContext( NpgsqlConnection connection ) => new(connection);


    public async ValueTask                OpenAsync( CancellationToken        token )                          => await Connection.OpenAsync(token);
    public async ValueTask<DbTransaction> BeginTransaction( CancellationToken token )                          => Transaction ??= await Connection.BeginTransactionAsync(token);
    public async ValueTask<DbTransaction> BeginTransaction( IsolationLevel    level, CancellationToken token ) => Transaction ??= await Connection.BeginTransactionAsync(level, token);


    public async ValueTask BulkInsertAsync<TSelf>( TSelf[] records, string tableName, CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        if ( Connection is not SqlConnection sqlConnection ) { throw new NotSupportedException("Connection must be SqlConnection"); }

        using SqlBulkCopy bulk = new(sqlConnection, SqlBulkCopyOptions.Default, Transaction as SqlTransaction);

        bulk.DestinationTableName = tableName;
        bulk.BatchSize            = records.Length;

        using DataTable data = TSelf.MetaData.DataTable;
        data.BeginLoadData();

        foreach ( TSelf record in records )
        {
            DataRow row = data.NewRow();
            await record.Import(row, token);
        }

        data.EndLoadData();
        await bulk.WriteToServerAsync(data, token);
    }



    #region Copy

    /// <summary>
    /// Begins a binary COPY FROM STDIN operation, a high-performance data import mechanism to a PostgreSQL table.
    /// </summary>
    /// <param name="copyFromCommand">A COPY FROM STDIN SQL command</param>
    /// <param name="token">An optional token to cancel the asynchronous operation. The default value is None.</param>
    /// <returns>A <see cref="NpgsqlBinaryImporter"/> which can be used to write rows and columns</returns>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/sql-copy.html.
    /// </remarks>
    public async ValueTask<NpgsqlBinaryImporter?> BeginBinaryImportAsync( string copyFromCommand, CancellationToken token = default ) => Connection is NpgsqlConnection connection
                                                                                                                                             ? await connection.BeginBinaryImportAsync(copyFromCommand, token)
                                                                                                                                             : null;

    /// <summary>
    /// Begins a binary COPY TO STDOUT operation, a high-performance data export mechanism from a PostgreSQL table.
    /// </summary>
    /// <param name="copyToCommand">A COPY TO STDOUT SQL command</param>
    /// <param name="token">An optional token to cancel the asynchronous operation. The default value is None.</param>
    /// <returns>A <see cref="NpgsqlBinaryExporter"/> which can be used to read rows and columns</returns>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/sql-copy.html.
    /// </remarks>
    public async ValueTask<NpgsqlBinaryExporter?> BeginBinaryExportAsync( string copyToCommand, CancellationToken token = default ) => Connection is NpgsqlConnection connection
                                                                                                                                           ? await connection.BeginBinaryExportAsync(copyToCommand, token)
                                                                                                                                           : null;

    /// <summary>
    /// Begins a textual COPY FROM STDIN operation, a data import mechanism to a PostgreSQL table.
    /// It is the user's responsibility to send the textual input according to the format specified
    /// in <paramref name="copyFromCommand"/>.
    /// </summary>
    /// <param name="copyFromCommand">A COPY FROM STDIN SQL command</param>
    /// <param name="token">An optional token to cancel the asynchronous operation. The default value is None.</param>
    /// <returns>
    /// A TextWriter that can be used to send textual data.</returns>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/sql-copy.html.
    /// </remarks>
    public async ValueTask<NpgsqlCopyTextWriter?> BeginTextImportAsync( string copyFromCommand, CancellationToken token = default ) => Connection is NpgsqlConnection connection
                                                                                                                                           ? await connection.BeginTextImportAsync(copyFromCommand, token)
                                                                                                                                           : null;

    /// <summary>
    /// Begins a textual COPY TO STDOUT operation, a data export mechanism from a PostgreSQL table.
    /// It is the user's responsibility to parse the textual input according to the format specified
    /// in <paramref name="copyToCommand"/>.
    /// </summary>
    /// <param name="copyToCommand">A COPY TO STDOUT SQL command</param>
    /// <param name="token">An optional token to cancel the asynchronous operation. The default value is None.</param>
    /// <returns>
    /// A TextReader that can be used to read textual data.</returns>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/sql-copy.html.
    /// </remarks>
    public async ValueTask<NpgsqlCopyTextReader?> BeginTextExportAsync( string copyToCommand, CancellationToken token = default ) => Connection is NpgsqlConnection connection
                                                                                                                                         ? await connection.BeginTextExportAsync(copyToCommand, token)
                                                                                                                                         : null;

    /// <summary>
    /// Begins a raw binary COPY operation (TO STDOUT or FROM STDIN), a high-performance data export/import mechanism to a PostgreSQL table.
    /// Note that unlike the other COPY API methods, <see cref="BeginRawBinaryCopyAsync(string, CancellationToken)"/> doesn't implement any encoding/decoding
    /// and is unsuitable for structured import/export operation. It is useful mainly for exporting a table as an opaque
    /// blob, for the purpose of importing it back later.
    /// </summary>
    /// <param name="copyCommand">A COPY TO STDOUT or COPY FROM STDIN SQL command</param>
    /// <param name="token">An optional token to cancel the asynchronous operation. The default value is None.</param>
    /// <returns>A <see cref="NpgsqlRawCopyStream"/> that can be used to read or write raw binary data.</returns>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/sql-copy.html.
    /// </remarks>
    public async ValueTask<NpgsqlRawCopyStream?> BeginRawBinaryCopyAsync( string copyCommand, CancellationToken token = default ) => Connection is NpgsqlConnection connection
                                                                                                                                         ? await connection.BeginRawBinaryCopyAsync(copyCommand, token)
                                                                                                                                         : null;

    #endregion



    #region Wait

    /// <summary>
    /// Waits until an asynchronous PostgreSQL messages (e.g. a notification) arrives, and
    /// exits immediately. The asynchronous message is delivered via the normal events
    /// (<see cref="NpgsqlConnection.Notification"/>, <see cref="NpgsqlConnection.Notice"/>).
    /// </summary>
    /// <param name="timeout">
    /// The time-out value, in milliseconds, passed to <see cref="Socket.ReceiveTimeout"/>.
    /// The default value is 0, which indicates an infinite time-out period.
    /// Specifying -1 also indicates an infinite time-out period.
    /// </param>
    /// <returns>true if an asynchronous message was received, false if timed out.</returns>
    public bool Wait( int timeout )
    {
        if ( Connection is NpgsqlConnection connection ) { connection.Wait(timeout); }

        return false;
    }

    /// <summary>
    /// Waits until an asynchronous PostgreSQL messages (e.g. a notification) arrives, and
    /// exits immediately. The asynchronous message is delivered via the normal events
    /// (<see cref="NpgsqlConnection.Notification"/>, <see cref="NpgsqlConnection.Notice"/>).
    /// </summary>
    /// <param name="timeout">
    /// The time-out value is passed to <see cref="Socket.ReceiveTimeout"/>.
    /// </param>
    /// <returns>true if an asynchronous message was received, false if timed out.</returns>
    public bool Wait( TimeSpan timeout ) => Wait((int)timeout.TotalMilliseconds);

    /// <summary>
    /// Waits until an asynchronous PostgreSQL messages (e.g. a notification) arrives, and
    /// exits immediately. The asynchronous message is delivered via the normal events
    /// (<see cref="NpgsqlConnection.Notification"/>, <see cref="NpgsqlConnection.Notice"/>).
    /// </summary>
    public void Wait() => Wait(0);

    /// <summary>
    /// Waits asynchronously until an asynchronous PostgreSQL messages (e.g. a notification)
    /// arrives, and exits immediately. The asynchronous message is delivered via the normal events
    /// (<see cref="NpgsqlConnection.Notification"/>, <see cref="NpgsqlConnection.Notice"/>).
    /// </summary>
    /// <param name="timeout">
    /// The time-out value, in milliseconds.
    /// The default value is 0, which indicates an infinite time-out period.
    /// Specifying -1 also indicates an infinite time-out period.
    /// </param>
    /// <param name="token">
    /// An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>true if an asynchronous message was received, false if timed out.</returns>
    public async ValueTask<bool> WaitAsync( int timeout, CancellationToken token = default )
    {
        if ( Connection is NpgsqlConnection connection ) { return await connection.WaitAsync(timeout, token); }

        return false;
    }

    /// <summary>
    /// Waits asynchronously until an asynchronous PostgreSQL messages (e.g. a notification)
    /// arrives, and exits immediately. The asynchronous message is delivered via the normal events
    /// (<see cref="NpgsqlConnection.Notification"/>, <see cref="NpgsqlConnection.Notice"/>).
    /// </summary>
    /// <param name="timeout">
    /// The time-out value as <see cref="TimeSpan"/>
    /// </param>
    /// <param name="token">
    /// An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>true if an asynchronous message was received, false if timed out.</returns>
    public ValueTask<bool> WaitAsync( TimeSpan timeout, CancellationToken token = default ) => WaitAsync((int)timeout.TotalMilliseconds, token);

    /// <summary>
    /// Waits asynchronously until an asynchronous PostgreSQL messages (e.g. a notification)
    /// arrives, and exits immediately. The asynchronous message is delivered via the normal events
    /// (<see cref="NpgsqlConnection.Notification"/>, <see cref="NpgsqlConnection.Notice"/>).
    /// </summary>
    /// <param name="token">
    /// An optional token to cancel the asynchronous operation. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    public ValueTask<bool> WaitAsync( CancellationToken token = default ) => WaitAsync(0, token);

    #endregion



    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        if ( Transaction != null ) { await Transaction.DisposeAsync(); }
    }
}
