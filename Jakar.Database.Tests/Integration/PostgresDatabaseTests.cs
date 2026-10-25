// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using Testcontainers.PostgreSql;



namespace Jakar.Database.Tests.Integration;


/// <summary> Runs the shared CRUD suite against PostgreSQL via the <c>JAKAR_TEST_POSTGRES</c> connection string, or a throwaway Testcontainer. Skips if neither is available. </summary>
[TestFixture]
[NonParallelizable]
public sealed class PostgresDatabaseTests : DatabaseDialectTestsBase
{
    // Started from the base [OneTimeSetUp] and disposed in [OneTimeTearDown]; NUnit1032 can't follow that and asks for a per-test [TearDown].
#pragma warning disable NUnit1032
    private PostgreSqlContainer? __container;
#pragma warning restore NUnit1032


    protected override async Task<DbHarness?> TryCreateHarnessAsync()
    {
        string? environment = Environment.GetEnvironmentVariable("JAKAR_TEST_POSTGRES");

        if ( !string.IsNullOrWhiteSpace(environment) )
        {
            // A failure here is the skip reason: the base fixture ignores the fixture with the exception's message.
            return await DbHarnessFactory.CreateAsync(nameof(PostgresDatabaseTests), environment, static ( builder, options ) => builder.AddDatabase<TestDatabase>(options), _ct);
        }

        try
        {
            // Testcontainers builders are immutable: each With* returns a new builder.
            __container = new PostgreSqlBuilder("postgres:18.1").WithDatabase(TestDatabase.AppName).Build();
            await __container.StartAsync();

            string connectionString = __container.GetConnectionString();
            return await DbHarnessFactory.CreateAsync(nameof(PostgresDatabaseTests), connectionString, static ( builder, options ) => builder.AddDatabase<TestDatabase>(options), _ct);
        }
        catch ( Exception )
        {
            await DisposeContainer();
            throw;
        }
    }


    [OneTimeTearDown] public override async ValueTask DisposeContainer()
    {
        if ( __container is not null )
        {
            await __container.DisposeAsync();
            __container = null;
        }
    }
}
