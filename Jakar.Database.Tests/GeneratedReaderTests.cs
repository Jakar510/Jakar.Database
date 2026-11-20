using System.Data;
using System.Data.Common;
using System.Reflection;
using Jakar.Extensions;
using Jakar.Json;
using ZLinq;



namespace Jakar.Database.Tests;


/// <summary> Reads records back through the generated DbDataReader constructors and Create(DbDataReader), from a row laid out the way the table stores it. </summary>
[TestFixture]
public sealed class GeneratedReaderTests : Assert
{
    private static readonly UserRecord __owner = UserRecord.Create("Owner", "Owner", Permissions<TestDatabase.TestRight>.SA());


    [Test] public void Owned_Record_Round_Trips()
    {
        RoleRecord role = RoleRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(), "ADMINS", __owner.ID);

        AssertRoundTrip(role);
    }

    [Test] public void Pair_Record_With_Enums_Ids_And_Nullable_Columns_Round_Trips()
    {
        UserRecord user = UserRecord.Create("Reader", "Reader", Permissions<TestDatabase.TestRight>.SA());
        user.FirstName         = "First";
        user.PreferredLanguage = SupportedLanguage.English;
        user.EscalateTo        = __owner.ID;
        user.SubscriptionID    = Guid.NewGuid();
        user.LockoutEnd        = DateTimeOffset.UtcNow;
        user.BadLogins         = 3;
        user.IsActive          = true;

        UserRecord copy = AssertRoundTrip(user);

        Multiple(() =>
                 {
                     That(copy.PreferredLanguage, Is.EqualTo(SupportedLanguage.English));
                     That(copy.EscalateTo,        Is.EqualTo(__owner.ID));
                     That(copy.ImageID,           Is.Null);
                     That(copy.Rights.Value,      Is.EqualTo(user.Rights.Value));
                 });
    }

    [Test] public void Nullable_Enum_Reads_Null_And_Values()
    {
        FileRecord file = new(RecordID<FileRecord>.New(), DateTimeOffset.UtcNow)
                          {
                              FileName = "name",
                              FileSize = 42,
                              Hash     = "hash",
                              Payload  = "payload"
                          };

        That(AssertRoundTrip(file).MimeType, Is.Null);

        file.MimeType = MimeType.Unknown;
        That(AssertRoundTrip(file).MimeType, Is.EqualTo(MimeType.Unknown));
    }

    [Test] public void Recovery_Code_Is_Not_Hashed_Again_When_Read()
    {
        RecoveryCodeRecord code = new("code", __owner);

        That(AssertRoundTrip(code).Code, Is.EqualTo(code.Code));
    }

    [Test] public void Table_Record_With_Required_Members_Round_Trips()
    {
        ResxRowRecord row = new("key", "neutral")
                            {
                                KeyID   = AutoRecordID<ResxRowRecord>.Create(5),
                                English = "english",
                                Thai    = "thai"
                            };

        That(AssertRoundTrip(row).KeyID, Is.EqualTo(row.KeyID));
    }

    [Test] public void Address_With_Nullable_Owner_Round_Trips()
    {
        AddressRecord address = AddressRecord.Create("line 1",
                                                     "line 2",
                                                     "city",
                                                     "state",
                                                     "postal",
                                                     "country",
                                                     Guid.NewGuid(),
                                                     __owner.ID);

        Multiple(() =>
                 {
                     AssertRoundTrip(address);
                     AssertRoundTrip(address with { UserID = null });
                 });
    }


    private static TSelf AssertRoundTrip<TSelf>( TSelf record )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        using DbDataReader reader = Row(record);
        TSelf              copy   = TSelf.Create(reader);

        That(copy, Is.EqualTo(record), typeof(TSelf).Name);
        return copy;
    }

    // One row with a column per mapped property, at the ordinal the metadata gives it, holding the value the database would store.
    private static DbDataReader Row<TSelf>( TSelf record )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        DataTable    table  = new();
        List<object?> values = [];

        foreach ( ColumnMetaData column in TSelf.MetaData.SortedColumns )
        {
            That(column.Index, Is.EqualTo(table.Columns.Count), column.PropertyName);

            table.Columns.Add(column.ColumnName, typeof(object));
            values.Add(ToStorage(typeof(TSelf).GetProperty(column.PropertyName, BindingFlags.Public | BindingFlags.Instance)!.GetValue(record)));
        }

        table.Rows.Add([.. values]);
        DataTableReader reader = table.CreateDataReader();
        reader.Read();
        return reader;
    }

    private static object? ToStorage( object? value ) => value switch
                                                         {
                                                             null                       => DBNull.Value,
                                                             UserRights rights          => rights.Value,
                                                             ISqlParameterValue wrapper => wrapper.ParameterValue,
                                                             Enum member                => member.ToString(),
                                                             JObjectNode json           => json.ToJson(),
                                                             _                          => value
                                                         };
}
