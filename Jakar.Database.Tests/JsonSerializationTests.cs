using System.Security.Claims;
using System.Text;
using Jakar.Extensions;
using Jakar.Json;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class JsonSerializationTests : Assert
{
    [Test] public void UserRecord_RoundTrips_With_AdditionalData()
    {
        UserRecord user = UserRecord.Create("tester", "password", Permissions<TestDatabase.TestRight>.SA());
        user.AdditionalData = new JObjectNode { ["theme"] = "dark", ["count"] = 3 };

        string     json = user.ToJson();
        UserRecord copy = UserRecord.FromJson(json);

        Multiple(() =>
                 {
                     That(copy.ID,                  Is.EqualTo(user.ID));
                     That(copy.UserName,            Is.EqualTo(user.UserName));
                     That(copy.PasswordHash,        Is.EqualTo(user.PasswordHash));
                     That(copy.Rights.Value,        Is.EqualTo(user.Rights.Value));
                     That(copy.DateCreated,         Is.EqualTo(user.DateCreated));
                     That(copy.AdditionalData,      Is.Not.Null);
                     That((string?)copy.AdditionalData?["theme"], Is.EqualTo("dark"));
                     That((int?)copy.AdditionalData?["count"],    Is.EqualTo(3));
                 });
    }

    [Test] public void Unknown_Members_Are_Captured_In_AdditionalData()
    {
        UserRecord  user = UserRecord.Create("tester", "password", Permissions<TestDatabase.TestRight>.SA());
        JObjectNode node = JNode.Parse(user.ToJson())!.AsObject();
        node["NotAColumn"] = "kept";

        UserRecord copy = UserRecord.FromJson(node.ToJson());

        That((string?)copy.AdditionalData?["NotAColumn"], Is.EqualTo("kept"));
    }

    [Test] public void RecoveryCode_RoundTrip_Keeps_The_Hash()
    {
        UserRecord owner = UserRecord.Create("owner", "password", Permissions<TestDatabase.TestRight>.SA());
        ( string code, RecoveryCodeRecord record ) = RecoveryCodeRecord.Create(owner, "ABC-123");

        RecoveryCodeRecord copy = RecoveryCodeRecord.FromJson(record.ToJson());

        Multiple(() =>
                 {
                     That(copy.Code,                              Is.EqualTo(record.Code));
                     That(RecoveryCodeRecord.IsValid(code, copy), Is.True);
                 });
    }

    [Test] public void Mapping_RoundTrips()
    {
        UserRoleRecord mapping = UserRoleRecord.Create(RecordID<UserRecord>.New(), RecordID<RoleRecord>.New());
        UserRoleRecord copy    = UserRoleRecord.FromJson(mapping.ToJson());

        Multiple(() =>
                 {
                     That(copy.KeyID,       Is.EqualTo(mapping.KeyID));
                     That(copy.ValueID,     Is.EqualTo(mapping.ValueID));
                     That(copy.DateCreated, Is.EqualTo(mapping.DateCreated));
                 });
    }

    [Test] public void JNode_Parameters_Are_Bound_As_Json_Text()
    {
        JObjectNode  data      = new() { ["a"] = 1 };
        CommandParameter parameter = UserRecord.MetaData[nameof(UserRecord.AdditionalData)].ToParameter(data);

        That(parameter.Value, Is.InstanceOf<string>());
        That(JNode.DeepEquals(JNode.Parse((string)parameter.Value!), data), Is.True);
    }

    [Test] public void JsonAdditionalData_Parses_Only_Objects()
    {
        Multiple(() =>
                 {
                     That(JsonAdditionalData.Parse("""{ "a" : 1 }""")?.Count, Is.EqualTo(1));
                     That(JsonAdditionalData.Parse("[1, 2]"),                 Is.Null);
                     That(JsonAdditionalData.Parse("not json"),               Is.Null);
                     That(JsonAdditionalData.Parse(null),                     Is.Null);
                 });
    }

    [Test] public void JwtParser_Reads_Claims_And_Role_Arrays()
    {
        string payload = Convert.ToBase64String(Encoding.Default.GetBytes($$"""{ "sub" : "abc", "n" : 5, "{{ClaimTypes.Role}}" : ["admin", "user"] }""")).TrimEnd('=');

        List<Claim> claims = JwtParser.ParseClaimsFromJwt($"header.{payload}.signature");

        Multiple(() =>
                 {
                     That(claims.Where(static x => x.Type == ClaimTypes.Role).Select(static x => x.Value), Is.EquivalentTo(new[] { "admin", "user" }));
                     That(claims.Single(static x => x.Type == "sub").Value,                                 Is.EqualTo("abc"));
                     That(claims.Single(static x => x.Type == "n").Value,                                   Is.EqualTo("5"));
                 });
    }
}
