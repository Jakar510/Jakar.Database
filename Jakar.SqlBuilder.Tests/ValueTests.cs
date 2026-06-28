// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 06/27/2026

using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = global::Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


[TestFixture]
public sealed class ValueTests
{
    [Test] public void Inline_NumberLiteral()
        => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("age").GreaterThan(18).Build().Sql, Is.EqualTo("SELECT * FROM t WHERE age > 18;"));

    [Test] public void Inline_NegativeNumber()
        => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("balance").LessThan(-5).Build().Sql, Is.EqualTo("SELECT * FROM t WHERE balance < -5;"));

    [Test] public void Bound_Parameter_CapturesValue()
    {
        SqlResult r = SqlBuilder.PostgreSQL.Select("*").From("t").Where("status").EqualTo(SqlValue.Param("active")).Build();
        Assert.That(r.Sql,                 Is.EqualTo("SELECT * FROM t WHERE status = $1;"));
        Assert.That(r.Parameters.Count,    Is.EqualTo(1));
        Assert.That(r.Parameters[0].Value, Is.EqualTo("active"));
    }

    [Test] public void Inline_StringEscapesSingleQuote()
        => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("name").EqualTo(SqlValue.Inline("O'Brien")).Build().Sql,
                       Is.EqualTo("SELECT * FROM t WHERE name = 'O''Brien';"));

    [Test] public void Raw_EmittedVerbatim_NoParens_Counted()
        => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("ts").GreaterThan(SqlValue.Raw("NOW()")).Build().Sql,
                       Is.EqualTo("SELECT * FROM t WHERE ts > NOW();"));

    [Test] public void ParamNull_IsInlineNull_NotBound()
    {
        SqlResult r = SqlBuilder.PostgreSQL.Select("*").From("t").Where("x").EqualTo(SqlValue.Param(null)).Build();
        Assert.That(r.Sql,              Is.EqualTo("SELECT * FROM t WHERE x = NULL;"));
        Assert.That(r.Parameters.IsEmpty, Is.True);
    }

    [Test] public void Ordinals_AreSequential()
    {
        SqlResult r = SqlBuilder.PostgreSQL.Select("*").From("t")
                                .Where("a").EqualTo(SqlValue.Param(1))
                                .And("b").EqualTo(SqlValue.Param(2))
                                .And("c").EqualTo(SqlValue.Param(3)).Build();
        Assert.That(r.Sql, Is.EqualTo("SELECT * FROM t WHERE a = $1 AND b = $2 AND c = $3;"));
        Assert.That(r.Parameters.Count, Is.EqualTo(3));
        Assert.That(r.Parameters[2].Value, Is.EqualTo(3));
    }

    [Test] public void SqlValueOf_Long()
        => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("x").EqualTo(SqlValue.Of(7L)).Build().Sql, Is.EqualTo("SELECT * FROM t WHERE x = 7;"));

    [Test] public void SqlValueOf_Bool_Postgres()
        => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Where("x").EqualTo(SqlValue.Of(true)).Build().Sql, Is.EqualTo("SELECT * FROM t WHERE x = TRUE;"));

    [Test] public void ToDictionary_KeysAreParamNames()
    {
        SqlResult r = SqlBuilder.PostgreSQL.Select("*").From("t").Where("a").EqualTo(SqlValue.Param("v")).Build();
        System.Collections.Generic.Dictionary<string, object?> map = r.Parameters.ToDictionary();
        Assert.That(map.ContainsKey("$1"), Is.True);
        Assert.That(map["$1"], Is.EqualTo("v"));
    }
}
