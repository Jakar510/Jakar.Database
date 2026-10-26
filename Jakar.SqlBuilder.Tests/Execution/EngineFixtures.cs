// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System.Data.Common;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.FirebirdSql;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.Oracle;
using Testcontainers.PostgreSql;
using Jakar.SqlBuilder;



// The containers are started by the base [OneTimeSetUp] (OpenAsync) and disposed by its [OneTimeTearDown] (StopAsync); NUnit1032 can't follow that and asks for a per-test [TearDown].
#pragma warning disable NUnit1032



namespace Jakar.SqlBuilder.Tests.Execution;


/// <summary> In-process: always runs. </summary>
[TestFixture]
public sealed class SqliteExecutionTests : DialectExecutionTestsBase
{
    protected override SqlDialectKind Dialect => SqlDialectKind.Sqlite;

    protected override async Task<DbConnection> OpenAsync()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }
}



[TestFixture]
public sealed class PostgresExecutionTests : DialectExecutionTestsBase
{
    private PostgreSqlContainer? __container;

    protected override SqlDialectKind Dialect => SqlDialectKind.PostgreSql;

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
    }

    /// <summary> <c>$1</c>, <c>$2</c>, ... are positional: Npgsql binds unnamed parameters in order. </summary>
    protected override string BindName( string token ) => string.Empty;
}



[TestFixture]
public sealed class SqlServerExecutionTests : DialectExecutionTestsBase
{
    private MsSqlContainer? __container;

    protected override SqlDialectKind Dialect => SqlDialectKind.SqlServer;

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
    }
}



[TestFixture]
public sealed class MySqlExecutionTests : DialectExecutionTestsBase
{
    private MySqlContainer? __container;

    protected override SqlDialectKind Dialect => SqlDialectKind.MySql;

    protected override async Task<DbConnection> OpenAsync()
    {
        // With binary logging on (MySQL's default), a user without SUPER may create triggers only when the server trusts them (Jakar.Database's last_modified triggers).
        __container = new MySqlBuilder("mysql:8.4").WithCommand("--log-bin-trust-function-creators=1").Build();
        await __container.StartAsync();

        MySqlConnection connection = new(__container.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }
    protected override async ValueTask StopAsync()
    {
        if ( __container is not null ) { await __container.DisposeAsync(); }
    }
}



[TestFixture]
public sealed class OracleExecutionTests : DialectExecutionTestsBase
{
    private OracleContainer? __container;

    protected override SqlDialectKind Dialect => SqlDialectKind.Oracle;

    protected override async Task<DbConnection> OpenAsync()
    {
        __container = new OracleBuilder("gvenzl/oracle-free:23-slim-faststart").Build();
        await __container.StartAsync();

        OracleConnection connection = new(__container.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }
    protected override async ValueTask StopAsync()
    {
        if ( __container is not null ) { await __container.DisposeAsync(); }
    }

    /// <summary> ODP.NET binds by position unless told otherwise; the builder's <c>:p0</c> tokens are names. </summary>
    protected override void   Configure( DbCommand command ) => ( (OracleCommand)command ).BindByName = true;
    protected override string BindName( string     token )   => token.TrimStart(':');
}



[TestFixture]
public sealed class FirebirdExecutionTests : DialectExecutionTestsBase
{
    private FirebirdSqlContainer? __container;

    protected override SqlDialectKind Dialect => SqlDialectKind.Firebird;

    protected override async Task<DbConnection> OpenAsync()
    {
        __container = new FirebirdSqlBuilder("jacobalberty/firebird:v5.0").Build();
        await __container.StartAsync();

        // The 5.0 image doesn't register the database name as an alias, so open the file the image created.
        FbConnectionStringBuilder settings = new(__container.GetConnectionString());
        if ( !settings.Database.StartsWith('/') ) { settings.Database = $"/firebird/data/{settings.Database}"; }

        FbConnection connection = new(settings.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    protected override async Task<DbConnection> FreshAsync( DbConnection connection )
    {
        string settings = connection.ConnectionString;
        await connection.DisposeAsync();
        FbConnection.ClearAllPools();

        FbConnection fresh = new(settings);
        await fresh.OpenAsync();
        return fresh;
    }
    protected override async ValueTask StopAsync()
    {
        if ( __container is not null ) { await __container.DisposeAsync(); }
    }
}
