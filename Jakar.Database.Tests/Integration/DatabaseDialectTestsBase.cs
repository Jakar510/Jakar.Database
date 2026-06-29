// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using System.Collections.Immutable;
using Jakar.Extensions;



namespace Jakar.Database.Tests.Integration;


/// <summary>
///     Comprehensive <see cref="Database"/> + <see cref="DbTable{TSelf}"/> coverage for every <c>TableRecord</c>, run against a real engine.
///     <para> Concrete fixtures supply a <see cref="DbHarness"/> from an environment connection string or a Testcontainer; when none is available (or the dialect is not yet supported by the library) the whole fixture is skipped via <see cref="Assert.Ignore(string)"/>. </para>
/// </summary>
public abstract class DatabaseDialectTestsBase : Assert
{
    protected static readonly CancellationToken _ct = CancellationToken.None;
    private                   DbHarness?        __harness;
    private                   string?           __skipReason;
    protected                 Database          Db => __harness?.Db ?? throw new InvalidOperationException("Harness was not created.");


    /// <summary> Returns a ready harness, or <see langword="null"/> to skip this dialect (e.g. no connection string / Testcontainer / unsupported). </summary>
    protected abstract Task<DbHarness?> TryCreateHarnessAsync();


    [OneTimeSetUp] public async Task OneTimeSetUp()
    {
        try { __harness = await TryCreateHarnessAsync(); }
        catch ( Exception e )
        {
            __skipReason = e.Message;
            __harness    = null;
        }

        if ( __harness is null ) { Ignore(__skipReason ?? $"{GetType().Name}: no database available; skipped."); }
    }

    [OneTimeTearDown] public async Task OneTimeTearDown()
    {
        if ( __harness is not null ) { await __harness.DisposeAsync(); }
    }

    [TearDown] public abstract ValueTask DisposeContainer();

    private async Task<UserRecord> NewUser( string prefix = "user" )
    {
        UserRecord user = UserRecord.Create($"{prefix}-{Guid.NewGuid():N}", "P@ssword123!", Permissions<TestDatabase.TestRight>.SA());
        return await Db.Users.Insert(user, _ct);
    }


    [Test] public async Task Users_can_be_inserted_fetched_updated_and_deleted()
    {
        UserRecord  user    = await NewUser();
        UserRecord? fetched = await Db.Users.Get(user.ID, _ct);

        That(user.ID.IsValid(), Is.True);
        That(fetched,           Is.Not.Null);
        That(fetched!.UserName, Is.EqualTo(user.UserName));

        fetched.Email = "updated@example.com";
        await Db.Users.Update(fetched, _ct);

        UserRecord? updated = await Db.Users.Get(nameof(UserRecord.UserName), user.UserName, _ct);
        That(updated,        Is.Not.Null);
        That(updated!.Email, Is.EqualTo("updated@example.com"));

        long count = await Db.Users.Count(_ct);
        That(count, Is.GreaterThan(0));

        await Db.Users.Delete(user.ID, _ct);

        UserRecord? gone = await Db.Users.Get(nameof(UserRecord.UserName), user.UserName, _ct);
        That(gone, Is.Null);
    }

    [Test] public async Task Roles_can_be_inserted_and_fetched()
    {
        UserRecord  owner   = await NewUser("role-owner");
        RoleRecord  role    = await Db.Roles.Insert(RoleRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(), "Admins", owner), _ct);
        RoleRecord? fetched = await Db.Roles.Get(role.ID, _ct);

        That(fetched,             Is.Not.Null);
        That(fetched!.NameOfRole, Is.EqualTo("Admin"));
    }

    [Test] public async Task Groups_can_be_inserted_and_fetched()
    {
        UserRecord   owner   = await NewUser("group-owner");
        GroupRecord  group   = await Db.Groups.Insert(GroupRecord.Create("Admins", Permissions<TestDatabase.TestRight>.SA(), "Admins", owner), _ct);
        GroupRecord? fetched = await Db.Groups.Get(group.ID, _ct);

        That(fetched,              Is.Not.Null);
        That(fetched!.NameOfGroup, Is.EqualTo("Admins"));
    }

    [Test] public async Task Files_can_be_inserted_and_fetched()
    {
        FileRecord file = new(RecordID<FileRecord>.New(), DateTimeOffset.UtcNow)
                          {
                              FileName        = "name",
                              FileDescription = "description",
                              FileType        = "type",
                              FileSize        = 42,
                              Hash            = "hash",
                              MimeType        = MimeType.Unknown,
                              Payload         = "payload",
                              FullPath        = $"/tmp/{Guid.NewGuid():N}.bin"
                          };

        file = await Db.Files.Insert(file, _ct);
        FileRecord? fetched = await Db.Files.Get(file.ID, _ct);

        That(fetched,           Is.Not.Null);
        That(fetched!.FileName, Is.EqualTo("name"));
        That(fetched.FileSize,  Is.EqualTo(42));
    }

    [Test] public async Task Addresses_can_be_inserted_and_fetched()
    {
        UserRecord    user    = await NewUser("address-owner");
        AddressRecord address = AddressRecord.Create("line 1", "line 2", "city", "state", "postal", "country", Guid.NewGuid(), user.ID);
        address = await Db.Addresses.Insert(address, _ct);

        AddressRecord? fetched = await Db.Addresses.Get(address.ID, _ct);
        That(fetched,        Is.Not.Null);
        That(fetched!.City,  Is.EqualTo("city"));
        That(fetched.UserID, Is.EqualTo(user.ID));
    }

    [Test] public async Task UserLoginProviders_can_be_inserted_and_fetched()
    {
        UserRecord              user   = await NewUser("login-owner");
        UserLoginProviderRecord record = new("provider", "Provider", "key", "value", RecordID<UserLoginProviderRecord>.New(), user.ID, DateTimeOffset.UtcNow);
        record = await Db.UserLoginProviders.Insert(record, _ct);

        UserLoginProviderRecord? fetched = await Db.UserLoginProviders.Get(record.ID, _ct);
        That(fetched,         Is.Not.Null);
        That(fetched!.UserID, Is.EqualTo(user.ID));
    }

    [Test] public async Task RecoveryCodes_can_be_inserted_for_a_user()
    {
        UserRecord               user  = await NewUser("recovery-owner");
        RecoveryCodeRecord.Codes codes = RecoveryCodeRecord.Create(user, 3);

        int inserted = 0;

        foreach ( RecoveryCodeRecord code in codes.Values )
        {
            RecoveryCodeRecord saved = await Db.RecoveryCodes.Insert(code, _ct);
            That(saved.ID.IsValid(), Is.True);
            inserted++;
        }

        That(inserted, Is.EqualTo(3));
    }

    [Test] public async Task Users_can_be_assigned_roles()
    {
        UserRecord owner = await NewUser("admin");
        RoleRecord role  = await Db.Roles.Insert(RoleRecord.Create("Reader", Permissions<TestDatabase.TestRight>.Create(TestDatabase.TestRight.Read), "Readers", owner), _ct);
        UserRecord user  = await NewUser("member");

        RoleRecord[] roleArray = [role];

        await using DbConnectionContext context = await Db.ConnectAsync(_ct);
        ImmutableArray<UserRoleRecord>  links   = UserRoleRecord.Create(user, roleArray.AsSpan());
        await UserRoleRecord.TryAdd(context, links, _ct);

        int count = 0;
        await foreach ( RoleRecord assigned in user.GetRoles(context, Db, _ct) ) { count++; }

        That(links, Has.Length.EqualTo(1));
        That(count, Is.EqualTo(1));
    }

    [Test] public async Task Users_can_be_assigned_groups()
    {
        UserRecord  owner = await NewUser("group-admin");
        GroupRecord group = await Db.Groups.Insert(GroupRecord.Create("Members", Permissions<TestDatabase.TestRight>.Create(TestDatabase.TestRight.Read), "Members", owner), _ct);
        UserRecord  user  = await NewUser("group-member");

        GroupRecord[] groupArray = [group];

        await using DbConnectionContext context = await Db.ConnectAsync(_ct);
        ImmutableArray<UserGroupRecord> links   = UserGroupRecord.Create(user, groupArray.AsSpan());
        await UserGroupRecord.TryAdd(context, links, _ct);

        int count = 0;
        await foreach ( GroupRecord assigned in user.GetGroups(context, Db, _ct) ) { count++; }

        That(count, Is.EqualTo(1));
    }

    [Test] public async Task Users_can_be_linked_to_addresses()
    {
        UserRecord    user    = await NewUser("addr-link");
        AddressRecord address = AddressRecord.Create("line 1", "", "city", "state", "postal", "country", Guid.NewGuid(), user.ID);

        await using DbConnectionContext context = await Db.ConnectAsync(_ct);
        address = await Db.Addresses.Insert(context, address, _ct);
        UserAddressRecord link = UserAddressRecord.Create(user, address);
        await UserAddressRecord.TryAdd(context, link, _ct);

        That(link.KeyID, Is.EqualTo(user.ID));
    }

    [Test] public async Task GetAll_and_First_return_inserted_rows()
    {
        await NewUser("scan-a");
        await NewUser("scan-b");

        int all = 0;
        await foreach ( UserRecord record in Db.Users.All(_ct) ) { all++; }

        await using DbConnectionContext context = await Db.ConnectAsync(_ct);
        UserRecord?                     first   = await context.FirstOrDefaultAsync<UserRecord>(_ct);

        That(all,   Is.GreaterThanOrEqualTo(2));
        That(first, Is.Not.Null);
    }
}
