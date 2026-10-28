// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Buffers;
using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary>
///     Stages are single-use and share one pooled buffer. Building a stage twice returns the same result; continuing a stage that a later stage has moved past, or one whose
///     statement was already streamed by <c>WriteTo</c>, throws instead of writing into a buffer another stage (or another query) owns.
/// </summary>
[TestFixture]
public sealed class ReuseTests
{
    private static SelectWhereStage Query( SqlDialectKind dialect = SqlDialectKind.SqlServer ) => SqlBuilder.For(dialect).Select("id").From("t").Where("a").EqualTo((object)1);


    // ---- idempotent terminals ----

    [Test] public void Build_Twice_Returns_The_Same_Result()
    {
        SelectWhereStage query  = Query();
        SqlResult        first  = query.Build();
        SqlResult        second = query.Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(first.Sql,                Is.EqualTo("SELECT [id]\nFROM [t]\nWHERE [a] = @p0;"));
                            Assert.That(second.Sql,               Is.SameAs(first.Sql));
                            Assert.That(second.Parameters,        Is.SameAs(first.Parameters));
                            Assert.That(second.Parameters.Count, Is.EqualTo(1));
                        });
    }

    [Test] public void BuildSql_And_Build_Agree_In_Either_Order()
    {
        SelectWhereStage a = Query();
        string           s = a.BuildSql();
        SqlResult        r = a.Build();

        SelectWhereStage b  = Query();
        SqlResult        r2 = b.Build();
        string           s2 = b.BuildSql();

        Assert.Multiple(() =>
                        {
                            Assert.That(r.Sql,               Is.EqualTo(s));
                            Assert.That(r.Parameters.Count, Is.EqualTo(1), "BuildSql first still keeps the parameters for a later Build");
                            Assert.That(s2,                  Is.EqualTo(r2.Sql));
                        });
    }

    [Test] public void Sourceless_Select_Builds_Twice()
    {
        SelectStage select = SqlBuilder.Oracle.Select().Column(static e => e.Number(1));
        string      first  = select.BuildSql();

        Assert.That(select.BuildSql(), Is.EqualTo(first).And.EqualTo("SELECT 1\nFROM DUAL"));
    }

    [Test] public void Terminal_After_WriteTo_Throws()
    {
        SelectWhereStage        query       = Query();
        ArrayBufferWriter<char> destination = new();
        _ = query.WriteTo(destination);

        Assert.Multiple(() =>
                        {
                            Assert.That(destination.WrittenSpan.ToString(), Is.EqualTo("SELECT [id]\nFROM [t]\nWHERE [a] = @p0;"));
                            Assert.That(static () => Query().WriteToTwice(), Throws.InvalidOperationException);
                        });
    }


    // ---- single-use stages ----

    [Test] public void Continuing_An_Earlier_Stage_Throws()
    {
        FromStage        from  = SqlBuilder.SqlServer.Select("id").From("t");
        SelectWhereStage where = from.Where("a").EqualTo(1);
        Exception?       stale = Catch(from);
        string           sql   = where.BuildSql();

        Assert.Multiple(() =>
                        {
                            Assert.That(stale, Is.TypeOf<InvalidOperationException>());
                            Assert.That(sql,   Is.EqualTo("SELECT [id]\nFROM [t]\nWHERE [a] = 1;"), "the stage that moved on is unharmed");
                        });
    }

    [Test] public void Continuing_A_Built_Statement_Throws()
    {
        FromStage from = SqlBuilder.SqlServer.Select("id").From("t");
        _ = from.Where("a").EqualTo(1).BuildSql();

        Assert.That(Catch(from), Is.TypeOf<InvalidOperationException>());
    }

    [Test] public void A_Released_Buffer_Is_Never_Shared()
    {
        FromStage from = SqlBuilder.SqlServer.Select("id").From("t");
        _ = from.BuildSql();
        string other  = SqlBuilder.SqlServer.Select("zzzzzz").From("other_table").BuildSql();
        string replay = from.BuildSql();

        Assert.Multiple(() =>
                        {
                            Assert.That(replay, Is.EqualTo("SELECT [id]\nFROM [t];"), "the built stage replays its own result");
                            Assert.That(other,  Is.EqualTo("SELECT [zzzzzz]\nFROM [other_table];"));
                        });
    }

    [Test] public void Nested_Builders_Do_Not_Interfere()
    {
        string outer = SqlBuilder.PostgreSQL.Select("id")
                                 .From("t")
                                 .Where(p => p.Column("a").EqualTo(SqlBuilder.PostgreSQL.Select("x").From("u").BuildSql().Length))
                                 .BuildSql();

        Assert.That(outer, Is.EqualTo("SELECT id\nFROM t\nWHERE (a = 16);")); // "SELECT x FROM u;" is 16 characters
    }

    [Test] public void Identical_Chains_Build_Identically()
    {
        for ( int i = 0; i < 3; i++ )
        {
            SqlResult result = SqlBuilder.PostgreSQL.Select("id").From("t").Where("a").EqualTo((object)i).OrderBy("id").Limit(5).Build();
            Assert.Multiple(() =>
                            {
                                Assert.That(result.Sql,                     Is.EqualTo("SELECT id\nFROM t\nWHERE a = $1\nORDER BY id\nLIMIT 5;"));
                                Assert.That(result.Parameters[0].Value,    Is.EqualTo(i));
                                Assert.That(result.Parameters.Count,       Is.EqualTo(1));
                            });
        }
    }

    /// <summary> Continues <paramref name="from"/> and returns what it threw (or <see langword="null"/>). </summary>
    private static Exception? Catch( FromStage from )
    {
        try
        {
            _ = from.OrderBy("id").BuildSql();
            return null;
        }
        catch ( Exception e ) { return e; }
    }
}



internal static class ReuseTestExtensions
{
    public static void WriteToTwice( this SelectWhereStage query )
    {
        ArrayBufferWriter<char> destination = new();
        _ = query.WriteTo(destination);
        _ = query.WriteTo(destination);
    }
}
