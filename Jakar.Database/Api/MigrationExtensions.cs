// Jakar.Database :: Jakar.Database
// 02/24/2026  08:41

namespace Jakar.Database;


public static class MigrationExtensions
{
    public static async Task<ContentHttpResult> GetMigrationsAndRenderHtml( [FromServices] Database db, CancellationToken token ) => await db.MigrationManager.AppliedMigrations(token);


    public static PostgresType ToDbPropertyType( this DbType type ) => type switch
                                                                       {
                                                                           DbType.AnsiString            => PostgresType.String,
                                                                           DbType.Binary                => PostgresType.Binary,
                                                                           DbType.Byte                  => PostgresType.Byte,
                                                                           DbType.Boolean               => PostgresType.Boolean,
                                                                           DbType.Currency              => PostgresType.Decimal,
                                                                           DbType.Date                  => PostgresType.Date,
                                                                           DbType.Decimal               => PostgresType.Decimal,
                                                                           DbType.Double                => PostgresType.Double,
                                                                           DbType.Guid                  => PostgresType.Guid,
                                                                           DbType.Int16                 => PostgresType.Short,
                                                                           DbType.Int32                 => PostgresType.Int,
                                                                           DbType.Int64                 => PostgresType.Long,
                                                                           DbType.SByte                 => PostgresType.SByte,
                                                                           DbType.Single                => PostgresType.Double,
                                                                           DbType.String                => PostgresType.String,
                                                                           DbType.StringFixedLength     => PostgresType.String,
                                                                           DbType.Time                  => PostgresType.Time,
                                                                           DbType.UInt16                => PostgresType.UShort,
                                                                           DbType.UInt32                => PostgresType.UInt,
                                                                           DbType.UInt64                => PostgresType.Long,
                                                                           DbType.VarNumeric            => PostgresType.Decimal,
                                                                           DbType.Xml                   => PostgresType.Xml,
                                                                           DbType.AnsiStringFixedLength => PostgresType.String,
                                                                           DbType.DateTime              => PostgresType.DateTime,
                                                                           DbType.DateTime2             => PostgresType.DateTime,
                                                                           DbType.DateTimeOffset        => PostgresType.DateTimeOffset,
                                                                           DbType.Object                => PostgresType.Json,
                                                                           _                            => throw new OutOfRangeException(type)
                                                                       };
    public static PostgresType? ToDbPropertyType( this DbType? type ) => type?.ToDbPropertyType();



    extension( WebApplication self )
    {
        public void InitializeLogging( bool parameterLoggingEnabled = true )
        {
            ILoggerFactory factory = self.Services.GetRequiredService<ILoggerFactory>();
            NpgsqlLoggingConfiguration.InitializeLogging(factory, parameterLoggingEnabled);
        }

        public async ValueTask ApplyMigrations( CancellationToken token = default )
        {
            await using AsyncServiceScope scope = self.Services.CreateAsyncScope();
            Database                      db    = scope.ServiceProvider.GetRequiredService<Database>();
            await db.MigrationManager.ApplyMigrations(token);
        }

        public async Task RunWithMigrationsAsync( string[]? urls, Func<IServiceProvider, CancellationToken, ValueTask>? beforeRunHandler = null, string migrationsEndpoint = MigrationManager.MIGRATIONS, CancellationToken token = default )
        {
            self.TryUseMigrationsEndPoint(migrationsEndpoint);

            try
            {
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
            finally { await self.DisposeAsync().ConfigureAwait(false); }
        }

        public void TryUseMigrationsEndPoint( string endpoint = MigrationManager.MIGRATIONS )
        {
            if ( self.Environment.IsDevelopment() ) { self.UseMigrationsEndPoint(endpoint); }
        }

        public void UseMigrationsEndPoint( string endpoint = MigrationManager.MIGRATIONS ) => self.MapGet(endpoint, GetMigrationsAndRenderHtml);
    }
}
