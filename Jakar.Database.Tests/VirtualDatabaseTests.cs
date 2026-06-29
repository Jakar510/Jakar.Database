// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using System.Data;
using System.Data.Common;
using Jakar.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;



namespace Jakar.Database.Tests;


/// <summary>
///     "Virtual" database tests: a <see cref="Database"/> that is fully constructed (all <see cref="DbTable{TSelf}"/> wired) but never opens a real connection.
///     Validates the in-memory structure and dialect wiring of <see cref="Database"/> / <see cref="DbTable{TSelf}"/> without a database engine.
/// </summary>
[TestFixture]
public sealed class VirtualDatabaseTests : Assert
{
    private FakeDatabase __db = null!;


    [OneTimeSetUp]    public       void OneTimeSetUp()    => __db = CreateDatabase();
    [OneTimeTearDown] public async Task OneTimeTearDown() => await __db.DisposeAsync();


    [Test] public void All_table_records_are_wired()
    {
        Multiple(() =>
                 {
                     That(__db.Users,              Is.Not.Null);
                     That(__db.Roles,              Is.Not.Null);
                     That(__db.Groups,             Is.Not.Null);
                     That(__db.Files,              Is.Not.Null);
                     That(__db.Addresses,          Is.Not.Null);
                     That(__db.RecoveryCodes,      Is.Not.Null);
                     That(__db.UserLoginProviders, Is.Not.Null);
                 });
    }

    [Test] public void Dialect_is_derived_from_database_type()
    {
        Multiple(() =>
                 {
                     That(__db.DatabaseType, Is.EqualTo(DatabaseType.PostgreSQL));
                     That(__db.Dialect,      Is.EqualTo(Jakar.SqlBuilder.SqlDialectKind.PostgreSql));
                 });
    }

    [Test] public void DbTables_expose_their_record_table_names()
    {
        Multiple(() =>
                 {
                     That(__db.Users.TableName.Value,              Is.EqualTo("users"));
                     That(__db.Roles.TableName.Value,              Is.EqualTo("roles"));
                     That(__db.Groups.TableName.Value,             Is.EqualTo("groups"));
                     That(__db.Files.TableName.Value,              Is.EqualTo("files"));
                     That(__db.Addresses.TableName.Value,          Is.EqualTo("addresses"));
                     That(__db.RecoveryCodes.TableName.Value,      Is.EqualTo("recovery_codes"));
                     That(__db.UserLoginProviders.TableName.Value, Is.EqualTo("user_login_providers"));
                 });
    }

    [Test] public void DbTable_metadata_matches_the_record_metadata() => That((object)( (IDbTable)__db.Users ).MetaData, Is.SameAs(UserRecord.MetaData));

    [Test] public void DbTable_inherits_database_transaction_isolation_level()
    {
        Multiple(() =>
                 {
                     That(__db.TransactionIsolationLevel,       Is.EqualTo(IsolationLevel.RepeatableRead));
                     That(__db.Users.TransactionIsolationLevel, Is.EqualTo(__db.TransactionIsolationLevel));
                 });
    }

    [Test] public void DbTable_empty_helpers_are_empty()
    {
        Multiple(() =>
                 {
                     That(DbTable<UserRecord>.Empty,      Is.Empty);
                     That(DbTable<UserRecord>.EmptyArray, Is.Empty);
                 });
    }


    private static FakeDatabase CreateDatabase()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [DbOptions.JWT_KEY] = "virtual-database-tests-secret-key-with-enough-length-for-hmac-sha512-signatures" }).Build();

        DbOptions options = new()
                            {
                                TelemetrySource          = new TelemetrySource(AppVersion.Default, Guid.NewGuid(), nameof(VirtualDatabaseTests), typeof(VirtualDatabaseTests).Assembly.FullName),
                                ConnectionStringResolver = "Host=localhost;Port=5432;Database=virtual;Username=dev;Password=dev",
                                CommandTimeout           = 30,
                                TokenIssuer              = nameof(VirtualDatabaseTests),
                                TokenAudience            = nameof(VirtualDatabaseTests),
                                LoggerOptions            = new AppLoggerOptions()
                            };

        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddSingleton(configuration);
        services.AddSingleton(options);
        services.AddSingleton<IOptions<DbOptions>>(options);
        options.ConfigureFusionCache(services.AddFusionCache());
        services.AddSingleton<FakeDatabase>();

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<FakeDatabase>();
    }



    private sealed class FakeDatabase( IConfiguration configuration, IOptions<DbOptions> options, IFusionCache cache ) : Database(configuration, options, cache)
    {
        public override    DatabaseType DatabaseType                                   => DatabaseType.PostgreSQL;
        protected override DbConnection CreateConnection( in ConnectionString secure ) => new NpgsqlConnection(secure);
    }
}
