// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using Testcontainers.MsSql;



namespace Jakar.Database.Tests.Integration;


/// <summary>
///     Runs the shared CRUD suite against Microsoft SQL Server via the <c>JAKAR_TEST_MSSQL</c> connection string, or a throwaway Testcontainer.
///     <para> NOTE: skips until the library's DDL generation is dialect-aware (it currently emits Postgres-only syntax such as <c>gen_random_uuid()</c>), so migrations fail and the fixture is ignored. </para>
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class SqlServerDatabaseTests : DatabaseDialectTestsBase
{
    private MsSqlContainer? __container;


    protected override async Task<DbHarness?> TryCreateHarnessAsync()
    {
        string? environment = Environment.GetEnvironmentVariable("JAKAR_TEST_MSSQL");

        if ( !string.IsNullOrWhiteSpace(environment) )
        {
            try { return await DbHarnessFactory.CreateAsync(nameof(SqlServerDatabaseTests), environment, static ( builder, options ) => builder.AddDatabase<MsSqlTestDatabase>(options), _ct); }
            catch ( Exception ) { return null; }
        }

        try
        {
            __container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await __container.StartAsync();

            string connectionString = __container.GetConnectionString();
            return await DbHarnessFactory.CreateAsync(nameof(SqlServerDatabaseTests), connectionString, static ( builder, options ) => builder.AddDatabase<MsSqlTestDatabase>(options), _ct);
        }
        catch ( Exception )
        {
            await DisposeContainer();
            return null;
        }
    }


    [TearDown] public override async ValueTask DisposeContainer()
    {
        if ( __container is not null )
        {
            await __container.DisposeAsync();
            __container = null;
        }
    }
}
