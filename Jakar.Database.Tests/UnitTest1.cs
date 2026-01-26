using Jakar.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using static Jakar.Database.TestDatabase;



namespace Jakar.Database.Tests;


[]
public sealed class Tests : Assert
{
    [SetUp] public async Task Setup()
    {
        PostgreSqlContainer? postgreSqlContainer = new PostgreSqlBuilder("postgres:18.1").Build();
        await postgreSqlContainer.StartAsync();
    }
    [Test] public async Task ApplyAndValidateMigrations() { await TestDatabase.TestAsync(); }
}



[TestFixture]
[NonParallelizable] // DB + migrations → avoid parallel runs
public sealed class TestDatabaseTests : Assert
{
    private WebApplication __app   = null!;
    private IServiceScope  __scope = null!;
    private TestDatabase   __db    = null!;

    [OneTimeSetUp] public async Task OneTimeSetup()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        SecuredStringResolverOptions connectionString = $"User ID=dev;Password=dev;Host=localhost;Port=5432;Database={TestDatabase.AppName}";

        DbOptions options = new()
                            {
                                ConnectionStringResolver = connectionString,
                                CommandTimeout           = 30,
                                TokenIssuer              = TestDatabase.AppName,
                                TokenAudience            = TestDatabase.AppName
                            };

        builder.AddDatabase<TestDatabase>(options);

        __app = builder.Build();
        __app.UseDefaults();

        __app.MapGet("/",     () => DateTimeOffset.UtcNow);
        __app.MapGet("/Ping", () => DateTimeOffset.UtcNow);

        await __app.ApplyMigrations();

        __scope = __app.Services.CreateScope();
        __db    = __scope.ServiceProvider.GetRequiredService<TestDatabase>();
    }

    [OneTimeTearDown] public async Task OneTimeTearDown()
    {
        __scope.Dispose();
        await __app.DisposeAsync();
    }


    // ------------------------------------------------------------
    // USERS
    // ------------------------------------------------------------

    [Test] public async Task Can_Create_Admin_And_User()
    {
        ( UserRecord admin, UserRecord user ) = await Add_Users(__db);

        Assert.Multiple(() =>
                        {
                            Assert.That(admin.ID, Is.Not.Null);
                            Assert.That(user.ID,  Is.Not.Null);

                            Assert.That(admin.Rights,
                                        Is.EqualTo(Permissions<TestRight>.SA()
                                                                         .ToString()));
                        });
    }

    // ------------------------------------------------------------
    // ROLES
    // ------------------------------------------------------------

    [Test] public async Task Can_Create_Roles_And_Assign_To_User()
    {
        ( UserRecord admin, UserRecord user )         = await Add_Users(__db);
        ( RoleRecord adminRole, RoleRecord userRole ) = await Add_Roles(__db, admin);

        UserRoleRecord[] roles = await Add_Roles(__db, user, [adminRole, userRole]);

        Assert.That(roles, Has.Length.EqualTo(2));
    }

    // ------------------------------------------------------------
    // GROUPS
    // ------------------------------------------------------------

    [Test] public async Task Can_Create_Groups_And_Assign_To_User()
    {
        ( UserRecord admin, UserRecord user )             = await Add_Users(__db);
        ( GroupRecord adminGroup, GroupRecord userGroup ) = await Add_Group(__db, admin);

        UserGroupRecord[] groups = await Add_Groups(__db, user, [adminGroup, userGroup]);

        Assert.That(groups, Has.Length.EqualTo(2));
    }

    // ------------------------------------------------------------
    // ADDRESS
    // ------------------------------------------------------------

    [Test] public async Task Can_Add_Address_To_User()
    {
        ( _, UserRecord user ) = await Add_Users(__db);

        ( AddressRecord address, UserAddressRecord link ) = await Add_Address(__db, user);

        Assert.That(address.ID, Is.Not.Null);
        Assert.That(link.ID,    Is.EqualTo(user.ID));
    }

    // ------------------------------------------------------------
    // FILE
    // ------------------------------------------------------------

    [Test] public async Task Can_Assign_File_As_User_Image()
    {
        ( _, UserRecord user ) = await Add_Users(__db);

        FileRecord file = await Add_File(__db, user);

        Assert.That(file.ID,      Is.Not.Null);
        Assert.That(user.ImageID, Is.EqualTo(file));
    }

    // ------------------------------------------------------------
    // LOGIN PROVIDER
    // ------------------------------------------------------------

    [Test] public async Task Can_Add_Login_Provider()
    {
        ( _, UserRecord user ) = await Add_Users(__db);

        UserLoginProviderRecord record = await Add_UserLoginProvider(__db, user);

        Assert.That(record.ID, Is.EqualTo(user.ID));
    }

    // ------------------------------------------------------------
    // RECOVERY CODES
    // ------------------------------------------------------------

    [Test] public async Task Can_Create_Recovery_Codes()
    {
        ( _, UserRecord user ) = await Add_Users(__db);

        var (codes, userCodes) = await Add_RecoveryCodes(__db, user);

        Assert.That(codes,     Has.Length.EqualTo(10));
        Assert.That(userCodes, Has.Length.EqualTo(10));
    }

    // =====================================================================
    // Shared helpers (copied directly from your TestDatabase)
    // =====================================================================


    private static async ValueTask<(UserRecord Admin, UserRecord User)> Add_Users( Database db, CancellationToken token = default )
    {
        UserRecord admin = UserRecord.Create("Admin", "Admin", Permissions<TestDatabase.TestRight>.SA());
        UserRecord user  = UserRecord.Create("User",  "User",  Permissions<TestDatabase.TestRight>.Create(TestDatabase.TestRight.Read));

        admin = await db.Users.Insert(admin, token);
        user  = await db.Users.Insert(user,  token);

        return ( admin, user );
    }

    private static async ValueTask<(RoleRecord Admin, RoleRecord User)> Add_Roles( Database db, UserRecord adminUser, CancellationToken token = default )
    {
        RoleRecord admin = RoleRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(), "Admins", adminUser);

        RoleRecord user = RoleRecord.Create("User", Permissions<TestDatabase.TestRight>.Create(TestDatabase.TestRight.Read), "Users", adminUser);

        return ( await db.Roles.Insert(admin, token), await db.Roles.Insert(user, token) );
    }

    private static async ValueTask<UserRoleRecord[]> Add_Roles( Database db, UserRecord user, RoleRecord[] roles, CancellationToken token = default )
    {
        ReadOnlyMemory<UserRoleRecord> records = UserRoleRecord.Create(user, roles);

        return await db.UserRoles.Insert(records, token)
                       .ToArray(records.Length, token);
    }

    private static async ValueTask<(GroupRecord Admin, GroupRecord User)> Add_Group( Database db, UserRecord adminUser, CancellationToken token = default )
    {
        GroupRecord admin = GroupRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(), "Admin", adminUser);

        GroupRecord user = GroupRecord.Create("User", Permissions<TestDatabase.TestRight>.Create(TestDatabase.TestRight.Read), "User", adminUser);

        return ( await db.Groups.Insert(admin, token), await db.Groups.Insert(user, token) );
    }

    private static async ValueTask<UserGroupRecord[]> Add_Groups( Database db, UserRecord user, GroupRecord[] groups, CancellationToken token = default )
    {
        ReadOnlyMemory<UserGroupRecord> records = UserGroupRecord.Create(user, groups);

        return await db.UserGroups.Insert(records, token)
                       .ToArray(records.Length, token);
    }

    private static async ValueTask<(AddressRecord, UserAddressRecord)> Add_Address( Database db, UserRecord user, CancellationToken token = default )
    {
        AddressRecord address = AddressRecord.Create("address line one", "", "city", "state", "postal", "country");

        address = await db.Addresses.Insert(address, token);

        UserAddressRecord link = await db.UserAddresses.Insert(UserAddressRecord.Create(user, address), token);

        return ( address, link );
    }

    private static async ValueTask<FileRecord> Add_File( Database db, UserRecord user, CancellationToken token = default )
    {
        FileRecord file = new("file name", "file description", "file type", 0, "hash", MimeType.Unknown, "payload", "path", RecordID<FileRecord>.New(), DateTimeOffset.UtcNow);

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

    private static async ValueTask<(RecoveryCodeRecord[], UserRecoveryCodeRecord[])> Add_RecoveryCodes( Database db, UserRecord user, CancellationToken token = default )
    {
        RecoveryCodeRecord.Codes codes = RecoveryCodeRecord.Create(user, 10);

        RecoveryCodeRecord[] records = await db.RecoveryCodes.Insert(codes.Values, token)
                                               .ToArray(codes.Count, token);

        UserRecoveryCodeRecord[] links = await db.UserRecoveryCodes.Insert(UserRecoveryCodeRecord.Create(user, records), token)
                                                 .ToArray(records.Length, token);

        return ( records, links );
    }
}
