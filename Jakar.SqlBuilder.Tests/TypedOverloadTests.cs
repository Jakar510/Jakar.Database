// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 06/27/2026

using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = global::Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


[TestFixture]
public sealed class TypedOverloadTests
{
    [Test] public void Typed_Select_Postgres() => Assert.That(SqlBuilder.PostgreSQL.Select<User>(nameof(User.ID), nameof(User.Name)).From<User>().Build().Sql, Is.EqualTo("SELECT id, name FROM users;"));

    [Test] public void Typed_Select_SqlServer() => Assert.That(SqlBuilder.SqlServer.Select<User>(nameof(User.ID), nameof(User.Name)).From<User>().Build().Sql, Is.EqualTo("SELECT [id], [name] FROM [users];"));

    [Test] public void Typed_ColumnNameTranslation() => Assert.That(SqlBuilder.PostgreSQL.Select<Order>(nameof(Order.UserID)).From<Order>().Build().Sql, Is.EqualTo("SELECT user_id FROM orders;"));

    [Test] public void Typed_Where() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From<User>().Where<User>(nameof(User.Active)).EqualTo(true).Build().Sql, Is.EqualTo("SELECT * FROM users WHERE active = TRUE;"));

    [Test] public void Typed_Join_OnEqualToColumn() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From<User>("u").LeftJoin<Order>("o").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).Build().Sql, Is.EqualTo("SELECT * FROM users u LEFT JOIN orders o ON user_id = id;"));

    [Test] public void Typed_GroupBy_OrderBy() => Assert.That(SqlBuilder.PostgreSQL.Select<User>(nameof(User.Name)).From<User>().GroupBy<User>(nameof(User.Name)).OrderByDesc<User>(nameof(User.Name)).Build().Sql, Is.EqualTo("SELECT name FROM users GROUP BY name ORDER BY name DESC;"));

    [Test] public void Typed_Aggregate_WithAlias() => Assert.That(SqlBuilder.SqlServer.Select<User>(nameof(User.Name)).Count<Order>(nameof(Order.ID), "orders").From<User>("u").Build().Sql, Is.EqualTo("SELECT [name], COUNT([id]) AS [orders] FROM [users] [u];"));

    [Test] public void Typed_Update() => Assert.That(SqlBuilder.PostgreSQL.Update<User>().Set<User>(nameof(User.Name), SqlValue.Param("Ada")).Where<User>(nameof(User.ID)).EqualTo(42).Build().Sql, Is.EqualTo("UPDATE users SET name = $1 WHERE id = 42;"));

    [Test] public void Typed_Delete() => Assert.That(SqlBuilder.SqlServer.Delete<User>().Where<User>(nameof(User.Active)).EqualTo(false).Build().Sql, Is.EqualTo("DELETE FROM [users] WHERE [active] = 0;"));

    [Test] public void Typed_Insert() => Assert.That(SqlBuilder.PostgreSQL.Insert().Into<User>(nameof(User.Name), nameof(User.Email)).ValuesParams("Ada", "a@x").Returning("id").Build().Sql, Is.EqualTo("INSERT INTO users (name, email) VALUES ($1, $2) RETURNING id;"));

    // ---- full integration chain (matches README example shape; typed columns are emitted unqualified) ----
    [Test] public void Typed_FullChain_Structural()
    {
        string sql = SqlBuilder.SqlServer.Select<User>(nameof(User.Name))
                               .Count<Order>(nameof(Order.ID), "orders")
                               .From<User>("u")
                               .LeftJoin<Order>("o")
                               .On<Order>(nameof(Order.UserID))
                               .EqualToColumn<User>(nameof(User.ID))
                               .Where<User>(nameof(User.Active))
                               .EqualTo(true)
                               .GroupBy<User>(nameof(User.Name))
                               .Having()
                               .Count<Order>(nameof(Order.Name))
                               .GreaterThan(5)
                               .OrderByDesc("orders")
                               .Offset(0)
                               .FetchNext(20)
                               .Build()
                               .Sql;

        Assert.That(sql, Does.StartWith("SELECT [name], COUNT([id]) AS [orders] FROM [users] [u] LEFT JOIN [orders] [o]"));
        Assert.That(sql, Does.Contain("WHERE [active] = 1"));
        Assert.That(sql, Does.Contain("GROUP BY [name] HAVING COUNT([name]) > 5"));
        Assert.That(sql, Does.EndWith("OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;"));
    }
}
