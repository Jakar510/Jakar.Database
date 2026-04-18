// Jakar.Database :: Jakar.Database
// 02/24/2026  08:41

using ILogger = Microsoft.Extensions.Logging.ILogger;



namespace Jakar.Database;


public static class MigrationExtensions
{
    public static async Task<ContentHttpResult> GetMigrationsAndRenderHtml( [FromServices] Database db, CancellationToken token ) => await db.MigrationManager.AppliedMigrations(token);


    public static DbColumnType ToDbPropertyType( this DbType type ) => type switch
                                                                       {
                                                                           DbType.AnsiString            => DbColumnType.String,
                                                                           DbType.Binary                => DbColumnType.Binary,
                                                                           DbType.Byte                  => DbColumnType.Byte,
                                                                           DbType.Boolean               => DbColumnType.Boolean,
                                                                           DbType.Currency              => DbColumnType.Decimal,
                                                                           DbType.Date                  => DbColumnType.Date,
                                                                           DbType.Decimal               => DbColumnType.Decimal,
                                                                           DbType.Double                => DbColumnType.Double,
                                                                           DbType.Guid                  => DbColumnType.Guid,
                                                                           DbType.Int16                 => DbColumnType.Short,
                                                                           DbType.Int32                 => DbColumnType.Int,
                                                                           DbType.Int64                 => DbColumnType.Long,
                                                                           DbType.SByte                 => DbColumnType.SByte,
                                                                           DbType.Single                => DbColumnType.Double,
                                                                           DbType.String                => DbColumnType.String,
                                                                           DbType.StringFixedLength     => DbColumnType.String,
                                                                           DbType.Time                  => DbColumnType.Time,
                                                                           DbType.UInt16                => DbColumnType.UShort,
                                                                           DbType.UInt32                => DbColumnType.UInt,
                                                                           DbType.UInt64                => DbColumnType.Long,
                                                                           DbType.VarNumeric            => DbColumnType.Decimal,
                                                                           DbType.Xml                   => DbColumnType.Xml,
                                                                           DbType.AnsiStringFixedLength => DbColumnType.String,
                                                                           DbType.DateTime              => DbColumnType.DateTime,
                                                                           DbType.DateTime2             => DbColumnType.DateTime,
                                                                           DbType.DateTimeOffset        => DbColumnType.DateTimeOffset,
                                                                           DbType.Object                => DbColumnType.Json,
                                                                           _                            => throw new OutOfRangeException(type)
                                                                       };
    public static DbColumnType? ToDbPropertyType( this DbType? type ) => type?.ToDbPropertyType();



    extension( WebApplication self )
    {
        public void InitializeLogging( bool parameterLoggingEnabled = true )
        {
            ILoggerFactory factory = self.Services.GetRequiredService<ILoggerFactory>();
            NpgsqlLoggingConfiguration.InitializeLogging(factory, parameterLoggingEnabled);
        }

        public async ValueTask ApplyMigrations( CancellationToken token = default )
        {
            await using AsyncServiceScope scope  = self.Services.CreateAsyncScope();
            Database                      db     = scope.ServiceProvider.GetRequiredService<Database>();
            ILogger                       logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ApplyMigrations));
            await db.MigrationManager.ApplyMigrations(logger, token);
        }
        public async ValueTask RevertMigrations( long migrateDownToInclusive, CancellationToken token = default )
        {
            await using AsyncServiceScope scope  = self.Services.CreateAsyncScope();
            Database                      db     = scope.ServiceProvider.GetRequiredService<Database>();
            ILogger                       logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(RevertMigrations));
            await db.MigrationManager.RevertMigrations(logger, migrateDownToInclusive, token);
        }


        public void TryUseMigrationsEndPoint( string endpoint = MigrationManager.MIGRATIONS )
        {
            if ( self.Environment.IsDevelopment() ) { self.UseMigrationsEndPoint(endpoint); }
        }
        public void UseMigrationsEndPoint( string endpoint = MigrationManager.MIGRATIONS ) => self.MapGet(endpoint, GetMigrationsAndRenderHtml);


        public async Task RunWithMigrationsAsync( string[]? urls, Func<IServiceProvider, CancellationToken, ValueTask>? beforeRunHandler = null, string migrationsEndpoint = MigrationManager.MIGRATIONS, CancellationToken token = default )
        {
            self.TryUseMigrationsEndPoint(migrationsEndpoint);

            self.InitializeLogging();

            await self.ApplyMigrations(token);

            if ( urls is not null ) { self.UseUrls(urls); }

            if ( beforeRunHandler is not null )
            {
                await using AsyncServiceScope scope = self.Services.CreateAsyncScope();
                await beforeRunHandler(scope.ServiceProvider, token);
            }

            await self.StartAsync(token).ConfigureAwait(false);

            await self.WaitForShutdownAsync(token).ConfigureAwait(false);
        }
    }
}
