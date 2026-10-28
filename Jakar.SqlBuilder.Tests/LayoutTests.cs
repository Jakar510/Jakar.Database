// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/26/2026

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Jakar.Database;
using Jakar.SqlBuilder;
using Jakar.SqlBuilder.Tests.Execution;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary>
///     <see cref="SqlLayout.Readable"/> and <see cref="SqlFormatter"/>: clauses start lines, multi-item lists go one item per line, column lists and subqueries indent. Only whitespace between
///     tokens changes, and formatting is idempotent.
/// </summary>
[TestFixture]
public sealed class LayoutTests
{
    private static SqlRoot Pg => SqlBuilder.PostgreSQLWith(SqlBuilderOptions.Readable);


    // ---- the layout ----

    [Test] public void Create_Table_Lists_One_Column_Per_Line()
        => Assert.That(Pg.CreateTable<RoleRecord>().BuildSql(), Is.EqualTo("""
                                                                         CREATE TABLE roles (
                                                                             id uuid DEFAULT gen_random_uuid() NOT NULL PRIMARY KEY,
                                                                             user_id uuid NOT NULL REFERENCES users (id),
                                                                             date_created timestamptz NOT NULL,
                                                                             last_modified timestamptz,
                                                                             concurrency_stamp text NOT NULL,
                                                                             name_of_role text NOT NULL UNIQUE,
                                                                             normalized_name text NOT NULL UNIQUE,
                                                                             rights text NOT NULL,
                                                                             additional_data json
                                                                         );
                                                                         """.ReplaceLineEndings("\n")));

    [Test] public void Select_Puts_Each_Clause_On_A_Line()
        => Assert.That(Pg.Select("id", "name").From("users").Where("active").EqualTo(true).And("age").GreaterThan(18).OrderBy("id").Limit(5).BuildSql(), Is.EqualTo("""
                                                                                                                                                         SELECT
                                                                                                                                                             id,
                                                                                                                                                             name
                                                                                                                                                         FROM users
                                                                                                                                                         WHERE active = TRUE
                                                                                                                                                             AND age > 18
                                                                                                                                                         ORDER BY id
                                                                                                                                                         LIMIT 5;
                                                                                                                                                         """.ReplaceLineEndings("\n")));

    [Test] public void A_Single_Item_Stays_On_The_Clause_Line()
        => Assert.That(Pg.Select("id").From("users").Where("id").EqualTo(1).BuildSql(), Is.EqualTo("SELECT id\nFROM users\nWHERE id = 1;"));

    [Test] public void Insert_Lists_Columns_And_Values()
        => Assert.That(Pg.Insert().Into("users", "name", "email").ValuesParams("Ada", "ada@x.io").Returning("id").BuildSql(), Is.EqualTo("""
                                                                                                                                       INSERT INTO users (
                                                                                                                                           name,
                                                                                                                                           email
                                                                                                                                       )
                                                                                                                                       VALUES (
                                                                                                                                           $1,
                                                                                                                                           $2
                                                                                                                                       )
                                                                                                                                       RETURNING id;
                                                                                                                                       """.ReplaceLineEndings("\n")));

    [Test] public void Insert_Puts_Each_Row_On_A_Line()
        => Assert.That(Pg.Insert().Into("users", "name").Values(SqlValue.Param("Ada")).Values(SqlValue.Param("Bob")).BuildSql(), Is.EqualTo("INSERT INTO users (name)\nVALUES\n    ($1),\n    ($2);"));

    [Test] public void Update_Lists_Assignments()
        => Assert.That(Pg.Update("t").Set("a", 1).Set("b", 2).Where("id").EqualTo(1).BuildSql(), Is.EqualTo("UPDATE t\nSET\n    a = 1,\n    b = 2\nWHERE id = 1;"));

    [Test] public void Subqueries_And_Ctes_Indent()
        => Assert.That(SqlBuilder.MySqlWith(SqlBuilderOptions.Readable).With("a").As(q => q.Select("id").From("t")).DeleteFrom("u").Where("id").In(q => q.Select("id").From("a")).BuildSql(), Is.EqualTo("""
                                                                                                                                                                                                  WITH `a` AS (
                                                                                                                                                                                                      SELECT `id`
                                                                                                                                                                                                      FROM `t`
                                                                                                                                                                                                  )
                                                                                                                                                                                                  DELETE FROM `u`
                                                                                                                                                                                                  WHERE `id` IN (
                                                                                                                                                                                                      SELECT `id`
                                                                                                                                                                                                      FROM `a`
                                                                                                                                                                                                  );
                                                                                                                                                                                                  """.ReplaceLineEndings("\n")));

    [Test] public void Upsert_Keeps_Do_Update_Set_Together()
        => Assert.That(Pg.Insert().Into("t", "id", "n").Values(SqlValue.Of(1), SqlValue.Inline("a")).OnConflict("id").DoUpdateSet("n", fromExcluded: true).Set("c", SqlValue.Of(2)).BuildSql(), Is.EqualTo("""
                                                                                                                                                                                                         INSERT INTO t (
                                                                                                                                                                                                             id,
                                                                                                                                                                                                             n
                                                                                                                                                                                                         )
                                                                                                                                                                                                         VALUES (
                                                                                                                                                                                                             1,
                                                                                                                                                                                                             'a'
                                                                                                                                                                                                         )
                                                                                                                                                                                                         ON CONFLICT (id)
                                                                                                                                                                                                         DO UPDATE SET
                                                                                                                                                                                                             n = EXCLUDED.n,
                                                                                                                                                                                                             c = 2;
                                                                                                                                                                                                         """.ReplaceLineEndings("\n")));

    [Test] public void Between_And_Case_Keep_Their_And_Inline()
        => Assert.That(SqlFormatter.Format("SELECT * FROM t WHERE a BETWEEN 1 AND 2 AND CASE WHEN b AND c THEN 1 ELSE 0 END = 1 OR d = 3;"),
                       Is.EqualTo("SELECT *\nFROM t\nWHERE a BETWEEN 1 AND 2\n    AND CASE WHEN b AND c THEN 1 ELSE 0 END = 1\n    OR d = 3;"));

    [Test] public void Calls_In_Lists_And_Groups_Stay_Inline()
        => Assert.That(SqlFormatter.Format("SELECT COUNT(*) FROM t WHERE id IN (1, 2) AND (a = 1 OR b = 2) AND EXTRACT(YEAR FROM d) = 2026;"),
                       Is.EqualTo("SELECT COUNT(*)\nFROM t\nWHERE id IN (1, 2)\n    AND (a = 1 OR b = 2)\n    AND EXTRACT(YEAR FROM d) = 2026;"));


    // ---- only whitespace between tokens changes ----

    [Test] public void Literals_Are_Copied_As_They_Are()
    {
        const string TEXT = "a, b  FROM c\n  WHERE d";
        string       sql  = Pg.Select("id").From("t").Where("name").EqualTo(SqlValue.Inline(TEXT)).BuildSql();

        Assert.Multiple(() =>
                        {
                            Assert.That(sql, Is.EqualTo("SELECT id\nFROM t\nWHERE name = 'a, b  FROM c\n  WHERE d';"));
                            Assert.That(SqlFormatter.Format("SELECT \"a, b FROM\", [c, d], `e, f` FROM t WHERE x = 'it''s, FROM' AND y = E'\\' , FROM';"),
                                        Is.EqualTo("SELECT\n    \"a, b FROM\",\n    [c, d],\n    `e, f`\nFROM t\nWHERE x = 'it''s, FROM'\n    AND y = E'\\' , FROM';"));
                            Assert.That(SqlFormatter.Format("SELECT 'a\\' , b' FROM t;", SqlDialectKind.MySql), Is.EqualTo("SELECT 'a\\' , b'\nFROM t;"), "MySQL strings escape with a backslash");
                        });
    }

    [Test] public void Comments_And_Dollar_Bodies_Are_Copied_As_They_Are()
        => Assert.Multiple(() =>
                           {
                               Assert.That(SqlFormatter.Format("SELECT a, -- one, FROM\nb FROM t /* WHERE, x */ WHERE c = 1"), Is.EqualTo("SELECT\n    a, -- one, FROM\n    b\nFROM t /* WHERE, x */\nWHERE c = 1"));
                               Assert.That(SqlFormatter.Format("SELECT $$ a, b FROM c $$, $f$ x WHERE y $f$, $1 FROM t"),        Is.EqualTo("SELECT\n    $$ a, b FROM c $$,\n    $f$ x WHERE y $f$,\n    $1\nFROM t"));
                           });

    [Test] public void Write_To_Streams_The_Readable_Form()
    {
        ArrayBufferWriter<char> destination = new();
        SqlParameterSet         parameters  = Pg.Select("id").From("t").Where("a").EqualTo(SqlValue.Param(1)).WriteTo(destination);

        Assert.Multiple(() =>
                        {
                            Assert.That(destination.WrittenSpan.ToString(), Is.EqualTo("SELECT id\nFROM t\nWHERE a = $1;"));
                            Assert.That(parameters.Count,                   Is.EqualTo(1));
                        });
    }

    [Test] public void Readable_Is_The_Default()
        => Assert.Multiple(() =>
                           {
                               Assert.That(SqlBuilderOptions.Default.Layout,                                                   Is.EqualTo(SqlLayout.Readable));
                               Assert.That(SqlBuilder.PostgreSQL.Select("id").From("users").Where("id").EqualTo(1).BuildSql(), Is.EqualTo("SELECT id\nFROM users\nWHERE id = 1;"));
                           });

    [Test] public void Compact_Writes_One_Line()
        => Assert.That(SqlBuilder.PostgreSQLWith(SqlBuilderOptions.Compact).Select("id", "name").From("users").Where("id").EqualTo(1).BuildSql(), Is.EqualTo("SELECT id, name FROM users WHERE id = 1;"));


    /// <summary> Every pattern, record table and fuzzed chain, in every dialect: the readable form has the same tokens, and formatting it again changes nothing. </summary>
    [TestCase(SqlDialectKind.PostgreSql)]
    [TestCase(SqlDialectKind.SqlServer)]
    [TestCase(SqlDialectKind.Sqlite)]
    [TestCase(SqlDialectKind.MySql)]
    [TestCase(SqlDialectKind.Oracle)]
    [TestCase(SqlDialectKind.Firebird)]
    public void Every_Statement_Keeps_Its_Tokens_And_Formats_Idempotently( SqlDialectKind dialect )
    {
        List<string> failures = [];
        int          checkedCount = 0;

        foreach ( string sql in Statements(dialect) )
        {
            checkedCount++;
            string readable = SqlFormatter.Format(sql, dialect);

            if ( Squeeze(readable) != Squeeze(sql) ) { failures.Add($"tokens changed:\n{sql}\n=>\n{readable}"); }
            else if ( SqlFormatter.Format(readable, dialect) != readable ) { failures.Add($"not idempotent:\n{readable}"); }
            else if ( readable.Split('\n').Any(static line => line.Length > 0 && char.IsWhiteSpace(line[^1])) ) { failures.Add($"trailing space:\n{readable}"); }
        }

        Assert.Multiple(() =>
                        {
                            Assert.That(checkedCount, Is.GreaterThan(300));
                            Assert.That(failures,     Is.Empty, string.Join("\n\n", failures.Take(5)));
                        });
    }

    private static IEnumerable<string> Statements( SqlDialectKind dialect )
    {
        foreach ( (string _, SqlResult create, string? _) in RecordTables.All(dialect) ) { yield return create.Sql; }

        for ( int seed = 0; seed < FuzzChains.COUNT; seed++ ) { yield return FuzzChains.Build(seed, dialect).Sql; }

        foreach ( SqlPattern pattern in SqlPatterns.All().Concat(SpecPatterns.All()) )
        {
            if ( pattern.RejectedBy?.Invoke(dialect) is not null ) { continue; }

            yield return pattern.Build(dialect).Sql;
        }
    }

    /// <summary> The text with all whitespace removed: two layouts of the same tokens squeeze to the same string (literals are pinned separately). </summary>
    private static string Squeeze( string sql ) => new(sql.Where(static c => !char.IsWhiteSpace(c)).ToArray());
}
