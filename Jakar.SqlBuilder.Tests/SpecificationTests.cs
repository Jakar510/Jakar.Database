// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using NUnit.Framework;
using Jakar.SqlBuilder;
using Jakar.SqlBuilder.Tests.Execution;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary>
///     The worked examples of SPECIFICATION.md, verbatim where the API allows, plus golden SQL for every statement form whose text differs per dialect (the engines in
///     <c>Execution/</c> prove each form runs; these pin its exact shape).
/// </summary>
[TestFixture]
public sealed class SpecificationTests
{
    private const SqlDialectKind PG   = SqlDialectKind.PostgreSql;
    private const SqlDialectKind MS   = SqlDialectKind.SqlServer;
    private const SqlDialectKind LITE = SqlDialectKind.Sqlite;
    private const SqlDialectKind MY   = SqlDialectKind.MySql;
    private const SqlDialectKind ORA  = SqlDialectKind.Oracle;
    private const SqlDialectKind FB   = SqlDialectKind.Firebird;

    private static SqlRoot For( SqlDialectKind dialect ) => SqlBuilder.For(dialect);


    // ---- the specification's own examples ----

    /// <summary> §7.1: window function + CTE. </summary>
    [Test] public void Spec_7_1_Cte_With_Window()
        => Assert.That(SqlBuilder.PostgreSQL
                                 .With("ranked").As(cte => cte
                                                           .Select("user_id", "score")
                                                           .Column(c => c.RowNumber().Over(o => o.PartitionBy("user_id").OrderByDesc("score")), "rn")
                                                           .From("scores"))
                                 .Select("user_id", "score")
                                 .From("ranked")
                                 .Where("rn").EqualTo(1)
                                 .Build().Sql,
                       Is.EqualTo("WITH ranked AS (\n    SELECT\n        user_id,\n        score,\n        ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY score DESC) AS rn\n    FROM scores\n)\nSELECT\n    user_id,\n    score\nFROM ranked\nWHERE rn = 1;"));

    /// <summary> §7.2: upsert with RETURNING. </summary>
    [Test] public void Spec_7_2_Upsert()
    {
        SqlResult result = SqlBuilder.PostgreSQL
                                     .Insert().Into<User>(nameof(User.Name), nameof(User.Email))
                                     .ValuesParams("Ada", "ada@x.io")
                                     .OnConflict("email").DoUpdateSet("name", fromExcluded: true)
                                     .Returning("id")
                                     .Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,               Is.EqualTo("INSERT INTO users (\n    name,\n    email\n)\nVALUES (\n    $1,\n    $2\n)\nON CONFLICT (email)\nDO UPDATE SET name = EXCLUDED.name\nRETURNING id;"));
                            Assert.That(result.Parameters.Count, Is.EqualTo(2));
                        });
    }

    /// <summary> §7.6: lambda groups balance their parentheses. </summary>
    [Test] public void Spec_7_6_Grouping()
    {
        SqlResult result = SqlBuilder.PostgreSQL.Select("*").From("t")
                                     .Where(g => g.Column("age").GreaterThan(18).And("country").EqualTo("US"))
                                     .Or(g => g.Column("vip").EqualTo(true))
                                     .Build();

        Assert.That(result.Sql, Is.EqualTo("SELECT *\nFROM t\nWHERE (age > 18 AND country = $1)\n    OR (vip = TRUE);"));
    }

    /// <summary> §8.3: join, group, lambda HAVING, order, paging on SQL Server. </summary>
    [Test] public void Spec_8_3_End_To_End()
        => Assert.That(SqlBuilder.SqlServer
                                 .Select("u.name").Count("o.id", "orders")
                                 .From("users", "u")
                                 .LeftJoin("orders", "o").On("o.user_id").EqualToColumn("u.id")
                                 .Where("u.active").EqualTo(true)
                                 .GroupBy("u.name")
                                 .Having(h => h.Count("o.id").GreaterThan(5))
                                 .OrderByDesc("orders")
                                 .Offset(0).FetchNext(20)
                                 .Build().Sql,
                       Is.EqualTo("SELECT\n    [u].[name],\n    COUNT([o].[id]) AS [orders]\nFROM [users] [u]\nLEFT JOIN [orders] [o] ON [o].[user_id] = [u].[id]\nWHERE [u].[active] = 1\nGROUP BY [u].[name]\nHAVING COUNT([o].[id]) > 5\nORDER BY [orders] DESC\nOFFSET 0 ROWS\nFETCH NEXT 20 ROWS ONLY;"));

    /// <summary> §9.3: the same query through the typed overloads, with the projections back-patched to the aliases (§9.5) and a bare <c>On</c> bound to the join's record. </summary>
    [Test] public void Spec_9_3_Typed()
        => Assert.That(SqlBuilder.SqlServer
                                 .Select<User>(nameof(User.Name)).Count<Order>(nameof(Order.ID), "orders")
                                 .From<User>("u")
                                 .LeftJoin<Order>("o").On(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID))
                                 .Where<User>(nameof(User.Active)).EqualTo(true)
                                 .GroupBy<User>(nameof(User.Name))
                                 .Having(h => h.Count<Order>(nameof(Order.Name)).GreaterThan(5))
                                 .OrderByDesc("orders")
                                 .Offset(0).FetchNext(20)
                                 .Build().Sql,
                       Is.EqualTo("SELECT\n    [u].[name],\n    COUNT([o].[id]) AS [orders]\nFROM [users] [u]\nLEFT JOIN [orders] [o] ON [o].[user_id] = [u].[id]\nWHERE [u].[active] = 1\nGROUP BY [u].[name]\nHAVING COUNT([o].[name]) > 5\nORDER BY [orders] DESC\nOFFSET 0 ROWS\nFETCH NEXT 20 ROWS ONLY;"));

    /// <summary> §9.4: StrictTypes rejects a value of the wrong type and <c>IS NULL</c> on a non-nullable column. </summary>
    [Test] public void Spec_9_4_StrictTypes()
    {
        SqlBuilderOptions strict = SqlBuilderOptions.Default with { StrictTypes = true };

        Assert.Multiple(() =>
                        {
                            Assert.That(() => SqlBuilder.For(MS, strict).Select("*").From<User>().Where<User>(nameof(User.ID)).EqualTo("oops").Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.TypeMismatch));
                            Assert.That(() => SqlBuilder.For(MS, strict).Select("*").From<User>().Where<User>(nameof(User.Name)).IsNull().Build(),     Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.TypeMismatch));
                            Assert.That(() => SqlBuilder.For(MS, strict).Update<User>().Set<User>(nameof(User.Active), SqlValue.Inline("x")).Build(),  Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.TypeMismatch));
                            Assert.That(SqlBuilder.For(MS, strict).Select("*").From<User>().Where<User>(nameof(User.ID)).EqualTo(42).Build().Sql,       Is.EqualTo("SELECT *\nFROM [users]\nWHERE [id] = 42;"));
                            Assert.That(SqlBuilder.For(MS, strict).Select("*").From<User>().Where<User>(nameof(User.Email)).IsNull().Build().Sql,      Is.EqualTo("SELECT *\nFROM [users]\nWHERE [email] IS NULL;"));
                        });
    }


    // ---- per-dialect statement forms ----

    [TestCase(PG,  "SELECT id\nFROM t\nORDER BY id\nFETCH NEXT 3 ROWS WITH TIES;")]
    [TestCase(MS,  "SELECT TOP (3) WITH TIES [id]\nFROM [t]\nORDER BY [id];")]
    [TestCase(ORA, "SELECT \"id\"\nFROM \"t\"\nORDER BY \"id\"\nFETCH NEXT 3 ROWS WITH TIES")]
    public void Top_With_Ties( SqlDialectKind dialect, string expected )
        => Assert.That(For(dialect).Select("id").Top(3, withTies: true).From("t").OrderBy("id").Build().Sql, Is.EqualTo(expected));

    [Test] public void Top_With_Ties_Needs_Order_By()
        => Assert.That(() => SqlBuilder.SqlServer.Select("id").Top(3, withTies: true).From("t").Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.MissingRequiredClause));

    [TestCase(PG,   "SELECT DISTINCT ON (user_id)\n    user_id,\n    total\nFROM orders;")]
    public void Distinct_On( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).SelectDistinctOn(["user_id"], "user_id", "total").From("orders").Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,   "SELECT x\nFROM a\nORDER BY x NULLS FIRST;")]
    [TestCase(MS,   "SELECT [x]\nFROM [a]\nORDER BY\n    CASE WHEN [x] IS NULL THEN 0 ELSE 1 END,\n    [x];")]
    [TestCase(MY,   "SELECT `x`\nFROM `a`\nORDER BY\n    CASE WHEN `x` IS NULL THEN 0 ELSE 1 END,\n    `x`;")]
    [TestCase(LITE, "SELECT \"x\"\nFROM \"a\"\nORDER BY \"x\" NULLS FIRST;")]
    public void Nulls_First( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Select("x").From("a").OrderBy("x").NullsFirst().Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,   "WITH RECURSIVE n (x) AS (\n    SELECT 1\n    UNION ALL\n    SELECT x + 1\n    FROM n\n    WHERE x < 5\n)\nSELECT x\nFROM n;")]
    [TestCase(MS,   "WITH [n] ([x]) AS (\n    SELECT 1\n    UNION ALL\n    SELECT [x] + 1\n    FROM [n]\n    WHERE [x] < 5\n)\nSELECT [x]\nFROM [n];")]
    [TestCase(ORA,  "WITH \"n\" (\"x\") AS (\n    SELECT 1\n    FROM DUAL\n    UNION ALL\n    SELECT \"x\" + 1\n    FROM \"n\"\n    WHERE \"x\" < 5\n)\nSELECT \"x\"\nFROM \"n\"")]
    public void With_Recursive( SqlDialectKind dialect, string expected )
        => Assert.That(For(dialect).WithRecursive("n", "x").As(q => q.Select().Column(e => e.Number(1)).UnionAll().Select().Column(e => e.Column("x").Plus().Number(1)).From("n").Where("x").LessThan(5)).Select("x").From("n").Build().Sql,
                       Is.EqualTo(expected));

    [TestCase(PG,   "WITH a AS MATERIALIZED (\n    SELECT id\n    FROM t\n)\nSELECT id\nFROM a;")]
    [TestCase(LITE, "WITH \"a\" AS MATERIALIZED (\n    SELECT \"id\"\n    FROM \"t\"\n)\nSELECT \"id\"\nFROM \"a\";")]
    public void Cte_Materialized( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).With("a").AsMaterialized(q => q.Select("id").From("t")).Select("id").From("a").Build().Sql, Is.EqualTo(expected));

    [Test] public void Dml_With_Is_Gated()
        => Assert.Multiple(() =>
                           {
                               Assert.That(() => For(MY).With("a").As(q => q.Select("id").From("t")).Insert().Into("u", "id").Select(q => q.Select("id").From("a")).Build(),  Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));
                               Assert.That(() => For(ORA).With("a").As(q => q.Select("id").From("t")).DeleteFrom("u").Build(),                                         Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));
                               Assert.That(For(MY).With("a").As(q => q.Select("id").From("t")).DeleteFrom("u").Where("id").In(q => q.Select("id").From("a")).Build().Sql, Is.EqualTo("WITH `a` AS (\n    SELECT `id`\n    FROM `t`\n)\nDELETE FROM `u`\nWHERE `id` IN (\n    SELECT `id`\n    FROM `a`\n);"));
                           });

    [TestCase(PG,   "SELECT id\nFROM t\nWHERE id = 1\nLIMIT 1\nFOR UPDATE SKIP LOCKED;")]
    [TestCase(MY,   "SELECT `id`\nFROM `t`\nWHERE `id` = 1\nLIMIT 1\nFOR UPDATE SKIP LOCKED;")]
    [TestCase(FB,   "SELECT \"id\"\nFROM \"t\"\nWHERE \"id\" = 1\nFETCH NEXT 1 ROWS ONLY\nFOR UPDATE WITH LOCK SKIP LOCKED;")]
    public void Lock_After_Paging( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Select("id").From("t").Where("id").EqualTo(1).ForUpdate().SkipLocked().Limit(1).Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,   "INSERT INTO t\nDEFAULT VALUES;")]
    [TestCase(MS,   "INSERT INTO [t]\nDEFAULT VALUES;")]
    [TestCase(MY,   "INSERT INTO `t` ()\nVALUES ();")]
    [TestCase(LITE, "INSERT INTO \"t\"\nDEFAULT VALUES;")]
    [TestCase(FB,   "INSERT INTO \"t\"\nDEFAULT VALUES;")]
    public void Default_Values( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Insert().Into("t").DefaultValues().Build().Sql, Is.EqualTo(expected));

    [Test] public void Default_Values_Oracle_Is_Rejected()
        => Assert.That(() => For(ORA).Insert().Into("t").DefaultValues().Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));

    [Test] public void Default_Values_Output_Goes_Before()
        => Assert.That(For(MS).Insert().Into("t").DefaultValues().Output("id").Build().Sql, Is.EqualTo("INSERT INTO [t]\nOUTPUT INSERTED.[id]\nDEFAULT VALUES;"));

    [TestCase(PG,   "INSERT INTO t (\n    id,\n    n\n)\nVALUES (\n    1,\n    'a'\n)\nON CONFLICT (id)\nDO NOTHING;")]
    [TestCase(LITE, "INSERT INTO \"t\" (\n    \"id\",\n    \"n\"\n)\nVALUES (\n    1,\n    'a'\n)\nON CONFLICT (\"id\")\nDO NOTHING;")]
    [TestCase(MY,   "INSERT INTO `t` (\n    `id`,\n    `n`\n)\nVALUES (\n    1,\n    'a'\n)\nON DUPLICATE KEY UPDATE `id` = `id`;")]
    public void On_Conflict_Do_Nothing( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Insert().Into("t", "id", "n").Values(SqlValue.Of(1), SqlValue.Inline("a")).OnConflict("id").DoNothing().Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,   "INSERT INTO t (\n    id,\n    n\n)\nVALUES (\n    1,\n    'a'\n)\nON CONFLICT (id)\nDO UPDATE SET\n    n = EXCLUDED.n,\n    c = 2;")]
    [TestCase(MY,   "INSERT INTO `t` (\n    `id`,\n    `n`\n)\nVALUES (\n    1,\n    'a'\n) AS excluded\nON DUPLICATE KEY UPDATE\n    `n` = excluded.`n`,\n    `c` = 2;")]
    public void On_Conflict_Do_Update( SqlDialectKind dialect, string expected )
        => Assert.That(For(dialect).Insert().Into("t", "id", "n").Values(SqlValue.Of(1), SqlValue.Inline("a")).OnConflict("id").DoUpdateSet("n", fromExcluded: true).Set("c", SqlValue.Of(2)).Build().Sql, Is.EqualTo(expected));

    [Test] public void Insert_Value_Count_Must_Match_Columns()
        => Assert.That(() => For(PG).Insert().Into("t", "a", "b").Values(SqlValue.Of(1)).Build(), Throws.ArgumentException);

    [Test] public void Insert_Select()
        => Assert.That(For(PG).Insert().Into("t", "a").Select(q => q.Select("x").From("s").Where("y").EqualTo(1)).Returning("a").Build().Sql, Is.EqualTo("INSERT INTO t (a)\nSELECT x\nFROM s\nWHERE y = 1\nRETURNING a;"));

    [Test] public void Overriding_System_Value()
        => Assert.That(For(PG).Insert().Into("t", "id").OverridingSystemValue().Values(SqlValue.Of(7)).Build().Sql, Is.EqualTo("INSERT INTO t (id) OVERRIDING SYSTEM VALUE\nVALUES (7);"));

    [TestCase(PG,   "UPDATE orders o\nSET total = 0\nFROM users u\nWHERE u.id = o.user_id;")]
    [TestCase(MS,   "UPDATE [o]\nSET [total] = 0\nFROM\n    [orders] [o],\n    [users] [u]\nWHERE [u].[id] = [o].[user_id];")]
    [TestCase(LITE, "UPDATE \"orders\" AS \"o\"\nSET \"total\" = 0\nFROM \"users\" \"u\"\nWHERE \"u\".\"id\" = \"o\".\"user_id\";")]
    [TestCase(MY,   "UPDATE `orders` `o`, `users` `u`\nSET `total` = 0\nWHERE `u`.`id` = `o`.`user_id`;")]
    public void Update_From( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Update("orders", "o").Set("total", SqlValue.Of(0)).From("users", "u").Where("u.id").EqualToColumn("o.user_id").Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,   "UPDATE t\nSET (a, b) = (1, 2)\nWHERE id = 1;")]
    [TestCase(MS,   "UPDATE [t]\nSET\n    [a] = 1,\n    [b] = 2\nWHERE [id] = 1;")]
    public void Update_Set_Row( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Update("t").SetRow(["a", "b"], SqlValue.Of(1), SqlValue.Of(2)).Where("id").EqualTo(1).Build().Sql, Is.EqualTo(expected));

    [Test] public void Update_Alias_Output_Sql_Server()
        => Assert.That(For(MS).Update("orders", "o").Set("total", SqlValue.Of(0)).Where("o.id").EqualTo(1).Output("id").Build().Sql, Is.EqualTo("UPDATE [o]\nSET [total] = 0\nOUTPUT INSERTED.[id]\nFROM [orders] [o]\nWHERE [o].[id] = 1;"));

    [TestCase(PG,   "DELETE FROM orders o\nUSING users u\nWHERE u.id = o.user_id;")]
    [TestCase(MS,   "DELETE [o]\nFROM\n    [orders] [o],\n    [users] [u]\nWHERE [u].[id] = [o].[user_id];")]
    [TestCase(MY,   "DELETE `o`\nFROM\n    `orders` `o`,\n    `users` `u`\nWHERE `u`.`id` = `o`.`user_id`;")]
    public void Delete_Using( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).DeleteFrom("orders", "o").Using("users", "u").Where("u.id").EqualToColumn("o.user_id").Build().Sql, Is.EqualTo(expected));

    [TestCase(MS, "DELETE FROM [orders]\nFROM [users] [u]\nWHERE [u].[id] = [orders].[user_id];")]
    [TestCase(MY, "DELETE `orders`\nFROM\n    `orders`,\n    `users` `u`\nWHERE `u`.`id` = `orders`.`user_id`;")]
    public void Delete_Using_Without_Alias( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).DeleteFrom("orders").Using("users", "u").Where("u.id").EqualToColumn("orders.user_id").Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,   "TRUNCATE TABLE t;")]
    [TestCase(MS,   "TRUNCATE TABLE [t];")]
    [TestCase(MY,   "TRUNCATE TABLE `t`;")]
    [TestCase(ORA,  "TRUNCATE TABLE \"t\"")]
    [TestCase(LITE, "DELETE FROM \"t\";")]
    [TestCase(FB,   "DELETE FROM \"t\";")]
    public void Truncate( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).Truncate("t").Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,  "MERGE INTO t a\nUSING s b ON (a.id = b.id)\nWHEN MATCHED AND (b.n > 1) THEN UPDATE SET n = b.n\nWHEN NOT MATCHED THEN INSERT (id, n) VALUES (b.id, b.n);")]
    [TestCase(MS,  "MERGE INTO [t] [a]\nUSING [s] [b] ON ([a].[id] = [b].[id])\nWHEN MATCHED AND ([b].[n] > 1) THEN UPDATE SET [n] = [b].[n]\nWHEN NOT MATCHED THEN INSERT ([id], [n]) VALUES ([b].[id], [b].[n]);")]
    [TestCase(ORA, "MERGE INTO \"t\" \"a\"\nUSING \"s\" \"b\" ON (\"a\".\"id\" = \"b\".\"id\")\nWHEN MATCHED THEN UPDATE SET \"n\" = \"b\".\"n\"\nWHERE \"b\".\"n\" > 1\nWHEN NOT MATCHED THEN INSERT (\"id\", \"n\") VALUES (\"b\".\"id\", \"b\".\"n\")")]
    public void Merge( SqlDialectKind dialect, string expected )
        => Assert.That(For(dialect).MergeInto("t", "a").Using("s", "b").On(p => p.Column("a.id").EqualToColumn("b.id"))
                                   .WhenMatchedThenUpdate(s => s.Set("n", e => e.Column("b.n")), p => p.Column("b.n").GreaterThan(1))
                                   .WhenNotMatchedThenInsert(["id", "n"], [e => e.Column("b.id"), e => e.Column("b.n")])
                                   .Build().Sql,
                       Is.EqualTo(expected));

    [Test] public void Merge_Sql_Server_Always_Terminated()
        => Assert.That(SqlBuilder.SqlServerWith(SqlBuilderOptions.Default with { AppendStatementTerminator = false }).MergeInto("t", "a").Using("s", "b").On(p => p.Column("a.id").EqualToColumn("b.id")).WhenMatchedThenDelete().Build().Sql, Does.EndWith(";"));

    [TestCase(LITE)] [TestCase(MY)]
    public void Merge_Is_Gated( SqlDialectKind dialect )
        => Assert.That(() => For(dialect).MergeInto("t", "a"), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.UnsupportedFeature));


    // ---- DDL ----

    [TestCase(PG,   "CREATE TABLE t (\n    id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,\n    n VARCHAR(10) DEFAULT 'x' NOT NULL UNIQUE,\n    u INTEGER REFERENCES users (id) ON DELETE CASCADE\n);")]
    [TestCase(MS,   "CREATE TABLE [t] (\n    [id] BIGINT IDENTITY(1,1) PRIMARY KEY,\n    [n] NVARCHAR(10) DEFAULT 'x' NOT NULL UNIQUE,\n    [u] INT REFERENCES [users] ([id]) ON DELETE CASCADE\n);")]
    [TestCase(LITE, "CREATE TABLE \"t\" (\n    \"id\" INTEGER PRIMARY KEY AUTOINCREMENT,\n    \"n\" TEXT DEFAULT 'x' NOT NULL UNIQUE,\n    \"u\" INTEGER REFERENCES \"users\" (\"id\") ON DELETE CASCADE\n);")]
    [TestCase(MY,   "CREATE TABLE `t` (\n    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,\n    `n` VARCHAR(10) DEFAULT 'x' NOT NULL UNIQUE,\n    `u` INT,\n    FOREIGN KEY (`u`) REFERENCES `users` (`id`) ON DELETE CASCADE\n);")]
    [TestCase(ORA,  "CREATE TABLE \"t\" (\n    \"id\" NUMBER(19) GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,\n    \"n\" VARCHAR2(10) DEFAULT 'x' NOT NULL UNIQUE,\n    \"u\" NUMBER(10) REFERENCES \"users\" (\"id\") ON DELETE CASCADE\n)")]
    public void Create_Table( SqlDialectKind dialect, string expected )
        => Assert.That(For(dialect).CreateTable("t")
                                   .Column("id", SqlType.BigInt, c => c.Identity().PrimaryKey())
                                   .Column("n", SqlType.VarChar(10), c => c.NotNull().Unique().Default(SqlValue.Inline("x")))
                                   .Column("u", SqlType.Int, c => c.References("users", "id", SqlReferentialAction.Cascade))
                                   .Build().Sql,
                       Is.EqualTo(expected));

    [Test] public void Create_Table_If_Not_Exists_Sql_Server_Guard()
        => Assert.That(For(MS).CreateTable("t", true).Column("a", SqlType.Int).Build().Sql, Is.EqualTo("IF OBJECT_ID('t', 'U') IS NULL CREATE TABLE [t] ([a] INT);"));

    [Test] public void Ddl_Rejects_Parameters()
        => Assert.That(() => For(PG).CreateTable("t").Column("a", SqlType.Int, c => c.Default(SqlValue.Param(1))).Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.InvalidLiteral));

    [Test] public void Ddl_Identity_And_Default_Conflict()
        => Assert.That(() => For(PG).CreateTable("t").Column("a", SqlType.Int, c => c.Identity().Default(SqlValue.Of(1))).Build(), Throws.ArgumentException);

    [Test] public void Create_Table_Needs_A_Column()
        => Assert.That(() => For(PG).CreateTable("t").Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.MissingRequiredClause));

    [TestCase(PG,   "CREATE TABLE widgets (\n    id BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL PRIMARY KEY,\n    code VARCHAR(20) NOT NULL UNIQUE,\n    qty INTEGER DEFAULT 0 NOT NULL CHECK (qty >= 0),\n    note VARCHAR(100),\n    owner_id INTEGER REFERENCES users (id) ON DELETE SET NULL\n);")]
    [TestCase(MY,   "CREATE TABLE `widgets` (\n    `id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,\n    `code` VARCHAR(20) NOT NULL UNIQUE,\n    `qty` INT DEFAULT 0 NOT NULL CHECK (`qty` >= 0),\n    `note` VARCHAR(100),\n    `owner_id` INT,\n    FOREIGN KEY (`owner_id`) REFERENCES `users` (`id`) ON DELETE SET NULL\n);")]
    public void Create_Table_From_Metadata( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).CreateTable<Widget>().Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,   "ALTER TABLE t ALTER COLUMN a TYPE BIGINT;")]
    [TestCase(MS,   "ALTER TABLE [t] ALTER COLUMN [a] BIGINT;")]
    [TestCase(MY,   "ALTER TABLE `t` MODIFY COLUMN `a` BIGINT;")]
    [TestCase(ORA,  "ALTER TABLE \"t\" MODIFY (\"a\" NUMBER(19))")]
    [TestCase(FB,   "ALTER TABLE \"t\" ALTER COLUMN \"a\" TYPE BIGINT;")]
    public void Alter_Column_Type( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).AlterTable("t").AlterColumnType("a", SqlType.BigInt).Build().Sql, Is.EqualTo(expected));

    [TestCase(PG, "ALTER TABLE t RENAME COLUMN a TO b;")]
    [TestCase(MS, "EXEC sp_rename 't.a', 'b', 'COLUMN';")]
    [TestCase(FB, "ALTER TABLE \"t\" ALTER COLUMN \"a\" TO \"b\";")]
    public void Rename_Column( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).AlterTable("t").RenameColumn("a", "b").Build().Sql, Is.EqualTo(expected));

    [TestCase(PG,  "DROP TABLE IF EXISTS t CASCADE;")]
    [TestCase(ORA, "DROP TABLE IF EXISTS \"t\" CASCADE CONSTRAINTS")]
    public void Drop_Table( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).DropTable("t", true, true).Build().Sql, Is.EqualTo(expected));

    [TestCase(PG, "DROP INDEX IF EXISTS ix;")]
    [TestCase(MS, "DROP INDEX IF EXISTS [ix] ON [t];")]
    [TestCase(MY, "DROP INDEX `ix` ON `t`;")]
    public void Drop_Index( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).DropIndex("ix", "t", dialect is not MY).Build().Sql, Is.EqualTo(expected));

    [Test] public void Create_Index_Full_Postgres()
        => Assert.That(For(PG).CreateIndex("ix", "t", true, true).Using("gin").Columns("a").Include("b").Where(p => p.Column("a").IsNotNull()).Build().Sql, Is.EqualTo("CREATE UNIQUE INDEX IF NOT EXISTS ix ON t USING gin (a) INCLUDE (b)\nWHERE a IS NOT NULL;"));

    [Test] public void Create_Index_If_Not_Exists_Sql_Server_Guard()
        => Assert.That(For(MS).CreateIndex("ix", "t", ifNotExists: true).Columns("a").Build().Sql, Is.EqualTo("IF NOT EXISTS (\n    SELECT 1\n    FROM sys.indexes\n    WHERE name = 'ix'\n        AND object_id = OBJECT_ID('t')\n)\nCREATE INDEX [ix] ON [t] ([a]);"));

    [TestCase(PG, "CREATE OR REPLACE VIEW v AS\nSELECT id\nFROM t;")]
    [TestCase(MS, "CREATE OR ALTER VIEW [v] AS\nSELECT [id]\nFROM [t];")]
    [TestCase(FB, "CREATE OR ALTER VIEW \"v\" AS\nSELECT \"id\"\nFROM \"t\";")]
    public void Create_Or_Replace_View( SqlDialectKind dialect, string expected ) => Assert.That(For(dialect).CreateOrReplaceView("v", q => q.Select("id").From("t")).Build().Sql, Is.EqualTo(expected));

    [Test] public void View_Rejects_Parameters()
        => Assert.That(() => For(PG).CreateView("v", q => q.Select("id").From("t").Where("id").EqualTo((object)1)).Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.InvalidLiteral));
}
