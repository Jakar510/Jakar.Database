using Jakar.Database.Resx;
using ZXing;



namespace Jakar.Database;


public static class MigrationExtensions
{
    extension( WebApplication self )
    {
        public void InitializeLogging( bool parameterLoggingEnabled = false )
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

                await self.StartAsync(token)
                          .ConfigureAwait(false);

                await self.WaitForShutdownAsync(token)
                          .ConfigureAwait(false);
            }
            finally
            {
                await self.DisposeAsync()
                          .ConfigureAwait(false);
            }
        }

        public void TryUseMigrationsEndPoint( string endpoint = MigrationManager.MIGRATIONS )
        {
            if ( self.Environment.IsDevelopment() ) { self.UseMigrationsEndPoint(endpoint); }
        }

        public void UseMigrationsEndPoint( string endpoint = MigrationManager.MIGRATIONS ) => self.MapGet(endpoint, GetMigrationsAndRenderHtml);
    }



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
    public static PostgresType? ToDbPropertyType( this DbType?       type )                     => type?.ToDbPropertyType();
    public static bool          HasFlagValue( this     ColumnOptions type, ColumnOptions flag ) => ( type & flag ) != 0;
}



[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public class MigrationManager
{
    public const       string                                                MIGRATIONS           = "/_migrations";
    private readonly   SortedDictionary<ulong, Func<ulong, MigrationRecord>> __migrationFactories = new(Comparer<ulong>.Default);
    protected readonly Database                                              _db;
    protected          HashSet<MigrationRecord>?                             _records;
    internal static    ulong                                                 MigrationID => ++field;

    public HashSet<MigrationRecord> Records => _records ??= __migrationFactories.AsValueEnumerable()
                                                                                .Select(pair => pair.Value(pair.Key))
                                                                                .ToHashSet();


    public MigrationManager( Database db )
    {
        _db                               = db;
        __migrationFactories[MigrationID] = MigrationRecord.AddPostgreSqlExtensions;
        __migrationFactories[MigrationID] = MigrationRecord.CreateTable;
        __migrationFactories[MigrationID] = MigrationRecord.SetLastModified;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<MimeType>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<SupportedLanguage>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<SubscriptionStatus>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<DeviceCategory>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<DevicePlatform>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<DeviceTypes>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<DistanceUnit>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<ProgrammingLanguage>;
        __migrationFactories[MigrationID] = MigrationRecord.FromEnum<Status>;
        __migrationFactories[MigrationID] = ResxRowRecord.CreateTable;
        __migrationFactories[MigrationID] = FileRecord.CreateTable;
        __migrationFactories[MigrationID] = UserRecord.CreateTable;
        __migrationFactories[MigrationID] = RecoveryCodeRecord.CreateTable;
        __migrationFactories[MigrationID] = UserRecoveryCodeRecord.CreateTable;
        __migrationFactories[MigrationID] = RoleRecord.CreateTable;
        __migrationFactories[MigrationID] = UserRoleRecord.CreateTable;
        __migrationFactories[MigrationID] = GroupRecord.CreateTable;
        __migrationFactories[MigrationID] = UserGroupRecord.CreateTable;
        __migrationFactories[MigrationID] = AddressRecord.CreateTable;
        __migrationFactories[MigrationID] = UserAddressRecord.CreateTable;
    }

    public void Add( Func<ulong, MigrationRecord> func )
    {
        _records = null;
        __migrationFactories.Add(MigrationID, func);
    }


    public virtual async ValueTask<ContentHttpResult> AppliedMigrations( CancellationToken token )
    {
        ImmutableArray<MigrationRecord> records = await AllMigrations(token);
        string                          html    = CreateHtml(in records);
        return TypedResults.Content(html, "text/html", Encoding.UTF8);
    }


    public virtual async ValueTask<ImmutableArray<MigrationRecord>> AllMigrations( CancellationToken token )
    {
        await using NpgsqlConnection connection = await _db.ConnectAsync(token);
        return await AllMigrations(connection, null, token);
    }
    public virtual async ValueTask<ImmutableArray<MigrationRecord>> AllMigrations( NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken token )
    {
        SqlCommand<MigrationRecord>     command = MigrationRecord.SelectSql;
        await using NpgsqlCommand       cmd     = command.ToCommand(connection, transaction);
        await using NpgsqlDataReader    reader  = await cmd.ExecuteReaderAsync(token);
        ImmutableArray<MigrationRecord> records = await reader.CreateAsync<MigrationRecord>(__migrationFactories.Count, token);
        return records;
    }


    public async ValueTask ApplyMigrations( CancellationToken token = default )
    {
        await using NpgsqlConnection    connection  = await _db.ConnectAsync(token);
        await using NpgsqlTransaction   transaction = await connection.BeginTransactionAsync(token);
        ImmutableArray<MigrationRecord> applied     = await AllMigrations(connection, transaction, token);
        HashSet<MigrationRecord>        pending     = Records;
        pending.ExceptWith(applied);

        try
        {
            List<Task> tasks = new(pending.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach ( MigrationRecord record in pending.OrderBy(static x => x.MigrationID) ) { tasks.Add(Apply(connection, transaction, record, token)); }

            await Task.WhenAll(tasks.AsSpan());
            await transaction.CommitAsync(token);
        }
        catch ( Exception e )
        {
            await transaction.RollbackAsync(token);
            throw new InvalidOperationException("Failed to apply Records", e);
        }
    }
    public virtual async Task Apply( NpgsqlConnection connection, NpgsqlTransaction transaction, MigrationRecord self, CancellationToken token )
    {
        PostgresParameters parameters = PostgresParameters.Create<MigrationRecord>();
        parameters.Add(nameof(MigrationRecord.MigrationID),    self.MigrationID);
        parameters.Add(nameof(MigrationRecord.Description),    self.Description);
        parameters.Add(nameof(MigrationRecord.TableID),        self.TableID);
        parameters.Add(nameof(MigrationRecord.AppliedOn),      self.AppliedOn);

        SqlCommand<MigrationRecord> command = new(MigrationRecord.ApplySql, parameters);
        await command.ExecuteNonQueryAsync(connection, transaction, token);
    }


    public virtual string CreateHtml( ref readonly ImmutableArray<MigrationRecord> records )
    {
        StringWriter writer = new();
        CreateHtml(writer, in records);
        return writer.ToString();
    }
    public virtual void CreateHtml( TextWriter self, ref readonly ImmutableArray<MigrationRecord> records )
    {
        self.WriteLine("<!DOCTYPE html>");
        self.WriteLine("<html lang=\"en\">");
        self.WriteLine("  <head>");
        self.WriteLine("    <meta charset=\"UTF-8\"/>");
        self.WriteLine("    <title>Migration Records</title>");
        self.WriteLine("    <style>");
        self.WriteLine("      body { font-family: system-ui, sans-serif; background: #fafafa; color: #222; margin: 2em; }");
        self.WriteLine("      h2 { border-bottom: 2px solid #ccc; padding-bottom: 0.25em; }");
        self.WriteLine("      table { border-collapse: collapse; width: 100%; margin-top: 1em; }");
        self.WriteLine("      th, td { border: 1px solid #ddd; padding: 6px 10px; }");
        self.WriteLine("      th { background-color: #f4f4f4; text-align: left; }");
        self.WriteLine("      tr:nth-child(even) { background-color: #f9f9f9; }");
        self.WriteLine("      tr:hover { background-color: #f1f1f1; }");
        self.WriteLine("    </style>");
        self.WriteLine("  </head>");
        self.WriteLine("  <body>");
        self.WriteLine("    <h2>Migration Records</h2>");
        self.WriteLine("    <table>");
        self.WriteLine("      <thead>");
        self.WriteLine("        <tr>");
        self.WriteLine("          <th>ID</th>");
        self.WriteLine("          <th>Table</th>");
        self.WriteLine("          <th>Description</th>");
        self.WriteLine("          <th>Applied On</th>");
        self.WriteLine("        </tr>");
        self.WriteLine("      </thead>");
        self.WriteLine("      <tbody>");

        foreach ( ref readonly MigrationRecord migration in records.AsSpan() )
        {
            self.Write("        <tr>");
            self.Write("<td>");
            self.Write(migration.MigrationID);
            self.Write("</td><td>");
            HtmlEncode(self, migration.TableID);
            self.Write("</td><td>");
            HtmlEncode(self, migration.Description);
            self.Write("</td><td>");
            self.Write(migration.AppliedOn.ToString("u"));
            self.WriteLine("</td></tr>");
        }

        self.WriteLine("      </tbody>");
        self.WriteLine("    </table>");
        self.WriteLine("  </body>");
        self.WriteLine("</html>");
    }
    protected virtual void HtmlEncode( TextWriter self, string? value )
    {
        if ( string.IsNullOrEmpty(value) ) { return; }

        ReadOnlySpan<char> span  = value.AsSpan();
        int                start = 0;

        for ( int i = 0; i < span.Length; i++ )
        {
            string? entity = span[i] switch
                             {
                                 '&'  => "&amp;",
                                 '<'  => "&lt;",
                                 '>'  => "&gt;",
                                 '"'  => "&quot;",
                                 '\'' => "&#39;",
                                 _    => null
                             };

            if ( entity is not null )
            {
                if ( i > start ) { self.Write(span.Slice(start, i - start)); }

                self.Write(entity);
                start = i + 1;
            }
        }

        if ( start < span.Length ) { self.Write(span[start..]); }
    }
}
