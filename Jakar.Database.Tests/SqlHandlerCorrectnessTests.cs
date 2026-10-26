// Jakar.Database :: Jakar.Database.Tests
// 10/25/2026

namespace Jakar.Database.Tests;


/// <summary>
///     What <see cref="SqlInterpolatedStringHandler{TSelf}"/> must get right whatever its implementation: inline text is escaped, <c>indent|format</c> splits correctly, a value after
///     <c>@</c> binds as a parameter of the column named before it, and finishing the handler is idempotent.
/// </summary>
[TestFixture]
public sealed class SqlHandlerCorrectnessTests : Assert
{
    [Test] public void Inline_strings_escape_single_quotes()
    {
        string   name  = "O'Brien";
        string[] names = ["a'b", "c"];

        Multiple(() =>
                 {
                     That(SqlCommand.Parse<RoleRecord>($"WHERE name = {name}").SQL, Is.EqualTo("WHERE name = 'O''Brien'"));
                     That(SqlCommand.Parse<RoleRecord>($"IN ({names})").SQL,        Is.EqualTo("IN ('a''b',\n'c')"));
                 });
    }

    [Test] public void Indent_and_format_split_at_the_pipe()
    {
        int[] ids = [1, 2];

        Multiple(() =>
                 {
                     That(SqlCommand.Parse<RoleRecord>($"X\n{ids:1|D3}").SQL, Is.EqualTo("X\n    001,\n    002"));
                     That(SqlCommand.Parse<RoleRecord>($"X\n{ids:1}").SQL,    Is.EqualTo("X\n    1,\n    2"));
                     That(SqlCommand.Parse<RoleRecord>($"X {ids:D2}").SQL,    Is.EqualTo("X 01,\n02"));
                 });
    }

    [Test] public void Value_after_at_binds_as_a_parameter_of_the_named_column()
    {
        int        count  = 42;
        Guid       id     = Guid.Parse("019f1ef9-49ca-7a30-b254-eab2321f9e51");
        SqlCommand number = SqlCommand.Parse<RoleRecord>($"WHERE {nameof(RoleRecord.NameOfRole)} = @{count}");
        SqlCommand guid   = SqlCommand.Parse<RoleRecord>($"WHERE {nameof(RoleRecord.ID)} = @{id}");

        Multiple(() =>
                 {
                     That(number.SQL,                     Is.EqualTo("WHERE name_of_role = @count"));
                     That(number.Parameters!.Value.Count, Is.EqualTo(1));
                     That(number.Parameters!.Value.Values[0].Value, Is.EqualTo(42));
                     That(guid.SQL,                       Is.EqualTo("WHERE id = @id"));
                     That(guid.Parameters!.Value.Values[0].Value, Is.EqualTo(id));
                     That(() => SqlCommand.Parse<RoleRecord>($"WHERE x = @{count}"), NUnit.Framework.Throws.TypeOf<FormatException>(), "no column was named for the parameter");
                 });
    }

    [Test] public void Finishing_the_handler_is_idempotent()
    {
        SqlInterpolatedStringHandler<RoleRecord> handler = new(7, 1);
        handler.AppendLiteral("SELECT ");
        handler.AppendFormatted(42);

        string     before = handler.ToString();
        SqlCommand first  = handler.ToSqlCommand();
        SqlCommand second = handler.ToSqlCommand();
        string     after  = handler.ToString();

        Multiple(() =>
                 {
                     That(before,     Is.EqualTo("SELECT 42"));
                     That(first.SQL,  Is.EqualTo("SELECT 42"));
                     That(second.SQL, Is.SameAs(first.SQL));
                     That(after,      Is.EqualTo("SELECT 42"));
                 });
    }

    [TestCase("  a  \n\n\n  b  \n")]
    [TestCase("\n\nx\n")]
    [TestCase("plain")]
    [TestCase("a\r\nb")]
    public void Clean_is_idempotent( string sql )
    {
        string once = SqlInterpolatedStringHandler<RoleRecord>.Clean(sql);
        That(SqlInterpolatedStringHandler<RoleRecord>.Clean(once), Is.EqualTo(once));
    }

    [Test] public void Clean_normalizes_blank_lines_and_trailing_space()
        => That(SqlInterpolatedStringHandler<RoleRecord>.Clean("  a  \n\n\n  b  \n"), Is.EqualTo("  a\n\n  b"));
}
