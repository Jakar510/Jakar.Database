// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using System.Data;
using Jakar.Extensions;



namespace Jakar.Database.Tests;


[TestFixture]
public sealed class SqlInterpolatedStringHandlerTests : Assert
{
#region SqlName / SqlRaw value types

    [Test] public void SqlName_normalizes_to_snake_case()
    {
        SqlName name           = SqlName.Create("NameOfRole");
        SqlName implicitFrom   = "FirstName";
        string  implicitToText = name;

        Multiple(() =>
                 {
                     That(name.Value,            Is.EqualTo("name_of_role"));
                     That(name.IsValid,          Is.True);
                     That(name.Length,           Is.EqualTo("name_of_role".Length));
                     That(name.ToString(),       Is.EqualTo("name_of_role"));
                     That(implicitFrom.Value,    Is.EqualTo("first_name"));
                     That(implicitToText,        Is.EqualTo("name_of_role"));
                     That(SqlName.Empty.IsValid, Is.False);
                 });
    }

    [Test] public void SqlRaw_preserves_value_verbatim()
    {
        SqlRaw raw = SqlRaw.Create("SELECT 1");

        Multiple(() =>
                 {
                     That(raw.Value,                 Is.EqualTo("SELECT 1"));
                     That(raw.IsValid,               Is.True);
                     That(raw.Length,                Is.EqualTo("SELECT 1".Length));
                     That(raw.ToString(),            Is.EqualTo("SELECT 1"));
                     That(new SqlRaw("   ").IsValid, Is.False);
                 });
    }

#endregion



#region Static helpers

    [Test] public void SanitizeParameterName_keeps_last_segment_and_replaces_invalid_characters()
    {
        Multiple(() =>
                 {
                     That(SqlInterpolatedStringHandler<RoleRecord>.SanitizeParameterName("schema.table.column"), Is.EqualTo("column"));
                     That(SqlInterpolatedStringHandler<RoleRecord>.SanitizeParameterName(""),                    Is.EqualTo("p"));
                     That(SqlInterpolatedStringHandler<RoleRecord>.SanitizeParameterName("   "),                 Is.EqualTo("p"));
                     That(SqlInterpolatedStringHandler<RoleRecord>.SanitizeParameterName("a b!c"),               Is.EqualTo("a_b_c"));
                 });
    }

    [Test] public void GetInsert_numbers_parameters_per_row_for_multiple_records()
    {
        RoleRecord first  = new("Admin");
        RoleRecord second = new("User");
        string     sql    = SqlCommand.GetInsert(first, second).SQL;

        Multiple(() =>
                 {
                     That(sql, Does.Contain("@normalized_name1"));
                     That(sql, Does.Contain("@normalized_name2"));
                     That(sql, Does.Contain("@name_of_role1"));
                     That(sql, Does.Contain("@name_of_role2"));
                     That(sql, Does.Not.Contain("@normalized_name,")); // the un-suffixed name must not appear
                 });
    }

    [Test] public void NeedsQuotes_is_true_for_string_like_types_only()
    {
        Multiple(() =>
                 {
                     That(SqlInterpolatedStringHandler<RoleRecord>.NeedsQuotes<string>(),         Is.True);
                     That(SqlInterpolatedStringHandler<RoleRecord>.NeedsQuotes<Guid>(),           Is.True);
                     That(SqlInterpolatedStringHandler<RoleRecord>.NeedsQuotes<DateTimeOffset>(), Is.True);
                     That(SqlInterpolatedStringHandler<RoleRecord>.NeedsQuotes<int>(),            Is.False);
                     That(SqlInterpolatedStringHandler<RoleRecord>.NeedsQuotes<bool>(),           Is.False);
                 });
    }

    [Test] public void Clean_strips_trailing_whitespace_and_collapses_blank_lines()
    {
        string cleaned = SqlInterpolatedStringHandler<RoleRecord>.Clean("SELECT * FROM roles   \nWHERE \n\n\n    id = '019f';   \n\n");

        Multiple(() =>
                 {
                     That(cleaned,              Is.EqualTo("SELECT * FROM roles\nWHERE\n\n    id = '019f';"));
                     That(cleaned.EndsWith('\n'), Is.False);
                     That(cleaned,              Does.Not.Contain(" \n"));
                     That(cleaned,              Does.Not.Contain("\n\n\n"));
                 });
    }

    [Test] public void AppendLiteral_inlines_non_injectable_values()
    {
        Guid                       guid        = Guid.Parse("019f1ef9-49ca-7a30-b254-eab2321f9e51");
        System.Text.StringBuilder  guidBuilder = new();
        System.Text.StringBuilder  intBuilder  = new();
        System.Text.StringBuilder  nullBuilder = new();

        SqlParameter.AppendLiteral(guidBuilder, guid);
        SqlParameter.AppendLiteral(intBuilder,  42);
        SqlParameter.AppendLiteral(nullBuilder, null);

        Multiple(() =>
                 {
                     That(guidBuilder.ToString(), Is.EqualTo($"'{guid}'"));
                     That(intBuilder.ToString(),  Is.EqualTo("42"));
                     That(nullBuilder.ToString(), Is.EqualTo("NULL"));
                 });
    }

    [Test] public void ParseFormat_extracts_indent_level()
    {
        string? indentOnly = "2";
        SqlInterpolatedStringHandler<RoleRecord>.ParseFormat(ref indentOnly, out ushort indentLevel);

        string? nonNumeric = "noindent";
        SqlInterpolatedStringHandler<RoleRecord>.ParseFormat(ref nonNumeric, out ushort fallbackLevel);

        Multiple(() =>
                 {
                     That(indentLevel,   Is.EqualTo((ushort)2));
                     That(indentOnly,    Is.Null);
                     That(fallbackLevel, Is.EqualTo((ushort)0));
                     That(nonNumeric,    Is.EqualTo("noindent"));
                 });
    }

#endregion



#region Interpolation behaviour (exercised through SqlCommand.Parse)

    [Test] public void Column_then_parameter_registers_a_bound_parameter()
    {
        string     adminName = "Admin";
        SqlCommand command   = SqlCommand.Parse<RoleRecord>($"WHERE {nameof(RoleRecord.NameOfRole)} = @{adminName}");

        Multiple(() =>
                 {
                     That(command.SQL,                     Does.Contain("name_of_role = @admin_name"));
                     That(command.Parameters.HasValue,     Is.True);
                     That(command.Parameters!.Value.Count, Is.EqualTo(1));
                 });
    }

    [Test] public void Null_values_render_as_sql_null()
    {
        string? value = null;
        That(SqlCommand.Parse<RoleRecord>($"SELECT {value}").SQL, Does.Contain("NULL"));
    }

    [Test] public void Char_and_guid_values_are_quoted()
    {
        Guid guid = Guid.NewGuid();

        Multiple(() =>
                 {
                     That(SqlCommand.Parse<RoleRecord>($"SELECT {'A'}").SQL,  Does.Contain("'A'"));
                     That(SqlCommand.Parse<RoleRecord>($"SELECT {guid}").SQL, Does.Contain($"'{guid}'"));
                 });
    }

    [Test] public void Enum_values_render_numeric_by_default_and_quoted_with_str_format()
    {
        Multiple(() =>
                 {
                     That(SqlCommand.Parse<RoleRecord>($"SELECT {TestDatabase.TestRight.Read}").SQL,     Does.Contain("1"));
                     That(SqlCommand.Parse<RoleRecord>($"SELECT {TestDatabase.TestRight.Read:str}").SQL, Does.Contain("'Read'"));
                 });
    }

    [Test] public void SqlRaw_is_emitted_without_escaping() => That(SqlCommand.Parse<RoleRecord>($"SELECT {SqlRaw.Create("RAW_TOKEN")}").SQL, Does.Contain("RAW_TOKEN"));

    [Test] public void Numeric_sequences_are_expanded()
    {
        int[]      numbers = [1, 2, 3];
        SqlCommand command = SqlCommand.Parse<RoleRecord>($"IN ({numbers})");

        Multiple(() =>
                 {
                     That(command.SQL, Does.Contain("1"));
                     That(command.SQL, Does.Contain("3"));
                 });
    }

    [Test] public void Parse_honours_explicit_command_type() => That(SqlCommand.Parse<RoleRecord>($"CALL do_thing()", CommandType.StoredProcedure).CommandType, Is.EqualTo(CommandType.StoredProcedure));

#endregion
}
