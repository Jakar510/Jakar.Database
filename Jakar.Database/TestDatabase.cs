// Jakar.Extensions :: Experiments
// 09/28/2023  10:02 AM

namespace Jakar.Database;


internal sealed class TestDatabase( IConfiguration configuration, IOptions<DbOptions> options, IFusionCache cache ) : Database(configuration, options, cache), IAppID
{
    public static readonly TelemetrySource Source;
    public static          Guid            AppID        { get; }
    public static          string          AppName      => nameof(TestDatabase);
    public static          AppVersion      AppVersion   { get; }
    public override        DatabaseType    DatabaseType => DatabaseType.PostgreSQL;


    static TestDatabase()
    {
        AppID      = Guid.NewGuid();
        AppVersion = new AppVersion(1, 0, 0, 1);
        Source     = new TelemetrySource(AppVersion, AppID, AppName, "Jakar.Database");
    }
    protected override DbConnection CreateConnection( in ConnectionString secure ) => new NpgsqlConnection(secure);


    public static WebApplicationBuilder Create()
    {
        WebApplicationBuilder        builder          = WebApplication.CreateBuilder();
        SecuredStringResolverOptions connectionString = "User ID=dev;Password=dev;Host=localhost;Port=5432;Database=jakar_database_sample;Include Error Detail=true;";

        DbOptions options = new()
                            {
                                TelemetrySource          = Source,
                                ConnectionStringResolver = connectionString,
                                CommandTimeout           = 30,
                                TokenIssuer              = AppName,
                                TokenAudience            = AppName,
                                LoggerOptions            = new AppLoggerOptions()
                            };

        builder.AddDatabase<TestDatabase>(options);
        builder.Services.AddHostedService<TesterService>();
        return builder;
    }
    [MustDisposeResource] public static WebApplication Create( [MustDisposeResource] out TestDatabase database )
    {
        WebApplicationBuilder builder = Create();
        WebApplication        app     = builder.Build();
        database = app.Services.GetRequiredService<TestDatabase>();
        return app;
    }


    public static string TestFormat<T>( T value, scoped in Span<char> destination, string format )
        where T : ISpanFormattable
    {
        value.TryFormat(destination, out int charsWritten, format, CultureInfo.InvariantCulture);
        return destination[..charsWritten].ToString();
    }
    public static void TestFormats( scoped in Span<char> destination )
    {
        Console.WriteLine();
        WriteLine(TestFormat(DateTimeOffset.UtcNow, in destination, "o"));
        WriteLine(TestFormat(DateTimeOffset.UtcNow, in destination, "r"));
        WriteLine(TestFormat(DateTimeOffset.UtcNow, in destination, "s"));
        WriteLine(TestFormat(DateTimeOffset.UtcNow, in destination, "u"));
        Console.WriteLine();
        WriteLine(TestFormat(TimeSpan.FromDays(5.1654654), in destination, "c"));
        WriteLine(TestFormat(TimeSpan.FromDays(5.1654654), in destination, "t"));
        WriteLine(TestFormat(TimeSpan.FromDays(5.1654654), in destination, "g"));
        Console.WriteLine();
    }
    public static void WriteLine( string line, [CallerArgumentExpression(nameof(line))] string paramName = EMPTY )
    {
        string header = new('=', paramName.Length + 20);
        Console.WriteLine();
        Console.WriteLine(header);
        Console.WriteLine(paramName.PadLeft(header.Length - 10).PadRight(header.Length));
        Console.WriteLine(header);
        Console.WriteLine();
        Console.WriteLine(line);
        Console.WriteLine();
    }
    public static void TestSQL()
    {
        const string           ADMIN      = "Admin";
        DateTimeOffset         date       = DateTimeOffset.UtcNow - TimeSpan.FromDays(5);
        RecordID<UserRecord>   userID     = RecordID<UserRecord>.New();
        RecordID<RoleRecord>   id         = RecordID<RoleRecord>.New();
        RecordPair<RoleRecord> pair       = new(id, date);
        RoleRecord             record     = new(ADMIN, ADMIN, Randoms.RandomString(10), new UserRights(""), id, userID, date);
        CommandParameters      parameters = CommandParameters.Create<RoleRecord>();
        parameters.Add(nameof(RoleRecord.NameOfRole),     ADMIN);
        parameters.Add(nameof(RoleRecord.NormalizedName), "Administrator");


        WriteLine(SqlCommand.GetRandom<RoleRecord>().ToString());
        WriteLine(SqlCommand.GetRandom<RoleRecord>(userID).ToString());
        WriteLine(SqlCommand.WherePaged<RoleRecord>(parameters, 0, 10).ToString());
        WriteLine(SqlCommand.WherePaged<RoleRecord>(userID,     0, 10).ToString());
        WriteLine(SqlCommand.WherePaged<RoleRecord>(0,          10).ToString());
        WriteLine(SqlCommand.WherePaged<RoleRecord>(date, 0, 10).ToString());
        
        WriteLine(SqlCommand.Where<RoleRecord>(parameters).ToString());
        
        WriteLine(SqlCommand.Parse<RoleRecord>($"SELECT * FROM {RoleRecord.TableName} WHERE {nameof(RoleRecord.NameOfRole)} = @{ADMIN};").ToString());
        
        WriteLine(SqlCommand.Get(id).ToString());
        WriteLine(SqlCommand.Get(id, RecordID<RoleRecord>.New()).ToString());
        
        WriteLine(SqlCommand.Get<RoleRecord>(parameters).ToString());
        WriteLine(SqlCommand.GetAll<RoleRecord>().ToString());
        WriteLine(SqlCommand.GetFirst<RoleRecord>().ToString());
        WriteLine(SqlCommand.GetLast<RoleRecord>().ToString());
        WriteLine(SqlCommand.GetCount<RoleRecord>().ToString());
        WriteLine(SqlCommand.GetSortedID<RoleRecord>().ToString());
        WriteLine(SqlCommand.GetExists<RoleRecord>(parameters).ToString());
        WriteLine(SqlCommand.GetDelete<RoleRecord>(parameters).ToString());
        WriteLine(SqlCommand.GetDelete(id).ToString());
        WriteLine(SqlCommand.GetDelete(id, RecordID<RoleRecord>.New()).ToString());
        WriteLine(SqlCommand.GetDeleteAll<RoleRecord>().ToString());
        WriteLine(SqlCommand.GetNext(pair).ToString());
        WriteLine(SqlCommand.GetNextID(pair).ToString());
        WriteLine(SqlCommand.GetCopy<RoleRecord>().ToString());
        WriteLine(SqlCommand.GetInsert(record).ToString());
        WriteLine(SqlCommand.GetInsert(record, record).ToString());
        WriteLine(SqlCommand.GetUpdate(record).ToString());
        WriteLine(SqlCommand.GetTryInsert(record, parameters).ToString());
        WriteLine(SqlCommand.InsertOrUpdate(record, in parameters).ToString());
    }


    public static async Task TestAsync( CancellationToken token = default )
    {
        WebApplicationBuilder      builder = Create();
        await using WebApplication app     = builder.Build();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseTelemetry();

        app.MapGet("/", static () => DateTimeOffset.UtcNow);

        await app.RunWithMigrationsAsync(["localhost:8081"], token: token).ConfigureAwait(false);
    }



    public enum TestRight
    {
        Admin,
        Read,
        Write
    }



    public sealed class TesterService( TestDatabase db ) : IHostedService
    {
        public async Task StartAsync( CancellationToken token )
        {
            ( UserRecord admin, UserRecord user )             = await Add_Users(db, token);
            ( RoleRecord adminRole, RoleRecord userRole )     = await Add_Roles(db, admin, token);
            ( GroupRecord adminGroup, GroupRecord userGroup ) = await Add_Group(db, admin, token);
            ImmutableArray<UserRoleRecord>  userRoles  = await Add_Roles(db, user, [adminRole, userRole], token);
            ImmutableArray<UserGroupRecord> userGroups = await Add_Groups(db, user, [adminGroup, userGroup], token);
            ( AddressRecord address, UserAddressRecord userAddress ) = await Add_Address(db, user, token);
            FileRecord              file          = await Add_File(db, user, token);
            UserLoginProviderRecord loginProvider = await Add_UserLoginProvider(db, user, token);
            ( ImmutableArray<RecoveryCodeRecord> recoveryCodes, ImmutableArray<UserRecoveryCodeRecord> userRecoveryCodes ) = await Add_RecoveryCodes(db, user, token);
        }
        public Task StopAsync( CancellationToken token ) => Task.CompletedTask;


        public static async ValueTask<(UserRecord Admin, UserRecord User)> Add_Users( Database db, CancellationToken token = default )
        {
            UserRecord admin = UserRecord.Create("Admin", "Admin", Permissions<TestRight>.SA());
            UserRecord user  = UserRecord.Create("User",  "User",  Permissions<TestRight>.Create(TestRight.Read));

            admin = await db.Users.Insert(admin, token);
            user  = await db.Users.Insert(user,  token);

            return ( admin, user );
        }

        public static async ValueTask<(RoleRecord Admin, RoleRecord User)> Add_Roles( Database db, UserRecord adminUser, CancellationToken token = default )
        {
            RoleRecord admin = RoleRecord.Create("Admin", Permissions<TestRight>.SA(),                   "Admins", adminUser);
            RoleRecord user  = RoleRecord.Create("User",  Permissions<TestRight>.Create(TestRight.Read), "Users",  adminUser);

            return ( await db.Roles.Insert(admin, token), await db.Roles.Insert(user, token) );
        }

        public static async ValueTask<ImmutableArray<UserRoleRecord>> Add_Roles( Database db, UserRecord user, RoleRecord[] roles, CancellationToken token = default )
        {
            await using DbConnectionContext context = await db.ConnectAsync(token, db.TransactionIsolationLevel);

            try
            {
                ImmutableArray<UserRoleRecord> records = UserRoleRecord.Create(user, roles.AsSpan());
                await UserRoleRecord.TryAdd(context, records, token);
                return records;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }

        public static async ValueTask<(GroupRecord Admin, GroupRecord User)> Add_Group( Database db, UserRecord adminUser, CancellationToken token = default )
        {
            GroupRecord admin = GroupRecord.Create("Admin", Permissions<TestRight>.SA(),                   "Admin", adminUser);
            GroupRecord user  = GroupRecord.Create("User",  Permissions<TestRight>.Create(TestRight.Read), "User",  adminUser);

            return ( await db.Groups.Insert(admin, token), await db.Groups.Insert(user, token) );
        }

        public static async ValueTask<ImmutableArray<UserGroupRecord>> Add_Groups( Database db, UserRecord user, GroupRecord[] groups, CancellationToken token = default )
        {
            await using DbConnectionContext context = await db.ConnectAsync(token, db.TransactionIsolationLevel);

            try
            {
                ImmutableArray<UserGroupRecord> records = UserGroupRecord.Create(user, groups.AsSpan());
                await UserGroupRecord.TryAdd(context, records, token);
                return records;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }

        public static async ValueTask<(AddressRecord, UserAddressRecord)> Add_Address( Database db, UserRecord user, CancellationToken token = default )
        {
            await using DbConnectionContext context = await db.ConnectAsync(token, db.TransactionIsolationLevel);

            try
            {
                AddressRecord address = AddressRecord.Create("address line one", "", "city", "state", "postal", "country");
                address = await db.Addresses.Insert(context, address, token);
                UserAddressRecord link = UserAddressRecord.Create(user, address);
                await UserAddressRecord.TryAdd(context, link, token);
                return ( address, link );
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }

        public static async ValueTask<FileRecord> Add_File( Database db, UserRecord user, CancellationToken token = default )
        {
            FileRecord file = new(RecordID<FileRecord>.New(), DateTimeOffset.UtcNow)
                              {
                                  FileName        = "file name",
                                  FileDescription = "file description",
                                  FileType        = "file type",
                                  FileSize        = 0,
                                  Hash            = "hash",
                                  MimeType        = MimeType.Unknown,
                                  Payload         = "payload",
                                  FullPath        = "full file system path"
                              };

            file = await db.Files.Insert(file, token);

            user.ImageID = file;
            await db.Users.Update(user, token);

            return file;
        }

        public static async ValueTask<UserLoginProviderRecord> Add_UserLoginProvider( Database db, UserRecord user, CancellationToken token = default )
        {
            UserLoginProviderRecord record = new("login provider", "provider display name", "provider key", "value", RecordID<UserLoginProviderRecord>.New(), user, DateTimeOffset.UtcNow);

            return await db.UserLoginProviders.Insert(record, token);
        }

        public static async ValueTask<(ImmutableArray<RecoveryCodeRecord>, ImmutableArray<UserRecoveryCodeRecord>)> Add_RecoveryCodes( Database db, UserRecord user, CancellationToken token = default )
        {
            await using DbConnectionContext context = await db.ConnectAsync(token, db.TransactionIsolationLevel);

            try
            {
                RecoveryCodeRecord.Codes               codes   = RecoveryCodeRecord.Create(user, 10);
                ImmutableArray<RecoveryCodeRecord>     records = await db.RecoveryCodes.Insert(context, codes.Values, token);
                ImmutableArray<UserRecoveryCodeRecord> links   = UserRecoveryCodeRecord.Create(user, records.AsSpan());
                await UserRecoveryCodeRecord.TryAdd(context, links, token);
                await context.CommitAsync(token);
                return ( records, links );
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
    }
}
