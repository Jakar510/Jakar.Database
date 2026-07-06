// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using System.Data;
using System.Text.RegularExpressions;
using Jakar.Extensions;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class SqlCommandTests : Assert
{
    private static RoleRecord CreateRole() => RoleRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(), "ADMINS");
    private static RoleRecord CreateRole( string name ) => RoleRecord.Create(name, Permissions<TestDatabase.TestRight>.SA(), name.ToUpperInvariant());


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

    [Test] public void Address_created_without_user_is_unowned_with_generated_id()
    {
        AddressRecord address = AddressRecord.Create("line1", "line2", "city", "state", "postal", "country");

        Multiple(() =>
                 {
                     // Shared/unowned: no user is inlined as an empty Guid FK; it is left NULL.
                     That(address.UserID, Is.Null);
                     // A missing id must be generated, not left as an empty Guid.
                     That(address.ID.Value, Is.Not.EqualTo(Guid.Empty));
                 });
    }

    [Test] public void GetUpdate_inlines_id_as_literal_not_operator()
    {
        RoleRecord role = CreateRole();

        string sql = SqlCommand.GetUpdate(role).SQL;

        Multiple(() =>
                 {
                     That(sql, Does.Contain("UPDATE").And.Contain("roles"));
                     // "@'guid'" reads @ as the Postgres abs() operator applied to a string, throwing "invalid input syntax for type double precision".
                     That(sql, Does.Not.Contain("@'"), "the id must be inlined as a literal, not prefixed with @");
                     That(sql, Does.Contain($"id = '{role.ID.Value}'"));
                 });
    }

    [Test] public void GetInsert_multi_row_binds_every_placeholder()
    {
        RoleRecord[] roles = [CreateRole("Admin"), CreateRole("Editor"), CreateRole("Viewer")];

        SqlCommand command = SqlCommand.GetInsert<RoleRecord>(roles);

        That(command.Parameters.HasValue, Is.True);

        // Collect every bound parameter name (stored without the leading '@').
        HashSet<string> bound = [];
        using ( ArrayBuffer<SqlParameter> buffer = command.Parameters!.Value.Parameters )
        {
            foreach ( ref readonly SqlParameter parameter in buffer.Span ) { bound.Add(parameter.ParameterName); }
        }

        // Extract every @placeholder actually written into the SQL text.
        MatchCollection placeholders = Regex.Matches(command.SQL, "@(?<name>[A-Za-z_][A-Za-z0-9_]*)");
        HashSet<string> referenced   = [.. placeholders.Select(static m => m.Groups["name"].Value)];

        Multiple(() =>
                 {
                     That(command.SQL, Does.Contain("INSERT INTO").And.Contain("roles"));
                     // Multi-row inserts are materialized back into full records via TSelf.Create(reader), so the whole row must be returned, not just the id.
                     That(command.SQL, Does.Contain("RETURNING *"));
                     // Multi-row insert must actually emit per-row @placeholders (regression guard: they used to be emitted with no bound parameter).
                     That(referenced, Is.Not.Empty, "expected the multi-row insert to emit @parameter placeholders");

                     foreach ( string name in referenced ) { That(bound, Does.Contain(name), $"placeholder @{name} has no bound parameter"); }
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
