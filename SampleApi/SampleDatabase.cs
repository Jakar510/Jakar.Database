// Jakar.Database :: SampleApi
// 02/04/2026  10:13

namespace SampleApi;


internal sealed class SampleDatabase( IConfiguration configuration, IOptions<DbOptions> options, FusionCache cache ) : Database(configuration, options, cache), IAppID
{
    public static Guid       AppID      { get; } = Guid.NewGuid();
    public static string     AppName    => nameof(SampleDatabase);
    public static AppVersion AppVersion { get; } = new(1, 0, 0, 1);


    protected override NpgsqlConnection CreateConnection( in SecuredString secure ) => new(secure);


    public static SampleDatabase Create( [MustDisposeResource] out WebApplication app )
    {
        WebApplicationBuilder        builder          = WebApplication.CreateBuilder();
        SecuredStringResolverOptions connectionString = $"User ID=dev;Password=dev;Host=localhost;Port=5432;Database={AppName}";

        DbOptions options = new()
                            {
                                ConnectionStringResolver = connectionString,
                                CommandTimeout           = 30,
                                TokenIssuer              = AppName,
                                TokenAudience            = AppName
                            };

        builder.AddDatabase<SampleDatabase>(options);

        app = builder.Build();
        return app.Services.GetRequiredService<SampleDatabase>();
    }


    public static void PrintCreateTables()
    {
        printSql(TableMetaData<UserRecord>.CreateTable());
        printSql(TableMetaData<AddressRecord>.CreateTable());
        printSql(TableMetaData<UserAddressRecord>.CreateTable());
        printSql(TableMetaData<GroupRecord>.CreateTable());
        printSql(TableMetaData<UserGroupRecord>.CreateTable());
        printSql(TableMetaData<RoleRecord>.CreateTable());
        printSql(TableMetaData<UserRoleRecord>.CreateTable());
        printSql(TableMetaData<RecoveryCodeRecord>.CreateTable());
        printSql(TableMetaData<UserRecoveryCodeRecord>.CreateTable());
        printSql(TableMetaData<FileRecord>.CreateTable());
        printSql(TableMetaData<UserLoginProviderRecord>.CreateTable());
        printSql(TableMetaData<ResxRowRecord>.CreateTable());

        return;

        static void printSql( string sql, [CallerArgumentExpression(nameof(sql))] string variableName = EMPTY )
        {
            const string BOUNDARY = "================================";
            Console.WriteLine(BOUNDARY);
            Console.WriteLine();
            Console.WriteLine(variableName);
            Console.WriteLine();
            Console.WriteLine(sql);
            Console.WriteLine();
            Console.WriteLine(BOUNDARY);
        }
    }

    public static async ValueTask TestAll( WebApplication app, CancellationToken token = default )
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await TestAll(scope.ServiceProvider, token);
    }
    public static async ValueTask TestAll( IServiceProvider provider, CancellationToken token = default )
    {
        SampleDatabase db = provider.GetRequiredService<SampleDatabase>();
        ( UserRecord admin, UserRecord user )             = await Add_Users(db, token);
        ( RoleRecord adminRole, RoleRecord userRole )     = await Add_Roles(db, admin, token);
        ( GroupRecord adminGroup, GroupRecord userGroup ) = await Add_Group(db, admin, token);
        ImmutableArray<UserRoleRecord>  userRoles  = await Add_Roles(db, user, [adminRole, userRole],   token);
        ImmutableArray<UserGroupRecord> userGroups = await Add_Roles(db, user, [adminGroup, userGroup], token);
        ( AddressRecord address, UserAddressRecord userAddress ) = await Add_Address(db, user, token);
        FileRecord              file          = await Add_File(db, user, token);
        UserLoginProviderRecord loginProvider = await Add_UserLoginProvider(db, user, token);
        ( ImmutableArray<RecoveryCodeRecord> recoveryCodes, ImmutableArray<UserRecoveryCodeRecord> userRecoveryCodes ) = await Add_RecoveryCodes(db, user, token);
    }
    private static async ValueTask<(ImmutableArray<RecoveryCodeRecord> records, ImmutableArray<UserRecoveryCodeRecord> results)> Add_RecoveryCodes( Database db, UserRecord user, CancellationToken token = default )
    {
        RecoveryCodeRecord.Codes codes = RecoveryCodeRecord.Create(user, 10);

        ImmutableArray<RecoveryCodeRecord>     records = await db.RecoveryCodes.Insert(codes.Values, token);
        ImmutableArray<UserRecoveryCodeRecord> memory  = UserRecoveryCodeRecord.Create(user, records.AsSpan());
        ImmutableArray<UserRecoveryCodeRecord> results = await db.UserRecoveryCodes.Insert(memory.AsMemory(), token);

        return ( records, results );
    }
    private static async ValueTask<UserLoginProviderRecord> Add_UserLoginProvider( Database db, UserRecord user, CancellationToken token = default )
    {
        UserLoginProviderRecord record = new("login provider", "provider display name", "provider key", "value", RecordID<UserLoginProviderRecord>.New(), user, DateTimeOffset.UtcNow);
        record = await db.UserLoginProviders.Insert(record, token);
        return record;
    }
    private static async ValueTask<FileRecord> Add_File( Database db, UserRecord user, CancellationToken token = default )
    {
        FileRecord record = new(RecordID<FileRecord>.New(), DateTimeOffset.UtcNow)
                            {
                                FileName        = "file name",
                                FileDescription = "file description",
                                FileType        = "file type",
                                FileSize        = 0,
                                Hash            = "hash",
                                MimeType        = MimeType.Unknown,
                                Payload         = "payload",
                                FullPath        = "full file system path",
                            };

        record       = await db.Files.Insert(record, token);
        user.ImageID = record;
        await db.Users.Update(user, token);
        return record;
    }
    private static async ValueTask<(AddressRecord result, UserAddressRecord userAddress)> Add_Address( Database db, UserRecord user, CancellationToken token = default )
    {
        AddressRecord     record      = AddressRecord.Create("address line one", "", "city", "state or province", "postal code with optional extension", "country");
        AddressRecord     result      = await db.Addresses.Insert(record, token);
        UserAddressRecord userAddress = await db.UserAddresses.Insert(UserAddressRecord.Create(user, result), token);
        return ( result, userAddress );
    }
    private static async ValueTask<ImmutableArray<UserGroupRecord>> Add_Roles( Database db, UserRecord user, GroupRecord[] roles, CancellationToken token = default )
    {
        ImmutableArray<UserGroupRecord> records = UserGroupRecord.Create(user, roles.AsSpan());
        ImmutableArray<UserGroupRecord> results = await db.UserGroups.Insert(records, token);

        return results;
    }
    private static async ValueTask<ImmutableArray<UserRoleRecord>> Add_Roles( Database db, UserRecord user, RoleRecord[] roles, CancellationToken token = default )
    {
        ImmutableArray<UserRoleRecord> records = UserRoleRecord.Create(user, roles.AsSpan());
        ImmutableArray<UserRoleRecord> results = await db.UserRoles.Insert(records, token);

        return results;
    }
    private static async ValueTask<(RoleRecord Admin, RoleRecord User)> Add_Roles( Database db, UserRecord adminUser, CancellationToken token = default )
    {
        RoleRecord admin = RoleRecord.Create("Admin", Permissions<TestRight>.SA(),                   "Admins", adminUser);
        RoleRecord user  = RoleRecord.Create("User",  Permissions<TestRight>.Create(TestRight.Read), "Users",  adminUser);
        return ( await db.Roles.Insert(admin, token), await db.Roles.Insert(user, token) );
    }
    private static async ValueTask<(GroupRecord Admin, GroupRecord User)> Add_Group( Database db, UserRecord adminUser, CancellationToken token = default )
    {
        GroupRecord admin = GroupRecord.Create("Admin", Permissions<TestRight>.SA(),                   "Admin", adminUser);
        GroupRecord user  = GroupRecord.Create("User",  Permissions<TestRight>.Create(TestRight.Read), "User",  adminUser);
        return ( await db.Groups.Insert(admin, token), await db.Groups.Insert(user, token) );
    }
    private static async ValueTask<(UserRecord Admin, UserRecord User)> Add_Users( Database db, CancellationToken token = default )
    {
        UserRecord admin = UserRecord.Create("Admin", "Admin", Permissions<TestRight>.SA());
        UserRecord user  = UserRecord.Create("User",  "User",  Permissions<TestRight>.Create(TestRight.Read));

        using ( Telemetry.DbSource.StartActivity("Users.Add.SU") ) { admin = await db.Users.Insert(admin, token); }

        using ( Telemetry.DbSource.StartActivity("Users.Add.User") ) { user = await db.Users.Insert(user, token); }

        return ( admin, user );
    }



    public enum TestRight
    {
        Admin,
        Read,
        Write
    }
}
