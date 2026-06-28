// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 06/27/2026

using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = global::Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


[TestFixture]
public sealed class SelectTests
{
    // ---- projection ----
    [Test] public void Select_Columns()         => Assert.That(SqlBuilder.PostgreSQL.Select("id", "name").From("users").Build().Sql,                        Is.EqualTo("SELECT id, name FROM users;"));
    [Test] public void Select_Star()            => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("users").Build().Sql,                                 Is.EqualTo("SELECT * FROM users;"));
    [Test] public void Select_Distinct()        => Assert.That(SqlBuilder.PostgreSQL.SelectDistinct("id").From("users").Build().Sql,                        Is.EqualTo("SELECT DISTINCT id FROM users;"));
    [Test] public void Select_SourcelessBuild() => Assert.That(SqlBuilder.PostgreSQL.Select("1").Build().Sql,                                               Is.EqualTo("SELECT 1;"));
    [Test] public void Select_AddColumn()       => Assert.That(SqlBuilder.PostgreSQL.Select("id").Column("name").Column("email").From("users").Build().Sql, Is.EqualTo("SELECT id, name, email FROM users;"));
    [Test] public void Select_DottedColumn()    => Assert.That(SqlBuilder.PostgreSQL.Select("u.name").From("users", "u").Build().Sql,                       Is.EqualTo("SELECT u.name FROM users u;"));

    // ---- aggregates ----
    [Test] public void Aggregate_CountStar()    => Assert.That(SqlBuilder.PostgreSQL.Select().Count("*").From("users").Build().Sql,                           Is.EqualTo("SELECT COUNT(*) FROM users;"));
    [Test] public void Aggregate_WithAlias()    => Assert.That(SqlBuilder.PostgreSQL.Select("u.name").Count("o.id", "orders").From("users", "u").Build().Sql, Is.EqualTo("SELECT u.name, COUNT(o.id) AS orders FROM users u;"));
    [Test] public void Aggregate_SumMinMaxAvg() => Assert.That(SqlBuilder.PostgreSQL.Select().Sum("a").Min("b").Max("c").Avg("d").From("t").Build().Sql,      Is.EqualTo("SELECT SUM(a), MIN(b), MAX(c), AVG(d) FROM t;"));

    // ---- joins ----
    [Test] public void Join_Inner() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("users", "u").InnerJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Build().Sql, Is.EqualTo("SELECT * FROM users u INNER JOIN orders o ON o.user_id = u.id;"));
    [Test] public void Join_Left()  => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("users", "u").LeftJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Build().Sql,  Is.EqualTo("SELECT * FROM users u LEFT JOIN orders o ON o.user_id = u.id;"));
    [Test] public void Join_Right() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("a").RightJoin("b").On("a.x").EqualToColumn("b.y").Build().Sql,                           Is.EqualTo("SELECT * FROM a RIGHT JOIN b ON a.x = b.y;"));
    [Test] public void Join_Full()  => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("a").FullJoin("b").On("a.x").EqualToColumn("b.y").Build().Sql,                            Is.EqualTo("SELECT * FROM a FULL JOIN b ON a.x = b.y;"));
    [Test] public void Join_Cross() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("a").CrossJoin("b").Build().Sql,                                                          Is.EqualTo("SELECT * FROM a CROSS JOIN b;"));

    // ---- where predicates ----
    [Test] public void Where_GreaterThanInline() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("age").GreaterThan(18).Build().Sql,                                                                                         Is.EqualTo("SELECT * FROM t WHERE age > 18;"));
    [Test] public void Where_AllComparators()    => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("a").EqualTo(1).And("b").NotEqualTo(2).And("c").GreaterOrEqual(3).And("d").LessThan(4).And("e").LessOrEqual(5).Build().Sql, Is.EqualTo("SELECT * FROM t WHERE a = 1 AND b <> 2 AND c >= 3 AND d < 4 AND e <= 5;"));
    [Test] public void Where_OrChain()           => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("a").EqualTo(1).Or("b").EqualTo(2).Build().Sql,                                                                             Is.EqualTo("SELECT * FROM t WHERE a = 1 OR b = 2;"));
    [Test] public void Where_IsNull()            => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("email").IsNull().Build().Sql,                                                                                              Is.EqualTo("SELECT * FROM t WHERE email IS NULL;"));
    [Test] public void Where_IsNotNull()         => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("email").IsNotNull().Build().Sql,                                                                                           Is.EqualTo("SELECT * FROM t WHERE email IS NOT NULL;"));
    [Test] public void Where_Like()              => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("name").Like("A%").Build().Sql,                                                                                             Is.EqualTo("SELECT * FROM t WHERE name LIKE 'A%';"));
    [Test] public void Where_In()                => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("id").In(1, 2, 3).Build().Sql,                                                                                              Is.EqualTo("SELECT * FROM t WHERE id IN (1, 2, 3);"));
    [Test] public void Where_Between()           => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("age").Between(18, 65).Build().Sql,                                                                                         Is.EqualTo("SELECT * FROM t WHERE age BETWEEN 18 AND 65;"));
    [Test] public void Where_EqualToColumn()     => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("a.x").EqualToColumn("b.y").Build().Sql,                                                                                    Is.EqualTo("SELECT * FROM t WHERE a.x = b.y;"));

    // ---- group / having ----
    [Test] public void GroupBy_Multiple() => Assert.That(SqlBuilder.PostgreSQL.Select("a", "b").Count("*", "n").From("t").GroupBy("a", "b").Build().Sql,           Is.EqualTo("SELECT a, b, COUNT(*) AS n FROM t GROUP BY a, b;"));
    [Test] public void Having_Aggregate() => Assert.That(SqlBuilder.PostgreSQL.Select("a").From("t").GroupBy("a").Having().Count("id").GreaterThan(5).Build().Sql, Is.EqualTo("SELECT a FROM t GROUP BY a HAVING COUNT(id) > 5;"));

    // ---- order / paging ----
    [Test] public void OrderBy_Asc()        => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").OrderBy("name").Asc().Build().Sql,              Is.EqualTo("SELECT * FROM t ORDER BY name ASC;"));
    [Test] public void OrderBy_Desc()       => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").OrderByDesc("name").Build().Sql,                Is.EqualTo("SELECT * FROM t ORDER BY name DESC;"));
    [Test] public void OrderBy_ThenBy()     => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").OrderBy("a").ThenByDesc("b").Build().Sql,       Is.EqualTo("SELECT * FROM t ORDER BY a, b DESC;"));
    [Test] public void Paging_LimitOffset() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").OrderBy("id").Limit(50).Offset(10).Build().Sql, Is.EqualTo("SELECT * FROM t ORDER BY id LIMIT 50 OFFSET 10;"));

    // ---- set operations ----
    [Test] public void Union()    => Assert.That(SqlBuilder.PostgreSQL.Select("id").From("a").Where("active").EqualTo(true).Union().Select("id").From("b").Build().Sql, Is.EqualTo("SELECT id FROM a WHERE active = TRUE UNION SELECT id FROM b;"));
    [Test] public void UnionAll() => Assert.That(SqlBuilder.PostgreSQL.Select("id").From("a").UnionAll().Select("id").From("b").Build().Sql,                            Is.EqualTo("SELECT id FROM a UNION ALL SELECT id FROM b;"));

    // ---- structural: parentheses balance ----
    [Test] public void Balanced_AggregatesAndIn()
    {
        string sql = SqlBuilder.PostgreSQL.Select().Count("*", "n").From("t").Where("id").In(1, 2, 3).Build().Sql;
        Assert.That(sql.Split('(').Length, Is.EqualTo(sql.Split(')').Length), "parentheses balanced");
    }
}
