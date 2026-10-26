// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using NUnit.Framework;
using Jakar.SqlBuilder;



namespace Jakar.SqlBuilder.Tests.Execution;


/// <summary>
///     Runs every <see cref="SqlPatterns"/> pattern on a real engine for one dialect: the SQL must be accepted, bind its parameters, and return the expected rows.
///     Patterns a dialect can't express must instead be rejected at build time with the expected <see cref="SqlBuildError"/>. Mutations run in a transaction that is rolled back,
///     so every pattern sees the same <see cref="TestData"/>. The fixture is skipped when its engine can't be started (no Docker, or no Linux containers).
/// </summary>
[NonParallelizable]
public abstract class DialectExecutionTestsBase
{
    private DbConnection? __connection;

    protected abstract SqlDialectKind Dialect { get; }
    protected          DbConnection   Connection => __connection ?? throw new InvalidOperationException("The engine isn't running.");


    /// <summary> Starts the engine (if it needs one) and returns an open connection. Throwing skips the fixture. </summary>
    protected abstract Task<DbConnection> OpenAsync();

    /// <summary> Stops the engine started by <see cref="OpenAsync"/>. </summary>
    protected virtual ValueTask StopAsync() => ValueTask.CompletedTask;

    /// <summary> The driver's name for a builder parameter token (<c>$1</c>, <c>@p0</c>, <c>:p0</c>). </summary>
    protected virtual string BindName( string token ) => token;

    protected virtual void Configure( DbCommand command ) { }

    /// <summary> A connection with nothing cached on it, before dropping tables earlier tests used (Firebird keeps them locked while its attachment has statements on them). </summary>
    protected virtual Task<DbConnection> FreshAsync( DbConnection connection ) => Task.FromResult(connection);


    [OneTimeSetUp] public async Task StartEngine()
    {
        try { __connection = await OpenAsync(); }
        catch ( Exception e )
        {
            await StopAsync();
            Assert.Ignore($"{Dialect}: engine unavailable; skipped. {e.GetType().Name}: {e.Message}");
        }

        foreach ( string sql in TestData.Schema(Dialect) )
        {
            await using DbCommand command = Connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    [OneTimeTearDown] public async Task StopEngine()
    {
        if ( __connection is not null ) { await __connection.DisposeAsync(); }

        await StopAsync();
    }


    [TestCaseSource(typeof(SqlPatterns), nameof(SqlPatterns.Cases))]
    public async Task Pattern_Runs_On_Engine( SqlPattern pattern )
    {
        if ( pattern.RejectedBy?.Invoke(Dialect) is { } reason )
        {
            Assert.That(() => pattern.Build(Dialect), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(reason), $"{Dialect} should reject {pattern.Name} at build time");
            return;
        }

        if ( pattern.RunsOn?.Invoke(Dialect) is false ) { Assert.Ignore($"{Dialect}'s engine has no {pattern.Name}."); }

        if ( pattern.Kind is PatternKind.Ddl )
        {
            await RunDdl(pattern);
            return;
        }

        SqlResult      result      = pattern.Build(Dialect);
        DbTransaction? transaction = pattern.Kind is PatternKind.Query
                                         ? null
                                         : await Connection.BeginTransactionAsync();

        try
        {
            await using DbCommand command = CreateCommand(in result, transaction);

            if ( pattern.Kind is PatternKind.Mutation )
            {
                int affected = await command.ExecuteNonQueryAsync();
                Assert.That(affected, Is.EqualTo(pattern.Rows(Dialect)), $"affected rows for:\n{result.Sql}");
                return;
            }

            List<object?[]> rows = [];
            await using ( DbDataReader reader = await command.ExecuteReaderAsync() )
            {
                while ( await reader.ReadAsync() )
                {
                    object?[] row = new object?[reader.FieldCount];
                    reader.GetValues(row!);
                    rows.Add(row);
                }
            }

            Assert.That(rows, Has.Count.EqualTo(pattern.Rows(Dialect)), $"rows for:\n{result.Sql}");
            if ( pattern.Check?.Invoke(rows) is { } failure ) { Assert.Fail($"{failure} for:\n{result.Sql}"); }
        }
        catch ( DbException e ) { Assert.Fail($"{Dialect} rejected the SQL:\n{result.Sql}\n{e.GetType().Name}: {e.Message}"); }
        finally
        {
            if ( transaction is not null )
            {
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
            }
        }
    }


    /// <summary> Every fuzzed chain (<see cref="FuzzChains"/>) parses, runs, and returns as many rows as on the SQLite reference. </summary>
    [Test] public async Task Fuzzed_Chains_Agree_With_Reference()
    {
        List<string> failures = [];

        for ( int seed = 0; seed < FuzzChains.COUNT; seed++ )
        {
            SqlResult result   = FuzzChains.Build(seed, Dialect);
            int       expected = await FuzzChains.ReferenceRows(seed);

            try
            {
                int actual = await FuzzChains.Rows(Connection, result, BindName, Configure);
                if ( actual != expected ) { failures.Add($"seed {seed}: {actual} rows, the reference returned {expected}\n{result.Sql}"); }
            }
            catch ( DbException e ) { failures.Add($"seed {seed}: {e.GetType().Name}: {e.Message}\n{result.Sql}"); }
        }

        Assert.That(failures, Is.Empty, string.Join("\n\n", failures.GetRange(0, Math.Min(5, failures.Count))));
    }

    /// <summary>
    ///     <c>CreateTable&lt;T&gt;()</c> for every Jakar.Database table (SPECIFICATION §9.6) runs on the engine, and each new table can be queried. The shared test tables
    ///     (one of which is also called <c>users</c>) are dropped for the duration and rebuilt afterwards.
    /// </summary>
    [Test] public async Task Record_Tables_Create_From_Metadata()
    {
        (string Table, SqlResult Create, string? Trigger)[] tables = RecordTables.All(Dialect);

        __connection = await FreshAsync(Connection);
        await Execute(SqlBuilder.For(Dialect).DropTable("orders").Build());
        await Execute(SqlBuilder.For(Dialect).DropTable("users").Build());

        try
        {
            await ExecuteRaw(RecordTables.TriggerFunction(Dialect));

            foreach ( (string table, SqlResult create, string? trigger) in tables )
            {
                await Execute(create);
                if ( trigger is not null ) { await ExecuteRaw(trigger); }

                int rows = await FuzzChains.Rows(Connection, SqlBuilder.For(Dialect).Select("*").From(table).Build(), BindName, Configure);
                Assert.That(rows, Is.Zero, table);
            }
        }
        finally
        {
            for ( int i = tables.Length - 1; i >= 0; i-- ) { await TryExecute(SqlBuilder.For(Dialect).DropTable(tables[i].Table).Build()); }

            foreach ( string sql in TestData.Schema(Dialect) )
            {
                await using DbCommand command = Connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    ///     The <c>last_modified</c> trigger (<see cref="Jakar.Database.LastModifiedTrigger"/>) replaces whatever the statement wrote, on insert and on update: a row written with a
    ///     year-2000 timestamp reads back with the current one.
    /// </summary>
    [Test] public async Task Last_Modified_Trigger_Sets_The_Column()
    {
        const string TABLE   = "ddl_lm";
        const string TRIGGER = "ddl_lm_set_last_modified";

        Database.DatabaseType type = Database.DatabaseTypeMap.ToDatabaseType(Dialect);
        SqlValue                    old  = SqlValue.Inline(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
        SqlResult                   read = SqlBuilder.For(Dialect).Select("last_modified").From(TABLE).Where("id").EqualTo(1).Build();

        try
        {
            await Execute(SqlBuilder.For(Dialect).CreateTable(TABLE)
                                    .Column("id",            SqlType.Int,         static c => c.PrimaryKey())
                                    .Column("name",          SqlType.VarChar(20))
                                    .Column("last_modified", SqlType.TimestampTz)
                                    .Build());
            await ExecuteRaw(RecordTables.TriggerFunction(Dialect));
            await ExecuteRaw(Database.LastModifiedTrigger.Create(type, TRIGGER, TABLE, "last_modified", ["id"]));

            await Execute(SqlBuilder.For(Dialect).Insert().Into(TABLE, "id", "name", "last_modified").Values(SqlValue.Of(1), SqlValue.Inline("a"), old).Build());
            int inserted = await Year(read);

            await Execute(SqlBuilder.For(Dialect).Update(TABLE).Set("name", SqlValue.Inline("b")).Set("last_modified", old).Where("id").EqualTo(1).Build());
            int updated = await Year(read);

            Assert.Multiple(() =>
                            {
                                Assert.That(inserted, Is.EqualTo(DateTime.UtcNow.Year), "after INSERT");
                                Assert.That(updated,  Is.EqualTo(DateTime.UtcNow.Year), "after UPDATE");
                            });

            await ExecuteRaw(Database.LastModifiedTrigger.Drop(type, TRIGGER, TABLE));
        }
        finally { await TryExecute(SqlBuilder.For(Dialect).DropTable(TABLE).Build()); }
    }

    /// <summary> The year of the single value <paramref name="query"/> reads (a DateTime, DateTimeOffset, or SQLite's text). </summary>
    private async Task<int> Year( SqlResult query )
    {
        await using DbCommand command = CreateCommand(in query, null);
        object?               value   = await command.ExecuteScalarAsync();

        return value switch
               {
                   DateTimeOffset offset => offset.Year,
                   DateTime time         => time.Year,
                   FirebirdSql.Data.Types.FbZonedDateTime zoned => zoned.DateTime.Year,
                   string text           => DateTimeOffset.Parse(text, System.Globalization.CultureInfo.InvariantCulture).Year,
                   _                     => throw new AssertionException($"last_modified read back as {value?.GetType().Name ?? "NULL"}")
               };
    }

    /// <summary> Cleanup: runs <paramref name="result"/>, ignoring a failure (the object may never have been created). </summary>
    private async Task TryExecute( SqlResult result )
    {
        await using DbCommand command = CreateCommand(in result, null);

        try { await command.ExecuteNonQueryAsync(); }
        catch ( DbException ) { }
    }

    private async Task ExecuteRaw( string sql )
    {
        await using DbCommand command = Connection.CreateCommand();
        command.CommandText = sql;

        try { await command.ExecuteNonQueryAsync(); }
        catch ( DbException e ) { Assert.Fail($"{Dialect} rejected:\n{sql}\n{e.GetType().Name}: {e.Message}"); }
    }

    private async Task Execute( SqlResult result )
    {
        await using DbCommand command = CreateCommand(in result, null);

        try { await command.ExecuteNonQueryAsync(); }
        catch ( DbException e ) { Assert.Fail($"{Dialect} rejected:\n{result.Sql}\n{e.GetType().Name}: {e.Message}"); }
    }

    private async Task RunDdl( SqlPattern pattern )
    {
        await Cleanup(pattern); // a failed earlier run may have left its objects behind

        try
        {
            foreach ( SqlResult setup in pattern.Setup?.Invoke(Dialect) ?? [] ) { await Execute(setup, $"setup of {pattern.Name}"); }

            await Execute(pattern.Build(Dialect), pattern.Name);
        }
        finally { await Cleanup(pattern); }
    }

    private async Task Execute( SqlResult result, string what )
    {
        await using DbCommand command = CreateCommand(in result, null);

        try { await command.ExecuteNonQueryAsync(); }
        catch ( DbException e ) { Assert.Fail($"{Dialect} rejected the {what}:\n{result.Sql}\n{e.GetType().Name}: {e.Message}"); }
    }

    private async Task Cleanup( SqlPattern pattern )
    {
        foreach ( SqlResult cleanup in pattern.Cleanup?.Invoke(Dialect) ?? [] )
        {
            try
            {
                await using DbCommand command = CreateCommand(in cleanup, null);
                await command.ExecuteNonQueryAsync();
            }
            catch ( DbException ) { }
        }
    }

    private DbCommand CreateCommand( ref readonly SqlResult result, DbTransaction? transaction )
    {
        DbCommand command = Connection.CreateCommand();
        command.CommandText = result.Sql;
        command.Transaction = transaction;
        Configure(command);

        foreach ( SqlParameter parameter in result.Parameters )
        {
            DbParameter value = command.CreateParameter();
            value.ParameterName = BindName(parameter.Name);
            value.Value         = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(value);
        }

        return command;
    }
}
