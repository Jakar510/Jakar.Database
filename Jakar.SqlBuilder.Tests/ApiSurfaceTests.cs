// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary> The public surface outside the statement stages: entry points, results, values, the default identifier folder, and <see cref="SqlWriter"/> used directly. </summary>
[TestFixture]
[NonParallelizable] // swaps the process-wide PostgreSQL folder
public sealed class ApiSurfaceTests
{
    [Test] public void Named_Entry_Points_Match_For()
    {
        SqlBuilderOptions options = new() { AppendStatementTerminator = false };

        Assert.Multiple(() =>
                        {
                            Assert.That(SqlBuilder.PostgreSQL.Select("a").From("t").Build().Dialect, Is.EqualTo(SqlDialectKind.PostgreSql));
                            Assert.That(SqlBuilder.SqlServer.Select("a").From("t").Build().Dialect,  Is.EqualTo(SqlDialectKind.SqlServer));
                            Assert.That(SqlBuilder.Sqlite.Select("a").From("t").Build().Dialect,     Is.EqualTo(SqlDialectKind.Sqlite));
                            Assert.That(SqlBuilder.MySql.Select("a").From("t").Build().Dialect,      Is.EqualTo(SqlDialectKind.MySql));
                            Assert.That(SqlBuilder.Oracle.Select("a").From("t").Build().Dialect,     Is.EqualTo(SqlDialectKind.Oracle));
                            Assert.That(SqlBuilder.Firebird.Select("a").From("t").Build().Dialect,   Is.EqualTo(SqlDialectKind.Firebird));

                            Assert.That(SqlBuilder.PostgreSQLWith(options).Select("a").From("t").Build().Sql, Is.EqualTo("SELECT a FROM t"));
                            Assert.That(SqlBuilder.SqlServerWith(options).Select("a").From("t").Build().Sql,  Is.EqualTo("SELECT [a] FROM [t]"));
                            Assert.That(SqlBuilder.SqliteWith(options).Select("a").From("t").Build().Sql,     Is.EqualTo("SELECT \"a\" FROM \"t\""));
                            Assert.That(SqlBuilder.MySqlWith(options).Select("a").From("t").Build().Sql,      Is.EqualTo("SELECT `a` FROM `t`"));
                            Assert.That(SqlBuilder.OracleWith(options).Select("a").From("t").Build().Sql,     Is.EqualTo("SELECT \"a\" FROM \"t\""));
                            Assert.That(SqlBuilder.FirebirdWith(options).Select("a").From("t").Build().Sql,   Is.EqualTo("SELECT \"a\" FROM \"t\""));
                        });
    }

    [Test] public void Options_Defaults()
    {
        SqlBuilderOptions options = SqlBuilderOptions.Default;

        Assert.Multiple(() =>
                        {
                            Assert.That(options.InitialBufferSize,         Is.EqualTo(SqlBuilderOptions.DEFAULT_BUFFER_SIZE));
                            Assert.That(options.AppendStatementTerminator, Is.True);
                            Assert.That(options.StrictTypes,               Is.False);
                            Assert.That(options.AliasProjections,          Is.True);
                            Assert.That(options with { StrictTypes = true }, Is.Not.EqualTo(options));
                        });
    }

    [Test] public void Result_And_Parameters()
    {
        SqlResult result = SqlBuilder.SqlServer.Select("*").From("t").Where("a").EqualTo((object)1).And("b").EqualTo((object)null).Build();
        ( string sql, SqlParameterSet parameters ) = result;
        string implicitSql = result;

        IReadOnlyList<SqlParameter> list       = parameters;
        IEnumerable                 enumerable = parameters;
        int                         counted    = 0;
        foreach ( object? _ in enumerable ) { counted++; }

        Assert.Multiple(() =>
                        {
                            Assert.That(result.ToString(), Is.EqualTo(sql));
                            Assert.That(implicitSql,       Is.EqualTo(sql));
                            Assert.That(parameters.IsEmpty, Is.False);
                            Assert.That(list[0].Name,      Is.EqualTo("@p0"));
                            Assert.That(counted,           Is.EqualTo(2));
                            Assert.That(parameters.ToDictionary(), Is.EquivalentTo(new Dictionary<string, object?> { ["@p0"] = 1, ["@p1"] = null }));
                            Assert.That(list[0].ToString(), Is.EqualTo("@p0 = 1"));
                            Assert.That(list[1].ToString(), Is.EqualTo("@p1 = NULL"));
                            Assert.That(SqlParameterSet.Empty.IsEmpty, Is.True);
                            Assert.That(SqlBuilder.SqlServer.Select("a").From("t").Build().Parameters, Is.SameAs(SqlParameterSet.Empty));
                        });
    }

    [Test] public void Values_Render_Every_Kind()
    {
        Assert.That(SqlBuilder.PostgreSQL.Insert().Into("t").Values(SqlValue.Param(1), SqlValue.Param(null), SqlValue.Inline("x".AsSpan()), SqlValue.Of(7), SqlValue.Of(true), SqlValue.Null, SqlValue.Raw("now()".AsSpan()), default).Build().Sql,
                    Is.EqualTo("INSERT INTO t VALUES ($1, NULL, 'x', 7, TRUE, NULL, now(), $2);"),
                    "default(SqlValue) is a bound null");
    }

    [Test] public void Insert_Without_A_Column_List()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(SqlBuilder.SqlServer.Insert().Into("t").ValuesParams(1).Build().Sql, Is.EqualTo("INSERT INTO [t] VALUES (@p0);"));
                            // A typed insert with no names lists every mapped, non-identity column (spec §9.6).
                            Assert.That(SqlBuilder.SqlServer.Insert<Order>().ValuesParams(1, "a").Build().Sql, Is.EqualTo("INSERT INTO [orders] ([user_id], [name]) VALUES (@p0, @p1);"));
                            Assert.That(() => SqlBuilder.SqlServer.Insert<Order>().ValuesParams(1).Build(), Throws.ArgumentException);
                        });
    }

    [Test] public void Empty_Alias_Is_No_Alias()
        => Assert.That(SqlBuilder.SqlServer.Select("*").From<User>("").InnerJoin<Order>("").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).Build().Sql,
                       Is.EqualTo("SELECT * FROM [users] INNER JOIN [orders] ON [orders].[user_id] = [users].[id];"));

    [Test] public void Default_Postgres_Folder_Is_Snake_Lower()
    {
        Func<string, string> pinned = SqlDialects.PostgresIdentifierFolder;
        SqlDialects.PostgresIdentifierFolder = static name => name;

        try
        {
            Assert.That(SqlBuilder.PostgreSQL.Select("FirstName").From("t").Build().Sql, Is.EqualTo("SELECT FirstName FROM t;"), "a host folder replaces the default");
        }
        finally { SqlDialects.PostgresIdentifierFolder = pinned; }

        Assert.Multiple(() =>
                        {
                            Assert.That(SqlDialectKind.PostgreSql.Fold("UserID"),            Is.EqualTo(TestSchema.SnakeLower("UserID")));
                            Assert.That(SqlDialectKind.SqlServer.Fold("UserID"),             Is.EqualTo("UserID"));
                            Assert.That(SqlDialectKind.PostgreSql.FoldsIdentifiersToLower(), Is.True);
                            Assert.That(SqlDialectKind.MySql.FoldsIdentifiersToLower(),      Is.False);
                        });
    }

    [TestCase("",              "")]
    [TestCase("Name",          "name")]
    [TestCase("FirstName",     "first_name")]
    [TestCase("already_snake", "already_snake")]
    [TestCase("Has_Upper",     "has_upper")]
    public void Builtin_Snake_Case_Folder( string name, string expected )
    {
        Func<string, string> pinned = SqlDialects.PostgresIdentifierFolder;
        SqlDialects.PostgresIdentifierFolder = SqlDialects.DefaultPostgresIdentifierFolder;

        try { Assert.That(SqlDialectKind.PostgreSql.Fold(name), Is.EqualTo(expected)); }
        finally { SqlDialects.PostgresIdentifierFolder = pinned; }
    }

    [Test] public void Table_Names_Come_From_Attributes()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(TableExtensions.GetTableName<AnnotatedTable>(),    Is.EqualTo("annotated"));
                            Assert.That(new AnnotatedTable().GetTableName(),              Is.EqualTo("annotated"));
                            Assert.That(typeof(ContribTable).GetTableName(),               Is.EqualTo("contrib"));
                            Assert.That(typeof(PlainTable).GetTableName(),                 Is.EqualTo(nameof(PlainTable)));
                            Assert.That(typeof(PlainTable).GetTableName(),                 Is.SameAs(typeof(PlainTable).GetTableName()), "cached");
                        });
    }


    // ---- SqlWriter used directly ----

    [Test] public void Writer_Reports_Its_State()
    {
        SqlWriter writer = new(SqlDialectKind.SqlServer, new SqlBuilderOptions { StrictTypes = true, InitialBufferSize = 2 });
        writer.Word("SELECT");
        writer.Space();
        writer.Append(ReadOnlySpan<char>.Empty);
        writer.OpenParen();

        bool   strict   = writer.StrictTypes;
        int    depth    = writer.ParenthesisDepth;
        int    length   = writer.Length;
        string snapshot = writer.Snapshot();

        Assert.Multiple(() =>
                        {
                            Assert.That(strict,   Is.True);
                            Assert.That(depth,    Is.EqualTo(1));
                            Assert.That(length,   Is.EqualTo("SELECT (".Length));
                            Assert.That(snapshot, Is.EqualTo("SELECT ("));
                        });

        SqlBuildError? reason = null;
        try { writer.Build(); }
        catch ( SqlBuildException e ) { reason = e.Reason; }

        Assert.That(reason, Is.EqualTo(SqlBuildError.UnbalancedParentheses));
    }

    [Test] public void Writer_Qualified_Columns()
    {
        SqlWriter writer = new(SqlDialectKind.PostgreSql, SqlBuilderOptions.Default with { AppendStatementTerminator = false });
        writer.QualifiedColumn("T", "FirstName", false);
        writer.Comma();
        writer.QualifiedColumn(null, "LastName", false);
        writer.Comma();
        writer.QualifiedColumn(null, "Age", true);

        Assert.That(writer.Build().Sql, Is.EqualTo("t.first_name, last_name, age"));
    }

    [Test] public void Writer_Output_Position_First_Mark_Wins()
    {
        SqlWriter writer = new(SqlDialectKind.SqlServer, SqlBuilderOptions.Default with { AppendStatementTerminator = false });
        writer.Word("A");
        writer.MarkOutputPosition();
        writer.Word("B");
        writer.MarkOutputPosition();
        writer.Word("C");

        int start = writer.Length;
        writer.Word("X");
        writer.MoveToOutputPosition(start);

        Assert.That(writer.Build().Sql, Is.EqualTo("A X B C"));
    }

    [Test] public void Writer_Without_Output_Mark_Leaves_Text_In_Place()
    {
        SqlWriter writer = new(SqlDialectKind.SqlServer, SqlBuilderOptions.Default with { AppendStatementTerminator = false });
        writer.Word("A");
        int start = writer.Length;
        writer.Word("B");
        writer.MoveToOutputPosition(start);

        Assert.That(writer.Build().Sql, Is.EqualTo("A B"));
    }

    [Test] public void Writer_Grows_Its_Table_Scope()
    {
        SqlWriter writer = new(SqlDialectKind.PostgreSql, SqlBuilderOptions.Default);
        for ( int i = 0; i < 9; i++ ) { writer.RegisterTable($"t{i}", null); }

        writer.RegisterTable("aliased", "a");

        string? unjoined = writer.QualifierFor("t8");
        string? aliased  = writer.QualifierFor("aliased");
        string? missing  = writer.QualifierFor("missing");

        Assert.Multiple(() =>
                        {
                            Assert.That(unjoined, Is.Null, "no join yet: unqualified");
                            Assert.That(aliased,  Is.EqualTo("a"));
                            Assert.That(missing,  Is.Null);
                        });

        writer.MarkClause(ClauseFlags.Join);
        Assert.That(writer.QualifierFor("t8"), Is.EqualTo("t8"));

        writer.ResetTables();
        Assert.That(writer.QualifierFor("aliased"), Is.Null);
        _ = writer.Build();
    }



    [Table("annotated")] public sealed class AnnotatedTable;
    [Dapper.Contrib.Extensions.Table("contrib")] public sealed class ContribTable;
    public sealed class PlainTable;
}
