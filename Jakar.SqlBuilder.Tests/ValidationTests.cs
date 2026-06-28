// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 06/27/2026

using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = global::Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


[TestFixture]
public sealed class ValidationTests
{
    [Test] public void Returning_OnSqlServer_Throws()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(static () => { SqlBuilder.SqlServer.Insert().Into("t", "a").ValuesParams(1).Returning("a"); })!;
        Assert.That(ex.Reason, Is.EqualTo(SqlBuildError.UnsupportedFeature));
    }

    [Test] public void Output_OnPostgres_Throws()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(static () => { SqlBuilder.PostgreSQL.Insert().Into("t", "a").ValuesParams(1).Output("a"); })!;
        Assert.That(ex.Reason, Is.EqualTo(SqlBuildError.UnsupportedFeature));
    }

    [Test] public void FullJoin_OnUnsupportedDialect_Throws()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(static () => { SqlBuilder.For(SqlDialectKind.MySql).Select("*").From("a").FullJoin("b"); })!;
        Assert.That(ex.Reason, Is.EqualTo(SqlBuildError.UnsupportedFeature));
    }

    [Test] public void NotSetDialect_Throws()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(static () => { SqlBuilder.For(SqlDialectKind.NotSet); })!;
        Assert.That(ex.Reason, Is.EqualTo(SqlBuildError.DialectNotSet));
    }

    [Test] public void UnknownColumn_Throws()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(static () => { SqlBuilder.PostgreSQL.Select<User>("NotAColumn"); })!;
        Assert.That(ex.Reason, Is.EqualTo(SqlBuildError.UnknownColumn));
    }

    [Test] public void Returning_Throws_CarriesPartialSql()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(static () => { SqlBuilder.SqlServer.DeleteFrom("t").Where("id").EqualTo(1).Returning("id"); })!;
        Assert.That(ex.PartialSql, Does.Contain("DELETE FROM [t]"));
        Assert.That(ex.Dialect,    Is.EqualTo(SqlDialectKind.SqlServer));
    }

    // ---- result behavior ----
    [Test] public void Parameters_Empty_WhenNoneBound() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From("t").Build().Parameters.IsEmpty, Is.True);

    [Test] public void Result_ImplicitStringConversion()
    {
        string sql = SqlBuilder.PostgreSQL.Select("*").From("t").Build();
        Assert.That(sql, Is.EqualTo("SELECT * FROM t;"));
    }

    [Test] public void Result_Deconstruct()
    {
        ( string sql, SqlParameterSet parameters ) = SqlBuilder.PostgreSQL.Select("*").From("t").Where("a").EqualTo(SqlValue.Param(1)).Build();
        Assert.That(sql,              Is.EqualTo("SELECT * FROM t WHERE a = $1;"));
        Assert.That(parameters.Count, Is.EqualTo(1));
    }

    [Test] public void Options_NoTerminator()
    {
        SqlBuilderOptions opts = new() { AppendStatementTerminator = false };
        Assert.That(SqlBuilder.PostgreSQLWith(opts).Select("*").From("t").Build().Sql, Is.EqualTo("SELECT * FROM t"));
    }

    [Test] public void Result_DialectRecorded() => Assert.That(SqlBuilder.Sqlite.Select("*").From("t").Build().Dialect, Is.EqualTo(SqlDialectKind.Sqlite));

    [Test] public void EmptyParameterSet_Singleton() => Assert.That(SqlParameterSet.Empty.IsEmpty, Is.True);
}
