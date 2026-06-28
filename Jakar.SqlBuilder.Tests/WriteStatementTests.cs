// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 06/27/2026

using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = global::Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


[TestFixture]
public sealed class InsertTests
{
    [Test] public void Insert_RawColumns_MixedValues()
    {
        SqlResult r = SqlBuilder.PostgreSQL.Insert().Into("users", "name", "email").Values(SqlValue.Param("Ada"), SqlValue.Inline("ada@x.io")).Build();
        Assert.That(r.Sql,                 Is.EqualTo("INSERT INTO users (name, email) VALUES ($1, 'ada@x.io');"));
        Assert.That(r.Parameters.Count,    Is.EqualTo(1));
        Assert.That(r.Parameters[0].Value, Is.EqualTo("Ada"));
    }

    [Test] public void Insert_AllBound() => Assert.That(SqlBuilder.PostgreSQL.Insert().Into("users", "name", "email").ValuesParams("Ada", "ada@x.io").Build().Sql, Is.EqualTo("INSERT INTO users (name, email) VALUES ($1, $2);"));

    [Test] public void Insert_MultiRow() => Assert.That(SqlBuilder.PostgreSQL.Insert().Into("users", "name").Values(SqlValue.Param("Ada")).Values(SqlValue.Param("Bob")).Build().Sql, Is.EqualTo("INSERT INTO users (name) VALUES ($1), ($2);"));

    [Test] public void Insert_Returning_Postgres() => Assert.That(SqlBuilder.PostgreSQL.Insert().Into("users", "name").ValuesParams("Ada").Returning("id").Build().Sql, Is.EqualTo("INSERT INTO users (name) VALUES ($1) RETURNING id;"));

    [Test] public void Insert_NullValueIsInlineNull() => Assert.That(SqlBuilder.PostgreSQL.Insert().Into("users", "name").Values(SqlValue.Null).Build().Sql, Is.EqualTo("INSERT INTO users (name) VALUES (NULL);"));

    [Test] public void Insert_SqlServerQuoting() => Assert.That(SqlBuilder.SqlServer.Insert().Into("users", "name").ValuesParams("Ada").Build().Sql, Is.EqualTo("INSERT INTO [users] ([name]) VALUES (@p0);"));

    // OUTPUT is dialect-gated and emitted (placement vs. canonical T-SQL is a known limitation — see SPECIFICATION).
    [Test] public void Insert_Output_SqlServer_ContainsClause() => Assert.That(SqlBuilder.SqlServer.Insert().Into("users", "name").ValuesParams("Ada").Output("id").Build().Sql, Does.Contain("OUTPUT INSERTED.[id]"));
}



[TestFixture]
public sealed class UpdateTests
{
    [Test] public void Update_SetWhere() => Assert.That(SqlBuilder.PostgreSQL.Update("users").Set("name", SqlValue.Param("Ada")).Where("id").EqualTo(42).Build().Sql, Is.EqualTo("UPDATE users SET name = $1 WHERE id = 42;"));

    [Test] public void Update_MultipleAssignments() => Assert.That(SqlBuilder.PostgreSQL.Update("t").Set("a", 1).Set("b", 2).Build().Sql, Is.EqualTo("UPDATE t SET a = 1, b = 2;"));

    [Test] public void Update_SetParam() => Assert.That(SqlBuilder.PostgreSQL.Update("t").SetParam("a", "x").Where("id").EqualTo(1).Build().Sql, Is.EqualTo("UPDATE t SET a = $1 WHERE id = 1;"));

    [Test] public void Update_Returning() => Assert.That(SqlBuilder.PostgreSQL.Update("t").Set("a", 1).Where("id").EqualTo(1).Returning("id").Build().Sql, Is.EqualTo("UPDATE t SET a = 1 WHERE id = 1 RETURNING id;"));
}



[TestFixture]
public sealed class DeleteTests
{
    [Test] public void Delete_Where() => Assert.That(SqlBuilder.PostgreSQL.DeleteFrom("users").Where("active").EqualTo(false).Build().Sql, Is.EqualTo("DELETE FROM users WHERE active = FALSE;"));

    [Test] public void Delete_All() => Assert.That(SqlBuilder.PostgreSQL.DeleteFrom("users").Build().Sql, Is.EqualTo("DELETE FROM users;"));

    [Test] public void Delete_Returning() => Assert.That(SqlBuilder.PostgreSQL.DeleteFrom("t").Where("id").EqualTo(1).Returning("id").Build().Sql, Is.EqualTo("DELETE FROM t WHERE id = 1 RETURNING id;"));

    [Test] public void Delete_AndOr_Chain() => Assert.That(SqlBuilder.PostgreSQL.DeleteFrom("t").Where("a").EqualTo(1).And("b").EqualTo(2).Or("c").IsNull().Build().Sql, Is.EqualTo("DELETE FROM t WHERE a = 1 AND b = 2 OR c IS NULL;"));
}
