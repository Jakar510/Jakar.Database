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
        ImmutableArray<UserRoleRecord>  userRoles  = await Add_UserRoles(db, user, [adminRole, userRole], token);
        ImmutableArray<UserGroupRecord> userGroups = await Add_Groups(db, user, [adminGroup, userGroup], token);
        ( AddressRecord address, UserAddressRecord userAddress ) = await Add_Address(db, user, token);
        FileRecord              file          = await Add_File(db, user, token);
        UserLoginProviderRecord loginProvider = await Add_UserLoginProvider(db, user, token);
        ( ImmutableArray<RecoveryCodeRecord> recoveryCodes, ImmutableArray<UserRecoveryCodeRecord> userRecoveryCodes ) = await Add_RecoveryCodes(db, user, token);
    }


    private static async ValueTask<(UserRecord Admin, UserRecord User)> Add_Users( Database db, CancellationToken token = default )
    {
        UserRecord admin = UserRecord.Create("Admin", "Admin", Permissions<TestRight>.SA());
        UserRecord user  = UserRecord.Create("User",  "User",  Permissions<TestRight>.Create(TestRight.Read));

        admin = await db.Users.Insert(admin, token);
        user  = await db.Users.Insert(user,  token);

        return ( admin, user );
    }

    private static async ValueTask<(RoleRecord Admin, RoleRecord User)> Add_Roles( Database db, UserRecord adminUser, CancellationToken token = default )
    {
        RoleRecord admin = RoleRecord.Create("Admin", Permissions<TestRight>.SA(),                   "Admins", adminUser);
        RoleRecord user  = RoleRecord.Create("User",  Permissions<TestRight>.Create(TestRight.Read), "Users",  adminUser);

        return ( await db.Roles.Insert(admin, token), await db.Roles.Insert(user, token) );
    }

    private static async ValueTask<ImmutableArray<UserRoleRecord>> Add_UserRoles( Database db, UserRecord user, RoleRecord[] roles, CancellationToken token = default )
    {
        await using NpgsqlConnection  connection  = await db.ConnectAsync(token);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

        try
        {
            ImmutableArray<UserRoleRecord> records = UserRoleRecord.Create(user, roles.AsSpan());
            await UserRoleRecord.TryAdd(connection, transaction, records, token);
            return records;
        }
        catch ( Exception )
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    private static async ValueTask<(GroupRecord Admin, GroupRecord User)> Add_Group( Database db, UserRecord adminUser, CancellationToken token = default )
    {
        GroupRecord admin = GroupRecord.Create("Admin", Permissions<TestRight>.SA(),                   "Admin", adminUser);
        GroupRecord user  = GroupRecord.Create("User",  Permissions<TestRight>.Create(TestRight.Read), "User",  adminUser);

        return ( await db.Groups.Insert(admin, token), await db.Groups.Insert(user, token) );
    }

    private static async ValueTask<ImmutableArray<UserGroupRecord>> Add_Groups( Database db, UserRecord user, GroupRecord[] groups, CancellationToken token = default )
    {
        await using NpgsqlConnection  connection  = await db.ConnectAsync(token);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

        try
        {
            ImmutableArray<UserGroupRecord> records = UserGroupRecord.Create(user, groups.AsSpan());
            await UserGroupRecord.TryAdd(connection, transaction, records, token);
            return records;
        }
        catch ( Exception )
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    private static async ValueTask<(AddressRecord, UserAddressRecord)> Add_Address( Database db, UserRecord user, CancellationToken token = default )
    {
        await using NpgsqlConnection  connection  = await db.ConnectAsync(token);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

        try
        {
            AddressRecord address = AddressRecord.Create("address line one", "", "city", "state", "postal", "country");
            address = await db.Addresses.Insert(connection, transaction, address, token);
            UserAddressRecord link = UserAddressRecord.Create(user, address);
            await UserAddressRecord.TryAdd(connection, transaction, link, token);
            return ( address, link );
        }
        catch ( Exception )
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    private static async ValueTask<FileRecord> Add_File( Database db, UserRecord user, CancellationToken token = default )
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
                              FullPath        = "full file system path",
                          };

        file = await db.Files.Insert(file, token);

        user.ImageID = file;
        await db.Users.Update(user, token);

        return file;
    }

    private static async ValueTask<UserLoginProviderRecord> Add_UserLoginProvider( Database db, UserRecord user, CancellationToken token = default )
    {
        UserLoginProviderRecord record = new("login provider", "provider display name", "provider key", "value", RecordID<UserLoginProviderRecord>.New(), user, DateTimeOffset.UtcNow);

        return await db.UserLoginProviders.Insert(record, token);
    }

    private static async ValueTask<(ImmutableArray<RecoveryCodeRecord>, ImmutableArray<UserRecoveryCodeRecord>)> Add_RecoveryCodes( Database db, UserRecord user, CancellationToken token = default )
    {
        await using NpgsqlConnection  connection  = await db.ConnectAsync(token);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

        try
        {
            RecoveryCodeRecord.Codes               codes   = RecoveryCodeRecord.Create(user, 10);
            ImmutableArray<RecoveryCodeRecord>     records = await db.RecoveryCodes.Insert(connection, transaction, codes.Values, token);
            ImmutableArray<UserRecoveryCodeRecord> links   = UserRecoveryCodeRecord.Create(user, records.AsSpan());
            await UserRecoveryCodeRecord.TryAdd(connection, transaction, links, token);
            await transaction.CommitAsync(token);
            return ( records, links );
        }
        catch ( Exception )
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }



    public enum TestRight
    {
        Admin,
        Read,
        Write
    }
}
