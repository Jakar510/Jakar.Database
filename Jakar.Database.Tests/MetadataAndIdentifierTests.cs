using System.Data;
using Jakar.Extensions;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class MetadataAndIdentifierTests
{
    [Test]
    public void RecordID_TryParse_round_trips_guid_values()
    {
        Guid                value  = Guid.CreateVersion7();
        RecordID<UserRecord> id    = RecordID<UserRecord>.Create(value);
        bool                 parsed = RecordID<UserRecord>.TryParse(id.ToString(), out RecordID<UserRecord> result);

        Assert.Multiple(() =>
                        {
                            Assert.That(parsed, Is.True);
                            Assert.That(result, Is.EqualTo(id));
                            Assert.That(RecordID<UserRecord>.Parse(id.ToString()), Is.EqualTo(id));
                        });
    }

    [Test]
    public void RecordID_TryParse_rejects_invalid_and_empty_guid_values()
    {
        bool parsed = RecordID<UserRecord>.TryParse("not-a-guid", out RecordID<UserRecord> result);
        RecordID<UserRecord>? empty = RecordID<UserRecord>.TryCreate(Guid.Empty);

        Assert.Multiple(() =>
                        {
                            Assert.That(parsed, Is.False);
                            Assert.That(result, Is.EqualTo(RecordID<UserRecord>.Empty));
                            Assert.That(empty,  Is.Null);
                        });
    }

    [Test]
    public void AutoRecordID_TryParse_round_trips_long_values()
    {
        AutoRecordID<ResxRowRecord> id = AutoRecordID<ResxRowRecord>.Create(42);
        bool                        parsed = AutoRecordID<ResxRowRecord>.TryParse("42", out AutoRecordID<ResxRowRecord> result);

        Assert.Multiple(() =>
                        {
                            Assert.That(parsed, Is.True);
                            Assert.That(result, Is.EqualTo(id));
                            Assert.That(AutoRecordID<ResxRowRecord>.Parse("42"), Is.EqualTo(id));
                        });
    }

    [Test]
    public void Identifier_dynamic_parameters_store_scalar_identifier_values()
    {
        Guid                     userId = Guid.CreateVersion7();
        CommandParameters        recordParameters = RecordID<UserRecord>.Create(userId).ToDynamicParameters();
        CommandParameters        autoParameters   = AutoRecordID<ResxRowRecord>.Create(42).ToDynamicParameters();

        Assert.Multiple(() =>
                        {
                            Assert.That(recordParameters.Count,       Is.EqualTo(1));
                            Assert.That(recordParameters.Values[0].Value, Is.EqualTo(userId));
                            Assert.That(autoParameters.Count,         Is.EqualTo(1));
                            Assert.That(autoParameters.Values[0].Value, Is.EqualTo(42L));
                        });
    }

    [Test]
    public void ColumnMetaData_property_accessor_reads_declared_and_inherited_properties()
    {
        UserRecord owner = UserRecord.Create("Owner", "Owner", Permissions<TestDatabase.TestRight>.SA());
        AddressRecord address = AddressRecord.Create("line 1", "line 2", "city", "state", "postal", "country", Guid.NewGuid(), owner.ID);

        Func<AddressRecord, object?> line1Accessor = ColumnMetaData.GetTablePropertyValueAccessor<AddressRecord>(nameof(AddressRecord.Line1));
        Func<AddressRecord, object?> userAccessor  = ColumnMetaData.GetTablePropertyValueAccessor<AddressRecord>(nameof(AddressRecord.UserID));
        Func<AddressRecord, object?> dateAccessor  = ColumnMetaData.GetTablePropertyValueAccessor<AddressRecord>(nameof(AddressRecord.DateCreated));

        Assert.Multiple(() =>
                        {
                            Assert.That(line1Accessor(address), Is.EqualTo(address.Line1));
                            Assert.That(userAccessor(address),  Is.EqualTo(address.UserID));
                            Assert.That(dateAccessor(address),  Is.EqualTo(address.DateCreated));
                        });
    }

    [Test]
    public void ColumnMetaData_data_column_reflects_nullability_and_fixed_metadata()
    {
        ColumnMetaData line1Column   = AddressRecord.MetaData[nameof(AddressRecord.Line1)];
        ColumnMetaData addressColumn = AddressRecord.MetaData[nameof(AddressRecord.Address)];
        DataColumn     line1         = line1Column.DataColumn;
        DataColumn     address       = addressColumn.DataColumn;

        Assert.Multiple(() =>
                        {
                            Assert.That(line1.AllowDBNull,  Is.False);
                            Assert.That(line1Column.IsFixed,        Is.True);
                            Assert.That(address.AllowDBNull, Is.True);
                            Assert.That(addressColumn.IsFixed,      Is.True);
                        });
    }

    [Test]
    public void ColumnMetaData_index_sql_is_provider_specific()
    {
        ColumnMetaData column = AddressRecord.MetaData[nameof(AddressRecord.Line1)];

        string postgresCreate = column.CreateIndex(AddressRecord.MetaData, DatabaseType.PostgreSQL).SQL;
        string sqlServerCreate = column.CreateIndex(AddressRecord.MetaData, DatabaseType.MicrosoftSqlServer).SQL;
        string postgresDrop = column.DropIndex(AddressRecord.MetaData, DatabaseType.PostgreSQL).SQL;
        string sqlServerDrop = column.DropIndex(AddressRecord.MetaData, DatabaseType.MicrosoftSqlServer).SQL;

        Assert.Multiple(() =>
                        {
                            Assert.That(postgresCreate, Does.Contain($"CREATE INDEX IF NOT EXISTS {column.IndexName} ON {AddressRecord.TableName}({column.ColumnName});"));
                            Assert.That(sqlServerCreate, Does.Contain($"CREATE INDEX {column.IndexName} ON {AddressRecord.TableName}({column.ColumnName});"));
                            Assert.That(sqlServerCreate, Does.Contain("sys.indexes"));
                            Assert.That(postgresDrop, Does.Contain($"DROP INDEX IF EXISTS {column.IndexName};"));
                            Assert.That(sqlServerDrop, Does.Contain($"DROP INDEX {column.IndexName} ON {AddressRecord.TableName};"));
                        });
    }
}
