// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 06/27/2026

using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = global::Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


[TestFixture]
public sealed class DialectTests
{
    // ---- identifier folding / quoting ----
    [Test] public void Identifiers_Postgres_FoldsToSnakeLower() => Assert.That(SqlBuilder.PostgreSQL.Select("Name", "FirstName").From("Users").Build().Sql, Is.EqualTo("SELECT name, first_name FROM users;"));

    [Test] public void Identifiers_SqlServer_BracketsPreserveCase() => Assert.That(SqlBuilder.SqlServer.Select("Name").From("Users").Build().Sql, Is.EqualTo("SELECT [Name] FROM [Users];"));

    [Test] public void Identifiers_Sqlite_DoubleQuotesPreserveCase() => Assert.That(SqlBuilder.Sqlite.Select("Name").From("Users").Build().Sql, Is.EqualTo("SELECT \"Name\" FROM \"Users\";"));

    [Test] public void Identifiers_SqlServer_EscapesCloseBracket() => Assert.That(SqlBuilder.SqlServer.Select("a]b").From("t").Build().Sql, Is.EqualTo("SELECT [a]]b] FROM [t];"));

    [Test] public void Identifiers_Sqlite_EscapesDoubleQuote() => Assert.That(SqlBuilder.Sqlite.Select("a\"b").From("t").Build().Sql, Is.EqualTo("SELECT \"a\"\"b\" FROM \"t\";"));

    // ---- parameters ----
    [Test] public void Parameters_Postgres_Dollar()
    {
        SqlResult r = SqlBuilder.PostgreSQL.Select("*").From("t").Where("a").EqualTo(SqlValue.Param(1)).And("b").EqualTo(SqlValue.Param(2)).Build();
        Assert.That(r.Parameters[0].Name, Is.EqualTo("$1"));
        Assert.That(r.Parameters[1].Name, Is.EqualTo("$2"));
    }

    [Test] public void Parameters_SqlServer_AtP()
    {
        SqlResult r = SqlBuilder.SqlServer.Select("*").From("t").Where("a").EqualTo(SqlValue.Param(1)).And("b").EqualTo(SqlValue.Param(2)).Build();
        Assert.That(r.Parameters[0].Name, Is.EqualTo("@p0"));
        Assert.That(r.Parameters[1].Name, Is.EqualTo("@p1"));
    }

    // ---- booleans ----
    [Test] public void Bool_Postgres_TrueFalse() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("active").EqualTo(true).Build().Sql, Is.EqualTo("SELECT * FROM t WHERE active = TRUE;"));

    [Test] public void Bool_SqlServer_OneZero() => Assert.That(SqlBuilder.SqlServer.Select("*").From("t").Where("active").EqualTo(false).Build().Sql, Is.EqualTo("SELECT * FROM [t] WHERE [active] = 0;"));

    // ---- paging ----
    [Test] public void Paging_Postgres_LimitOffset() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").OrderBy("id").Limit(5).Build().Sql, Is.EqualTo("SELECT * FROM t ORDER BY id LIMIT 5;"));

    [Test] public void Paging_SqlServer_OffsetFetch() => Assert.That(SqlBuilder.SqlServer.Select("*").From("t").OrderBy("id").Offset(0).FetchNext(20).Build().Sql, Is.EqualTo("SELECT * FROM [t] ORDER BY [id] OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;"));

    [Test] public void Paging_SqlServer_LimitBecomesOffsetFetch() => Assert.That(SqlBuilder.SqlServer.Select("*").From("t").OrderBy("id").Limit(5).Build().Sql, Is.EqualTo("SELECT * FROM [t] ORDER BY [id] OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY;"));

    [Test] public void Paging_Sqlite_LimitOffset() => Assert.That(SqlBuilder.Sqlite.Select("*").From("t").OrderBy("id").Limit(5).Offset(2).Build().Sql, Is.EqualTo("SELECT * FROM \"t\" ORDER BY \"id\" LIMIT 5 OFFSET 2;"));

    // ---- TOP (SQL Server only) ----
    [Test] public void Top_SqlServer_Emits() => Assert.That(SqlBuilder.SqlServer.Select().Top(10).Column("id").From("t").Build().Sql, Is.EqualTo("SELECT TOP (10) [id] FROM [t];"));

    [Test] public void Top_Postgres_NoOp() => Assert.That(SqlBuilder.PostgreSQL.Select().Top(10).Column("id").From("t").Build().Sql, Is.EqualTo("SELECT id FROM t;"));
}
