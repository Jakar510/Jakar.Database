// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using System.Data;
using Jakar.Extensions;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class SqlCommandTests : Assert
{
    private static RoleRecord CreateRole() => RoleRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(), "ADMINS");


    [Test] public void GetAll_selects_every_row()
        => That(SqlCommand.GetAll<RoleRecord>().SQL, Does.Contain("SELECT * FROM").And.Contain("roles"));

    [Test] public void GetCount_projects_count_of_primary_key()
    {
        string sql = SqlCommand.GetCount<RoleRecord>().SQL;

        Multiple(() =>
                 {
                     That(sql, Does.Contain("COUNT(id)"));
                     That(sql, Does.Contain("roles"));
                 });
    }

    [Test] public void Get_by_single_id_inlines_the_identifier()
    {
        RecordID<RoleRecord> id  = RecordID<RoleRecord>.New();
        string               sql = SqlCommand.Get(id).SQL;

        Multiple(() =>
                 {
                     That(sql, Does.Contain("SELECT * FROM").And.Contain("roles"));
                     That(sql, Does.Contain("WHERE"));
                     That(sql, Does.Contain(id.Value.ToString()));
                 });
    }

    [Test] public void Get_by_many_ids_emits_in_clause()
    {
        string sql = SqlCommand.Get(RecordID<RoleRecord>.New(), RecordID<RoleRecord>.New()).SQL;

        Multiple(() =>
                 {
                     That(sql, Does.Contain("roles"));
                     That(sql, Does.Contain("in ("));
                 });
    }

    [Test] public void GetDelete_and_GetDeleteAll_emit_delete_statements()
    {
        Multiple(() =>
                 {
                     That(SqlCommand.GetDelete(RecordID<RoleRecord>.New()).SQL, Does.Contain("DELETE FROM").And.Contain("roles"));
                     That(SqlCommand.GetDeleteAll<RoleRecord>().SQL,            Does.Contain("DELETE FROM").And.Contain("roles"));
                 });
    }

    [Test] public void GetInsert_emits_insert_and_binds_parameters()
    {
        SqlCommand command = SqlCommand.GetInsert(CreateRole());

        Multiple(() =>
                 {
                     That(command.SQL, Does.Contain("INSERT INTO").And.Contain("roles"));
                     That(command.SQL, Does.Contain("VALUES"));
                     That(command.SQL, Does.Contain("RETURNING id"));
                     That(command.Parameters.HasValue, Is.True);
                     That(command.Parameters!.Value.ParameterCount, Is.GreaterThan(0));
                 });
    }

    [Test] public void WherePaged_overloads_emit_offset_and_limit()
    {
        Multiple(() =>
                 {
                     That(SqlCommand.WherePaged<RoleRecord>(0, 10).SQL,                            Does.Contain("OFFSET 0").And.Contain("LIMIT 10"));
                     That(SqlCommand.WherePaged<RoleRecord>(RecordID<UserRecord>.New(), 0, 5).SQL, Does.Contain("user_id"));
                 });
    }

    [Test] public void GetFirst_and_GetLast_order_by_date_created()
    {
        Multiple(() =>
                 {
                     That(SqlCommand.GetFirst<RoleRecord>().SQL, Does.Contain("ORDER BY").And.Contain("ASC"));
                     That(SqlCommand.GetLast<RoleRecord>().SQL,  Does.Contain("ORDER BY").And.Contain("DESC"));
                 });
    }

    [Test] public void GetRandom_orders_randomly()
        => That(SqlCommand.GetRandom<RoleRecord>().SQL, Does.Contain("RANDOM()").And.Contain("roles"));

    [Test] public void Create_typed_attaches_empty_parameter_set()
    {
        SqlCommand command = SqlCommand.Create<RoleRecord>("SELECT 1");

        Multiple(() =>
                 {
                     That(command.SQL,                Is.EqualTo("SELECT 1"));
                     That(command.Parameters.HasValue, Is.True);
                     That(command.Parameters!.Value.Count, Is.EqualTo(0));
                 });
    }

    [Test] public void Implicit_conversions_round_trip_sql_and_parameters()
    {
        SqlCommand command = "SELECT 1";
        string     sql     = command;
        CommandParameters? parameters = command;

        Multiple(() =>
                 {
                     That(sql,                Is.EqualTo("SELECT 1"));
                     That(parameters,         Is.Null);
                     That(command.CommandType, Is.EqualTo(CommandType.Text));
                 });
    }

    [Test] public void Equality_compares_sql_parameters_and_command_type()
    {
        SqlCommand left      = "SELECT 1";
        SqlCommand sameSql   = "select 1"; // SQL comparison is case-insensitive
        SqlCommand differing = "SELECT 2";

        Multiple(() =>
                 {
                     That(left, Is.EqualTo(sameSql));
                     That(left, Is.Not.EqualTo(differing));
                     That(left == sameSql,            Is.True);
                     That(left != differing,          Is.True);
                     That(left.GetHashCode(), Is.EqualTo(sameSql.GetHashCode()));
                 });
    }
}
