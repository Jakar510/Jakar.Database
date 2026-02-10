// Jakar.Database :: Jakar.Database.Tests
// 01/26/2026  09:59

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Jakar.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;



namespace Jakar.Database.Tests;


[TestFixture]
[NonParallelizable]
[SuppressMessage("ReSharper", "UnusedVariable")] // DB + migrations → avoid parallel runs
public sealed class DatabaseTests : Assert
{
    private WebApplication       __app   = null!;
    private IServiceScope        __scope = null!;
    private TestDatabase         __db    = null!;
    private PostgreSqlContainer? __postgreSqlContainer;
    [OneTimeSetUp] public async Task OneTimeSetup()
    {
        const string      USER             = "dev";
        const string      PASSWORD         = "dev";
        PostgreSqlBuilder containerBuilder = new("postgres:18.1");
        containerBuilder.WithUsername(USER);
        containerBuilder.WithPassword(PASSWORD);
        containerBuilder.WithDatabase(TestDatabase.AppName);

        __postgreSqlContainer = containerBuilder.Build();
        await __postgreSqlContainer.StartAsync();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        SecuredStringResolverOptions connectionString = $"User ID={USER};Password={PASSWORD};Host={__postgreSqlContainer.IpAddress};Port={__postgreSqlContainer.GetMappedPublicPort()};Database={TestDatabase.AppName}";

        DbOptions options = new()
                            {
                                ConnectionStringResolver = connectionString,
                                CommandTimeout           = 30,
                                TokenIssuer              = TestDatabase.AppName,
                                TokenAudience            = TestDatabase.AppName
                            };

        builder.AddDatabase<TestDatabase>(options);

        __app = builder.Build();
        await __app.ApplyMigrations();

        __scope = __app.Services.CreateScope();
        __db    = __scope.ServiceProvider.GetRequiredService<TestDatabase>();
    }

    [OneTimeTearDown] public async Task OneTimeTearDown()
    {
        __scope.Dispose();
        await __app.DisposeAsync();
        await __db.DisposeAsync();

        if ( __postgreSqlContainer is not null )
        {
            await __postgreSqlContainer.StopAsync();
            await __postgreSqlContainer.DisposeAsync();
        }
    }


    // ------------------------------------------------------------
    // USERS
    // ------------------------------------------------------------

    [Test] public async Task Can_Create_Admin_And_User()
    {
        ( UserRecord admin, UserRecord user ) = await Add_Users(__db);

        Multiple(() =>
                 {
                     That(admin.ID.Value, Is.Not.EqualTo(Guid.Empty));
                     That(admin.ID.Value, Is.Not.EqualTo(Guid.AllBitsSet));

                     That(user.ID.Value, Is.Not.EqualTo(Guid.Empty));
                     That(user.ID.Value, Is.Not.EqualTo(Guid.AllBitsSet));

                     That(admin.Rights,
                          Is.EqualTo(Permissions<TestDatabase.TestRight>.SA()
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
        ImmutableArray<UserRoleRecord> roles = await Add_Roles(__db, user, [adminRole, userRole]);

        That(roles, Has.Length.EqualTo(2));
    }

    // ------------------------------------------------------------
    // GROUPS
    // ------------------------------------------------------------

    [Test] public async Task Can_Create_Groups_And_Assign_To_User()
    {
        ( UserRecord admin, UserRecord user )             = await Add_Users(__db);
        ( GroupRecord adminGroup, GroupRecord userGroup ) = await Add_Group(__db, admin);
        ImmutableArray<UserGroupRecord> groups = await Add_Groups(__db, user, [adminGroup, userGroup]);

        That(groups, Has.Length.EqualTo(2));
    }

    // ------------------------------------------------------------
    // ADDRESS
    // ------------------------------------------------------------

    [Test] public async Task Can_Add_Address_To_User()
    {
        ( UserRecord admin, UserRecord user )             = await Add_Users(__db);
        ( AddressRecord address, UserAddressRecord link ) = await Add_Address(__db, user);

        That(link.KeyID, Is.EqualTo(user.ID));
    }

    // ------------------------------------------------------------
    // FILE
    // ------------------------------------------------------------

    [Test] public async Task Can_Assign_File_As_User_Image()
    {
        ( UserRecord admin, UserRecord user ) = await Add_Users(__db);
        FileRecord file = await Add_File(__db, user);

        That(user.ImageID, Is.EqualTo(file));
    }

    // ------------------------------------------------------------
    // LOGIN PROVIDER
    // ------------------------------------------------------------

    [Test] public async Task Can_Add_Login_Provider()
    {
        ( UserRecord admin, UserRecord user ) = await Add_Users(__db);
        UserLoginProviderRecord record = await Add_UserLoginProvider(__db, user);

        That(record.ID, Is.EqualTo(user.ID));
    }

    // ------------------------------------------------------------
    // RECOVERY CODES
    // ------------------------------------------------------------

    [Test] public async Task Can_Create_Recovery_Codes()
    {
        ( UserRecord admin, UserRecord user )                                                          = await Add_Users(__db);
        ( ImmutableArray<RecoveryCodeRecord> codes, ImmutableArray<UserRecoveryCodeRecord> userCodes ) = await Add_RecoveryCodes(__db, user);

        That(codes,     Has.Length.EqualTo(10));
        That(userCodes, Has.Length.EqualTo(10));
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
        RoleRecord admin = RoleRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(),                                "Admins", adminUser);
        RoleRecord user  = RoleRecord.Create("User",  Permissions<TestDatabase.TestRight>.Create(TestDatabase.TestRight.Read), "Users",  adminUser);

        return ( await db.Roles.Insert(admin, token), await db.Roles.Insert(user, token) );
    }

    private static async ValueTask<ImmutableArray<UserRoleRecord>> Add_Roles( Database db, UserRecord user, RoleRecord[] roles, CancellationToken token = default )
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
        GroupRecord admin = GroupRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(),                                "Admin", adminUser);
        GroupRecord user  = GroupRecord.Create("User",  Permissions<TestDatabase.TestRight>.Create(TestDatabase.TestRight.Read), "User",  adminUser);

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
}
