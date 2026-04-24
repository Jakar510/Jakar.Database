using Jakar.Extensions;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class GeneratedRecordTests : Assert
{
    [Test] public void Generated_ToDynamicParameters_Covers_All_Record_Columns()
    {
        UserRecord owner = UserRecord.Create("Owner", "Owner", Permissions<TestDatabase.TestRight>.SA());
        AddressRecord address = AddressRecord.Create("line 1", "line 2", "city", "state", "postal", "country", Guid.NewGuid(), owner.ID);
        RoleRecord role = RoleRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(), "ADMINS", owner.ID);
        FileRecord file = new(RecordID<FileRecord>.New(), DateTimeOffset.UtcNow)
                          {
                              FileName = "name",
                              FileDescription = "description",
                              FileType = "type",
                              FileSize = 42,
                              Hash = "hash",
                              MimeType = MimeType.Unknown,
                              Payload = "payload",
                              FullPath = "/tmp/file.bin"
                          };
        UserLoginProviderRecord login = new("provider", "Provider", "key", "value", RecordID<UserLoginProviderRecord>.New(), owner.ID, DateTimeOffset.UtcNow);

        Multiple(() =>
                 {
                     That(address.ToDynamicParameters().Count, Is.EqualTo(AddressRecord.PropertyCount));
                     That(file.ToDynamicParameters().Count, Is.EqualTo(FileRecord.PropertyCount));
                     That(role.ToDynamicParameters().Count, Is.EqualTo(RoleRecord.PropertyCount));
                     That(owner.ToDynamicParameters().Count, Is.EqualTo(UserRecord.PropertyCount));
                     That(login.ToDynamicParameters().Count, Is.EqualTo(UserLoginProviderRecord.PropertyCount));
                 });
    }

    [Test] public void Core_Migration_Factories_Define_DownSql()
    {
        Multiple(() =>
                 {
                     That(MigrationRecord.SetLastModified(0).CanRollback, Is.True);
                     That(MigrationRecord.FromEnum<MimeType>(1).CanRollback, Is.True);
                     That(MigrationRecord.Create<AddressRecord>(10, "create addresses", AddressRecord.MetaData.CreateTableSql(DatabaseType.PostgreSQL), "DROP TABLE IF EXISTS addresses CASCADE;").CanRollback, Is.True);
                     That(MigrationRecord.Create<RoleRecord>(11, "create roles", RoleRecord.MetaData.CreateTableSql(DatabaseType.PostgreSQL), "DROP TABLE IF EXISTS roles CASCADE;").CanRollback, Is.True);
                 });
    }
}
