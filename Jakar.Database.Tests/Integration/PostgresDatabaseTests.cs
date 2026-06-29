// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using Testcontainers.PostgreSql;



namespace Jakar.Database.Tests.Integration;


/// <summary> Runs the shared CRUD suite against PostgreSQL via the <c>JAKAR_TEST_POSTGRES</c> connection string, or a throwaway Testcontainer. Skips if neither is available. </summary>
[TestFixture]
[NonParallelizable]
public sealed class PostgresDatabaseTests : DatabaseDialectTestsBase
{
    private PostgreSqlContainer? __container;


    protected override async Task<DbHarness?> TryCreateHarnessAsync()
    {
        string? environment = Environment.GetEnvironmentVariable("JAKAR_TEST_POSTGRES");

        if ( !string.IsNullOrWhiteSpace(environment) )
        {
            try { return await DbHarnessFactory.CreateAsync(nameof(PostgresDatabaseTests), environment, static ( builder, options ) => builder.AddDatabase<TestDatabase>(options), _ct); }
            catch ( Exception ) { return null; }
        }

        try
        {
            PostgreSqlBuilder containerBuilder = new("postgres:18.1");
            containerBuilder.WithUsername("dev");
            containerBuilder.WithPassword("dev");
            containerBuilder.WithDatabase(TestDatabase.AppName);

            __container = containerBuilder.Build();
            await __container.StartAsync();

            string connectionString = $"User ID=dev;Password=dev;Host={__container.IpAddress};Port={__container.GetMappedPublicPort()};Database={TestDatabase.AppName}";
            return await DbHarnessFactory.CreateAsync(nameof(PostgresDatabaseTests), connectionString, static ( builder, options ) => builder.AddDatabase<TestDatabase>(options), _ct);
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
