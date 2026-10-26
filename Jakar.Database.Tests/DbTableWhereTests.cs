// Jakar.Database :: Jakar.Database.Tests
// 10/26/2026

namespace Jakar.Database.Tests;


/// <summary> <see cref="DbTable{TSelf}.Where{TValue}(string, TValue, CancellationToken)"/> and <c>WhereID</c> compare the named column to a bound parameter. </summary>
[TestFixture]
public sealed class DbTableWhereTests : Assert
{
    [Test] public void Where_compares_the_column_to_a_bound_value()
    {
        SqlCommand command = DbTable<RoleRecord>.WhereSql(nameof(RoleRecord.NameOfRole), "Admin");

        Multiple(() =>
                 {
                     That(command.SQL,                               Is.EqualTo("SELECT * FROM roles WHERE name_of_role = @value;"));
                     That(command.Parameters!.Value.Count,           Is.EqualTo(1));
                     That(command.Parameters!.Value.Values[0].Value, Is.EqualTo("Admin"));
                 });
    }

    [Test] public void WhereID_compares_the_column_to_a_bound_value()
    {
        Guid       id      = Guid.Parse("019f1ef9-49ca-7a30-b254-eab2321f9e51");
        SqlCommand command = DbTable<RoleRecord>.WhereIDSql(nameof(RoleRecord.ID), id);

        Multiple(() =>
                 {
                     That(command.SQL,                               Is.EqualTo("SELECT id FROM roles WHERE id = @value;"));
                     That(command.Parameters!.Value.Count,           Is.EqualTo(1));
                     That(command.Parameters!.Value.Values[0].Value, Is.EqualTo(id));
                 });
    }

    [Test] public void Where_binds_a_null_value()
    {
        SqlCommand command = DbTable<RoleRecord>.WhereSql<string>(nameof(RoleRecord.NameOfRole), null);

        Multiple(() =>
                 {
                     That(command.SQL,                               Is.EqualTo("SELECT * FROM roles WHERE name_of_role = @value;"));
                     That(command.Parameters!.Value.Values[0].Value, Is.Null);
                 });
    }
}
