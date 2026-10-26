// Jakar.Database :: Jakar.Database.Tests
// 10/25/2026

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;



// The containers are started by the base [OneTimeSetUp] (OpenAsync) and disposed by its [OneTimeTearDown] (StopAsync); NUnit1032 can't follow that and asks for a per-test [TearDown].
#pragma warning disable NUnit1032


namespace Jakar.Database.Tests.Integration;


/// <summary>
///     The migrations (<see cref="TableMetaData{TSelf}.CreateTableSql(in DatabaseType)"/>, now written by <c>SqlBuilder.CreateTable&lt;TRecord&gt;()</c>) must create the same schema as the
///     hand-written DDL they replaced, which is frozen in <c>Integration/LegacyDdl/</c> (<c>{table}.{DatabaseType}.sql</c>). Every table is created both ways, each in its own schema or
///     database, and the catalogs must match: columns, types, nullability, identity, defaults, keys, foreign keys and checks. Skipped when the engine's container can't be started.
/// </summary>
[NonParallelizable]
public abstract class CreateTableAgreementTestsBase
{
    protected const string LEGACY    = "via_legacy";
    protected const string MIGRATION = "via_migration";

    private DbConnection? __connection;

    protected DbConnection Connection => __connection ?? throw new InvalidOperationException("The engine isn't running.");

    protected abstract DatabaseType                   DatabaseType { get; }
    protected abstract SqlDialectKind Dialect      { get; }

    /// <summary> Starts the engine and returns an open connection. Throwing skips the fixture. </summary>
    protected abstract Task<DbConnection> OpenAsync();
    protected abstract ValueTask          StopAsync();

    /// <summary> Creates <paramref name="target"/> (a schema or database) and makes it the current one. </summary>
    protected abstract Task CreateAsync( string target );

    /// <summary> Makes <paramref name="target"/> the current schema or database. </summary>
    protected abstract Task UseAsync( string target );

    /// <summary> Columns of every table in the current schema / database: name, type, length, precision, nullability, identity, default. </summary>
    protected abstract string ColumnsSql { get; }

    /// <summary> Primary, unique and foreign keys of every table, as column lists (constraint names differ, so they're left out). </summary>
    protected abstract string KeysSql { get; }

    /// <summary> Foreign key targets and actions, and check definitions. </summary>
    protected abstract string ReferencesSql { get; }


    // Foreign-key order: every table after the tables it references.
    private (string Table, Func<string> Legacy, Func<string> Migration)[] Tables() =>
    [
        Table<FileRecord>(),
        Table<UserRecord>(),
        Table<AddressRecord>(),
        Table<GroupRecord>(),
        Table<RoleRecord>(),
        Table<RecoveryCodeRecord>(),
        Table<UserLoginProviderRecord>(),
        Table<UserAddressRecord>(),
        Table<UserGroupRecord>(),
        Table<UserRoleRecord>(),
        Table<UserRecoveryCodeRecord>()
    ];

    private (string, Func<string>, Func<string>) Table<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        DatabaseType type = DatabaseType;
        return ( TSelf.SqlTableName, () => Legacy(TSelf.SqlTableName, type), () => TSelf.MetaData.CreateTableSql(in type) );
    }

    private static string Legacy( string table, DatabaseType type ) => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Integration", "LegacyDdl", $"{table}.{type}.sql"));


    [OneTimeSetUp] public async Task Start()
    {
        try { __connection = await OpenAsync(); }
        catch ( Exception e )
        {
            await Stop();
            Assert.Ignore($"{Dialect} unavailable; skipped. {e.GetType().Name}: {e.Message}");
        }

        await Create(LEGACY,    static table => table.Legacy());
        await Create(MIGRATION, static table => table.Migration());
    }

    [OneTimeTearDown] public async Task Stop()
    {
        if ( __connection is not null ) { await __connection.DisposeAsync(); }

        __connection = null;
        await StopAsync();
    }

    private async Task Create( string target, Func<(string Table, Func<string> Legacy, Func<string> Migration), string> sql )
    {
        await CreateAsync(target);

        foreach ( (string Table, Func<string> Legacy, Func<string> Migration) table in Tables() )
        {
            string statement = sql(table);

            try { await Execute(statement); }
            catch ( DbException e ) { Assert.Fail($"{target} couldn't create {table.Table}:\n{statement}\n{e.Message}"); }
        }
    }

    protected async Task Execute( string sql )
    {
        await using DbCommand command = Connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<string>> Rows( string sql, string target )
    {
        await UseAsync(target);
        await using DbCommand command = Connection.CreateCommand();
        command.CommandText = sql;

        List<string> rows = [];
        await using DbDataReader reader = await command.ExecuteReaderAsync();
        while ( await reader.ReadAsync() )
        {
            object[] values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(string.Join(" | ", values));
        }

        rows.Sort(StringComparer.Ordinal);
        return rows;
    }

    private async Task Agree( string sql )
    {
        List<string> legacy    = await Rows(sql, LEGACY);
        List<string> migration = await Rows(sql, MIGRATION);

        Assert.That(legacy,    Is.Not.Empty);
        Assert.That(migration, Is.EqualTo(legacy), $"legacy:\n{string.Join('\n', legacy)}\n\nmigration:\n{string.Join('\n', migration)}");
    }


    [Test] public Task Columns_Agree()    => Agree(ColumnsSql);
    [Test] public Task Keys_Agree()       => Agree(KeysSql);
    [Test] public Task References_Agree() => Agree(ReferencesSql);
}



[TestFixture]
public sealed class PostgresCreateTableAgreementTests : CreateTableAgreementTestsBase
{
    private PostgreSqlContainer? __container;

    protected override DatabaseType                    DatabaseType => DatabaseType.PostgreSQL;
    protected override SqlDialectKind Dialect      => SqlDialectKind.PostgreSql;

    protected override async Task<DbConnection> OpenAsync()
    {
        __container = new PostgreSqlBuilder("postgres:18.1").Build();
        await __container.StartAsync();

        NpgsqlConnection connection = new(__container.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }

    protected override async ValueTask StopAsync()
    {
        if ( __container is not null ) { await __container.DisposeAsync(); }

        __container = null;
    }

    protected override async Task CreateAsync( string target ) => await Execute($"CREATE SCHEMA {target}; SET search_path TO {target};");
    protected override async Task UseAsync( string    target ) => await Execute($"SET search_path TO {target};");

    protected override string ColumnsSql => """
                                            SELECT table_name, column_name, data_type, character_maximum_length, numeric_precision, numeric_scale, is_nullable, is_identity, identity_generation, COALESCE(column_default, '')
                                            FROM information_schema.columns WHERE table_schema = current_schema()
                                            """;

    protected override string KeysSql => """
                                         SELECT tc.table_name, tc.constraint_type, string_agg(kcu.column_name, ',' ORDER BY kcu.ordinal_position)
                                         FROM information_schema.table_constraints tc
                                         JOIN information_schema.key_column_usage kcu ON kcu.constraint_schema = tc.constraint_schema AND kcu.constraint_name = tc.constraint_name
                                         WHERE tc.table_schema = current_schema() AND tc.constraint_type IN ('PRIMARY KEY', 'UNIQUE', 'FOREIGN KEY')
                                         GROUP BY tc.table_name, tc.constraint_type, tc.constraint_name
                                         """;

    protected override string ReferencesSql => """
                                               SELECT (SELECT relname FROM pg_class WHERE oid = c.conrelid), pg_get_constraintdef(c.oid)
                                               FROM pg_constraint c JOIN pg_namespace n ON n.oid = c.connamespace
                                               WHERE n.nspname = current_schema() AND c.contype IN ('f', 'c')
                                               """;
}



[TestFixture]
public sealed class SqlServerCreateTableAgreementTests : CreateTableAgreementTestsBase
{
    private MsSqlContainer? __container;

    protected override DatabaseType                    DatabaseType => DatabaseType.MicrosoftSqlServer;
    protected override SqlDialectKind Dialect      => SqlDialectKind.SqlServer;

    protected override async Task<DbConnection> OpenAsync()
    {
        __container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await __container.StartAsync();

        SqlConnection connection = new(__container.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }

    protected override async ValueTask StopAsync()
    {
        if ( __container is not null ) { await __container.DisposeAsync(); }

        __container = null;
    }

    // T-SQL has no search_path: each pipeline gets its own database.
    protected override async Task CreateAsync( string target )
    {
        await Execute($"CREATE DATABASE [{target}];");
        await UseAsync(target);
    }

    protected override Task UseAsync( string target ) => Connection.ChangeDatabaseAsync(target);

    protected override string ColumnsSql => """
                                            SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE,
                                                   COLUMNPROPERTY(OBJECT_ID(QUOTENAME(TABLE_SCHEMA) + '.' + QUOTENAME(TABLE_NAME)), COLUMN_NAME, 'IsIdentity'), COALESCE(COLUMN_DEFAULT, '')
                                            FROM INFORMATION_SCHEMA.COLUMNS
                                            """;

    protected override string KeysSql => """
                                         SELECT tc.TABLE_NAME, tc.CONSTRAINT_TYPE, STRING_AGG(kcu.COLUMN_NAME, ',') WITHIN GROUP (ORDER BY kcu.ORDINAL_POSITION)
                                         FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                                         JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                                         WHERE tc.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE', 'FOREIGN KEY')
                                         GROUP BY tc.TABLE_NAME, tc.CONSTRAINT_TYPE, tc.CONSTRAINT_NAME
                                         """;

    protected override string ReferencesSql => """
                                               SELECT OBJECT_NAME(fk.parent_object_id), COL_NAME(fkc.parent_object_id, fkc.parent_column_id), OBJECT_NAME(fk.referenced_object_id),
                                                      COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id), fk.delete_referential_action_desc, fk.update_referential_action_desc
                                               FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                                               UNION ALL
                                               SELECT OBJECT_NAME(parent_object_id), definition, '', '', '', '' FROM sys.check_constraints
                                               """;
}
