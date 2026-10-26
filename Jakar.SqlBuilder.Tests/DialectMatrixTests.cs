// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary> The exact SQL every dialect-divergent feature renders, for all six dialects. <see cref="DialectExecutionTestsBase"/> runs the same patterns on the real engines. </summary>
[TestFixture]
public sealed class DialectMatrixTests
{
    private const SqlDialectKind PG = SqlDialectKind.PostgreSql;
    private const SqlDialectKind MS = SqlDialectKind.SqlServer;
    private const SqlDialectKind LITE = SqlDialectKind.Sqlite;
    private const SqlDialectKind MY = SqlDialectKind.MySql;
    private const SqlDialectKind ORA = SqlDialectKind.Oracle;
    private const SqlDialectKind FB = SqlDialectKind.Firebird;

    private static SqlRoot For( SqlDialectKind dialect ) => SqlBuilder.For(dialect);


    // ---- identifiers ----

    [TestCase(PG,   "SELECT name, first_name FROM users;")]
    [TestCase(MS,   "SELECT [Name], [FirstName] FROM [Users];")]
    [TestCase(LITE, "SELECT \"Name\", \"FirstName\" FROM \"Users\";")]
    [TestCase(MY,   "SELECT `Name`, `FirstName` FROM `Users`;")]
    [TestCase(ORA,  "SELECT \"Name\", \"FirstName\" FROM \"Users\"")]
    [TestCase(FB,   "SELECT \"Name\", \"FirstName\" FROM \"Users\";")]
    public void Identifiers_Are_Quoted_Or_Folded( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Select("Name", "FirstName").From("Users").Build().Sql, Is.EqualTo(expected));

    [TestCase(MS,   "SELECT [a]]b] FROM [t];")]
    [TestCase(LITE, "SELECT \"a\"\"b\" FROM \"t\";")]
    [TestCase(MY,   "SELECT `a``b` FROM `t`;")]
    [TestCase(ORA,  "SELECT \"a\"\"b\" FROM \"t\"")]
    [TestCase(FB,   "SELECT \"a\"\"b\" FROM \"t\";")]
    public void Quote_Characters_Are_Escaped( SqlDialectKind dialect, string expected )
    {
        string name = dialect switch
                      {
                          MS => "a]b",
                          MY => "a`b",
                          _  => "a\"b"
                      };

        Assert.That(For(dialect).Select(name).From("t").Build().Sql, Is.EqualTo(expected));
    }

    [TestCase(PG,   "SELECT u.name FROM users u;")]
    [TestCase(MS,   "SELECT [u].[name] FROM [users] [u];")]
    [TestCase(MY,   "SELECT `u`.`name` FROM `users` `u`;")]
    [TestCase(ORA,  "SELECT \"u\".\"name\" FROM \"users\" \"u\"")]
    public void Qualified_Columns_Quote_Each_Part( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Select("u.name").From("users", "u").Build().Sql, Is.EqualTo(expected));


    // ---- parameters ----

    [TestCase(PG,   "$1",  "$2")]
    [TestCase(MS,   "@p0", "@p1")]
    [TestCase(LITE, "@p0", "@p1")]
    [TestCase(MY,   "@p0", "@p1")]
    [TestCase(ORA,  ":p0", ":p1")]
    [TestCase(FB,   "@p0", "@p1")]
    public void Parameter_Tokens( SqlDialectKind dialect, string first, string second )
    {
        SqlResult result = For(dialect).Select("*").From("t").Where("a").EqualTo((object)1).And("b").EqualTo((object)"x").Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,                 Does.Contain($"= {first} AND").And.EndWith($"= {second}" + Terminator(dialect)));
                            Assert.That(result.Parameters.Count,    Is.EqualTo(2));
                            Assert.That(result.Parameters[0].Name,  Is.EqualTo(first));
                            Assert.That(result.Parameters[1].Name,  Is.EqualTo(second));
                            Assert.That(result.Parameters[1].Value, Is.EqualTo("x"));
                        });
    }


    // ---- literals ----

    [TestCase(PG,   "TRUE",  "FALSE")]
    [TestCase(MS,   "1",     "0")]
    [TestCase(LITE, "1",     "0")]
    [TestCase(MY,   "1",     "0")]
    [TestCase(ORA,  "1",     "0")]
    [TestCase(FB,   "TRUE",  "FALSE")]
    public void Booleans( SqlDialectKind dialect, string yes, string no )
    {
        Assert.That(For(dialect).Select("*").From("t").Where("a").EqualTo(true).Or("b").EqualTo(false).Build().Sql,
                    Does.EndWith($"= {yes} OR {Q(dialect, "b")} = {no}{Terminator(dialect)}"));
    }

    [TestCase(PG)] [TestCase(MS)] [TestCase(LITE)] [TestCase(MY)] [TestCase(ORA)] [TestCase(FB)]
    public void Inline_Strings_Escape_Quotes( SqlDialectKind dialect ) => Assert.That(For(dialect).Select("*").From("t").Where("a").Like("O'Brien%").Build().Sql, Does.Contain("LIKE 'O''Brien%'"));


    // ---- paging ----

    [TestCase(PG,   "ORDER BY id LIMIT 5;")]
    [TestCase(MS,   "ORDER BY [id] OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY;")]
    [TestCase(LITE, "ORDER BY \"id\" LIMIT 5;")]
    [TestCase(MY,   "ORDER BY `id` LIMIT 5;")]
    [TestCase(ORA,  "ORDER BY \"id\" FETCH NEXT 5 ROWS ONLY")]
    [TestCase(FB,   "ORDER BY \"id\" FETCH NEXT 5 ROWS ONLY;")]
    public void Limit( SqlDialectKind dialect, string expectedTail ) => Assert.That(For(dialect).Select("*").From("t").OrderBy("id").Limit(5).Build().Sql, Does.EndWith(expectedTail));

    [TestCase(PG,   "ORDER BY id OFFSET 2;")]
    [TestCase(MS,   "ORDER BY [id] OFFSET 2 ROWS;")]
    [TestCase(LITE, "ORDER BY \"id\" LIMIT -1 OFFSET 2;")]
    [TestCase(MY,   "ORDER BY `id` LIMIT 18446744073709551615 OFFSET 2;")]
    [TestCase(ORA,  "ORDER BY \"id\" OFFSET 2 ROWS")]
    [TestCase(FB,   "ORDER BY \"id\" OFFSET 2 ROWS;")]
    public void Offset_Alone( SqlDialectKind dialect, string expectedTail ) => Assert.That(For(dialect).Select("*").From("t").OrderBy("id").Offset(2).Build().Sql, Does.EndWith(expectedTail));

    [TestCase(PG,   "LIMIT 5 OFFSET 2;")]
    [TestCase(MS,   "OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY;")]
    [TestCase(LITE, "LIMIT 5 OFFSET 2;")]
    [TestCase(MY,   "LIMIT 5 OFFSET 2;")]
    [TestCase(ORA,  "OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY")]
    [TestCase(FB,   "OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY;")]
    public void Limit_And_Offset_In_Either_Order( SqlDialectKind dialect, string expectedTail )
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(For(dialect).Select("*").From("t").OrderBy("id").Limit(5).Offset(2).Build().Sql,     Does.EndWith(expectedTail));
                            Assert.That(For(dialect).Select("*").From("t").OrderBy("id").Offset(2).FetchNext(5).Build().Sql, Does.EndWith(expectedTail));
                        });
    }

    [TestCase(PG,   "SELECT id FROM t LIMIT 3;")]
    [TestCase(MS,   "SELECT TOP (3) [id] FROM [t];")]
    [TestCase(LITE, "SELECT \"id\" FROM \"t\" LIMIT 3;")]
    [TestCase(MY,   "SELECT `id` FROM `t` LIMIT 3;")]
    [TestCase(ORA,  "SELECT \"id\" FROM \"t\" FETCH NEXT 3 ROWS ONLY")]
    [TestCase(FB,   "SELECT \"id\" FROM \"t\" FETCH NEXT 3 ROWS ONLY;")]
    public void Top( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Select().Top(3).Column("id").From("t").Build().Sql, Is.EqualTo(expected));

    [Test] public void SqlServer_Paging_Needs_OrderBy()
        => Assert.That(() => SqlBuilder.SqlServer.Select("*").From("t").Limit(5).Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.MissingRequiredClause));

    [Test] public void SqlServer_Top_And_Paging_Conflict()
        => Assert.That(() => SqlBuilder.SqlServer.Select().Top(3).Column("id").From("t").OrderBy("id").Offset(1).Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));

    [TestCase(PG)] [TestCase(MS)] [TestCase(LITE)] [TestCase(MY)] [TestCase(ORA)] [TestCase(FB)]
    public void Negative_Paging_Is_Rejected( SqlDialectKind dialect )
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(() => For(dialect).Select("*").From("t").OrderBy("id").Limit(-1).Build(),  Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.InvalidLiteral));
                            Assert.That(() => For(dialect).Select("*").From("t").OrderBy("id").Offset(-1).Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.InvalidLiteral));
                            Assert.That(() => For(dialect).Select().Top(-1).Column("id").From("t").Build(),       Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.InvalidLiteral));
                        });
    }


    // ---- statement shape ----

    [TestCase(PG,   "SELECT 1;")]
    [TestCase(MS,   "SELECT 1;")]
    [TestCase(LITE, "SELECT 1;")]
    [TestCase(MY,   "SELECT 1;")]
    [TestCase(ORA,  "SELECT 1 FROM DUAL")]
    [TestCase(FB,   "SELECT 1 FROM RDB$DATABASE;")]
    public void Sourceless_Select( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Select().Column("1").Build().Sql.Replace("\"1\"", "1").Replace("[1]", "1").Replace("`1`", "1"), Is.EqualTo(expected));

    [TestCase(PG)] [TestCase(MS)] [TestCase(LITE)] [TestCase(MY)] [TestCase(ORA)] [TestCase(FB)]
    public void Terminator_Can_Be_Turned_Off( SqlDialectKind dialect )
        => Assert.That(SqlBuilder.For(dialect, new SqlBuilderOptions { AppendStatementTerminator = false }).Select("a").From("t").Build().Sql, Does.Not.EndWith(";"));

    [TestCase(PG,   true)]
    [TestCase(MS,   true)]
    [TestCase(LITE, true)]
    [TestCase(MY,   false)]
    [TestCase(ORA,  true)]
    [TestCase(FB,   true)]
    public void Full_Join_Support( SqlDialectKind dialect, bool supported )
    {
        Action build = () => For(dialect).Select("*").From("a").FullJoin("b").On("a.x").EqualToColumn("b.y").Build();

        if ( supported ) { Assert.That(build, Throws.Nothing); }
        else { Assert.That(build, Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature)); }
    }


    // ---- RETURNING / OUTPUT ----

    [TestCase(PG,   "INSERT INTO t (a) VALUES ($1) RETURNING id;")]
    [TestCase(LITE, "INSERT INTO \"t\" (\"a\") VALUES (@p0) RETURNING \"id\";")]
    [TestCase(FB,   "INSERT INTO \"t\" (\"a\") VALUES (@p0) RETURNING \"id\";")]
    public void Insert_Returning( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Insert().Into("t", "a").ValuesParams(1).Returning("id").Build().Sql, Is.EqualTo(expected));

    [TestCase(MS)] [TestCase(MY)] [TestCase(ORA)]
    public void Returning_Is_Rejected_Where_Unsupported( SqlDialectKind dialect )
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(() => For(dialect).Insert().Into("t", "a").ValuesParams(1).Returning("id").Build(),           Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));
                            Assert.That(() => For(dialect).Update("t").Set("a", 1).Where("id").EqualTo(1).Returning("id").Build(),   Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));
                            Assert.That(() => For(dialect).DeleteFrom("t").Where("id").EqualTo(1).Returning("id").Build(),           Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));
                        });
    }

    [Test] public void SqlServer_Output_Goes_Before_Values_And_Where()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(SqlBuilder.SqlServer.Insert().Into("t", "a").ValuesParams(1).Values(SqlValue.Param(2)).Output("id").Build().Sql,
                                        Is.EqualTo("INSERT INTO [t] ([a]) OUTPUT INSERTED.[id] VALUES (@p0), (@p1);"));
                            Assert.That(SqlBuilder.SqlServer.Update("t").Set("a", 1).Where("id").EqualTo(2).Output("id", "a").Build().Sql,
                                        Is.EqualTo("UPDATE [t] SET [a] = 1 OUTPUT INSERTED.[id], INSERTED.[a] WHERE [id] = 2;"));
                            Assert.That(SqlBuilder.SqlServer.Update("t").Set("a", 1).Output("id").Build().Sql,
                                        Is.EqualTo("UPDATE [t] SET [a] = 1 OUTPUT INSERTED.[id];"));
                            Assert.That(SqlBuilder.SqlServer.DeleteFrom("t").Where("id").EqualTo(2).And("a").IsNull().Output("id").Build().Sql,
                                        Is.EqualTo("DELETE FROM [t] OUTPUT DELETED.[id] WHERE [id] = 2 AND [a] IS NULL;"));
                            Assert.That(SqlBuilder.SqlServer.DeleteFrom("t").Output("id").Build().Sql,
                                        Is.EqualTo("DELETE FROM [t] OUTPUT DELETED.[id];"));
                        });
    }

    [TestCase(PG)] [TestCase(LITE)] [TestCase(MY)] [TestCase(ORA)] [TestCase(FB)]
    public void Output_Is_SqlServer_Only( SqlDialectKind dialect )
        => Assert.That(() => For(dialect).DeleteFrom("t").Output("id").Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));


    // ---- structural guards (dialect-independent) ----

    [TestCase(PG)] [TestCase(MS)] [TestCase(LITE)] [TestCase(MY)] [TestCase(ORA)] [TestCase(FB)]
    /// <summary> An empty column / value list is argument misuse (spec §10): <see cref="ArgumentException"/> before any SQL is written; an empty <c>IN ()</c> is an empty predicate. </summary>
    public void Empty_Lists_Are_Rejected( SqlDialectKind dialect )
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(() => For(dialect).Select("*").From("t").Where("id").In().Build(),        Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.EmptyPredicate));
                            Assert.That(() => For(dialect).Select("*").From("t").Where("id").InParams().Build(),  Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.EmptyPredicate));
                            Assert.That(() => For(dialect).Select("a").From("t").GroupBy().Build(),               Throws.ArgumentException);
                            Assert.That(() => For(dialect).Select().From("t").Build(),                            Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.MissingRequiredClause));
                            Assert.That(() => For(dialect).Select().Build(),                                      Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.MissingRequiredClause));
                            Assert.That(() => For(dialect).Insert().Into("t", "a").Values().Build(),              Throws.ArgumentException);
                            Assert.That(() => For(dialect).Insert().Into("t", "a").ValuesParams().Build(),        Throws.ArgumentException);
                            Assert.That(() => For(dialect).Insert().Into("t", "a").ValuesParams(1).Values().Build(), Throws.ArgumentException);
                        });
    }

    [Test] public void NotSet_Dialect_Is_Rejected()
        => Assert.That(() => SqlBuilder.For(SqlDialectKind.NotSet).Select("a").From("t").Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.DialectNotSet));


    private static string Terminator( SqlDialectKind dialect ) => dialect.SupportsStatementTerminator()
                                                                     ? ";"
                                                                     : string.Empty;
    private static string Q( SqlDialectKind dialect, string name )
    {
        ( char open, char close ) = dialect.QuoteChars();
        return open is '\0'
                   ? name
                   : $"{open}{name}{close}";
    }
}
