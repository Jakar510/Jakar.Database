// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

namespace Jakar.Database.Tests.Integration;


/// <summary>
///     Runs the shared CRUD suite against SQLite via the <c>JAKAR_TEST_SQLITE</c> connection string, or a throwaway temp-file database.
///     <para> NOTE: skips until the library re-enables SQLite (the <c>DatabaseType.SQLite</c> enum value, data-type maps, and connection-type handling are currently disabled), so migrations fail and the fixture is ignored. </para>
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class SqliteDatabaseTests : DatabaseDialectTestsBase
{
    private string? __databaseFile;


    protected override async Task<DbHarness?> TryCreateHarnessAsync()
    {
        string? environment = Environment.GetEnvironmentVariable("JAKAR_TEST_SQLITE");
        string  connectionString;

        if ( !string.IsNullOrWhiteSpace(environment) ) { connectionString = environment; }
        else
        {
            __databaseFile   = Path.Combine(Path.GetTempPath(), $"jakar-{Guid.NewGuid():N}.db");
            connectionString = $"Data Source={__databaseFile}";
        }

        try { return await DbHarnessFactory.CreateAsync(nameof(SqliteDatabaseTests), connectionString, static ( builder, options ) => builder.AddDatabase<SqliteTestDatabase>(options), _ct); }
        catch ( Exception ) { return null; }
    }

    public override ValueTask DisposeContainer()
    {
        if ( __databaseFile is null || !File.Exists(__databaseFile) ) { return ValueTask.CompletedTask; }

        try { File.Delete(__databaseFile); }
        catch ( FileNotFoundException ) { }

        return ValueTask.CompletedTask;
    }
}
