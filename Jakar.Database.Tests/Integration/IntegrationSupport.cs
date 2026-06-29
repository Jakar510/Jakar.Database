// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using System.Data.Common;
using Jakar.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;

namespace Jakar.Database.Tests.Integration;


/// <summary> A Microsoft SQL Server backed <see cref="Database"/> used only by the integration harness. </summary>
internal sealed class MsSqlTestDatabase( IConfiguration configuration, IOptions<DbOptions> options, IFusionCache cache ) : Database(configuration, options, cache)
{
    public override DatabaseType DatabaseType => DatabaseType.MicrosoftSqlServer;
    protected override DbConnection CreateConnection( in ConnectionString secure ) => new SqlConnection(secure);
}



/// <summary>
///     A SQLite backed <see cref="Database"/> used only by the integration harness.
///     <para> SQLite is not yet a wired <see cref="DatabaseType"/>, so this exists so the SQLite fixture can attempt a connection and skip cleanly until the library adds first-class SQLite support. </para>
/// </summary>
internal sealed class SqliteTestDatabase( IConfiguration configuration, IOptions<DbOptions> options, IFusionCache cache ) : Database(configuration, options, cache)
{
    public override DatabaseType DatabaseType => DatabaseType.NotSet;
    protected override DbConnection CreateConnection( in ConnectionString secure ) => new SqliteConnection(secure);
}



/// <summary> Owns the <see cref="WebApplication"/>, scope, and resolved <see cref="Database"/> for one integration run. The owning fixture disposes any Testcontainer separately. </summary>
public sealed class DbHarness( WebApplication app, IServiceScope scope, Database db ) : IAsyncDisposable
{
    public Database Db => db;

    public async ValueTask DisposeAsync()
    {
        scope.Dispose();

        try { await app.DisposeAsync(); }
        catch ( Exception ) { /* best-effort teardown */ }
    }
}



public static class DbHarnessFactory
{
    /// <summary> Builds a <see cref="WebApplication"/> around <paramref name="register"/>, applies migrations, and resolves the <see cref="Database"/>. Throws if the database is unreachable or migrations fail (callers convert that into a skip). </summary>
    public static async Task<DbHarness> CreateAsync( string name, string connectionString, Action<WebApplicationBuilder, DbOptions> register, CancellationToken token )
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        DbOptions options = new()
                            {
                                TelemetrySource          = new TelemetrySource(AppVersion.Default, Guid.NewGuid(), name, typeof(DbHarnessFactory).Assembly.FullName),
                                ConnectionStringResolver = connectionString,
                                CommandTimeout           = 30,
                                TokenIssuer              = name,
                                TokenAudience            = name,
                                LoggerOptions            = new AppLoggerOptions()
                            };

        register(builder, options);

        WebApplication app = builder.Build();

        try
        {
            await app.ApplyMigrations(token);
        }
        catch ( Exception )
        {
            await app.DisposeAsync();
            throw;
        }

        IServiceScope scope = app.Services.CreateScope();
        Database      db    = scope.ServiceProvider.GetRequiredService<Database>();
        return new DbHarness(app, scope, db);
    }
}
