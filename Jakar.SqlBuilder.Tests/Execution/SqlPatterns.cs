// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests.Execution;


public enum PatternKind
{
    /// <summary> Returns rows (SELECT, or a mutation with RETURNING / OUTPUT). </summary>
    Query,

    /// <summary> INSERT / UPDATE / DELETE: the affected-row count is checked, then the transaction is rolled back. </summary>
    Mutation,

    /// <summary> A mutation that returns rows (RETURNING / OUTPUT), rolled back. </summary>
    MutationQuery,

    /// <summary> DDL: run outside a transaction (MySQL, Oracle and Firebird commit DDL implicitly) between its setup and cleanup statements; success is the engine accepting it. </summary>
    Ddl
}



/// <summary> One builder pattern: how to build it, what the engine must return, and the dialects that must reject it at build time. </summary>
/// <param name="RunsOn"> The dialects whose engine can run it (e.g. a function only some engines have); the others are skipped, not rejected. </param>
/// <param name="RowsFor"> A per-dialect override of <paramref name="ExpectedRows"/> (e.g. MySQL counts an upsert's update as two affected rows). </param>
/// <param name="Setup"> Statements run before it (DDL patterns). </param>
/// <param name="Cleanup"> Statements run after it, whatever happened (DDL patterns); failures are ignored. </param>
public sealed record SqlPattern( string                                        Name,
                                 PatternKind                                   Kind,
                                 Func<SqlDialectKind, SqlResult>               Build,
                                 int                                           ExpectedRows,
                                 Func<SqlDialectKind, SqlBuildError?>?         RejectedBy = null,
                                 Func<IReadOnlyList<object?[]>, string?>?      Check      = null,
                                 Func<SqlDialectKind, bool>?                   RunsOn     = null,
                                 Func<SqlDialectKind, int?>?                   RowsFor    = null,
                                 Func<SqlDialectKind, IEnumerable<SqlResult>>? Setup      = null,
                                 Func<SqlDialectKind, IEnumerable<SqlResult>>? Cleanup    = null )
{
    public int Rows( SqlDialectKind dialect ) => RowsFor?.Invoke(dialect) ?? ExpectedRows;

    public override string ToString() => Name;
}



/// <summary>
///     Every builder entry point and option, against the shared schema (<see cref="TestData"/>). <see cref="DialectExecutionTestsBase"/> runs each on a real engine per dialect;
///     the expected row counts are the same everywhere, so a dialect that renders a pattern differently still has to return the same answer.
/// </summary>
public static class SqlPatterns
{
    private static SqlRoot For( SqlDialectKind dialect ) => SqlBuilder.For(dialect);

    private static SqlBuildError? Only( SqlDialectKind dialect, Func<SqlDialectKind, bool> supported ) => supported(dialect)
                                                                                                              ? null
                                                                                                              : SqlBuildError.UnsupportedFeature;

    private static Func<IReadOnlyList<object?[]>, string?> FirstValue( object expected ) => rows => rows.Count > 0 && Equal(rows[0][0], expected)
                                                                                                        ? null
                                                                                                        : $"first value was '{( rows.Count > 0 ? rows[0][0] : "<no rows>" )}', expected '{expected}'";
    private static bool Equal( object? actual, object expected ) => expected switch
                                                                    {
                                                                        long number => actual is not null and not DBNull && Convert.ToInt64(actual) == number,
                                                                        string text => string.Equals(actual?.ToString(), text, StringComparison.Ordinal),
                                                                        _           => Equals(actual, expected)
                                                                    };


    public static IEnumerable<TestCaseData> Cases()
    {
        foreach ( SqlPattern pattern in All() ) { yield return new TestCaseData(pattern).SetName(pattern.Name); }

        foreach ( SqlPattern pattern in SpecPatterns.All() ) { yield return new TestCaseData(pattern).SetName(pattern.Name); }
    }


    public static IEnumerable<SqlPattern> All()
    {
        // ---- projection ----
        yield return new SqlPattern("select_columns",          PatternKind.Query, d => For(d).Select("id", "name").From("users").Build(),                        4);
        yield return new SqlPattern("select_star",             PatternKind.Query, d => For(d).Select("*").From("users").Build(),                                 4);
        yield return new SqlPattern("select_distinct",         PatternKind.Query, d => For(d).SelectDistinct("active").From("users").Build(),                    2);
        yield return new SqlPattern("select_add_column",       PatternKind.Query, d => For(d).Select("id").Column("name").Column("email").From("users").Build(), 4);
        yield return new SqlPattern("select_column_alias",     PatternKind.Query, d => For(d).Select("name").As("n").From("users").OrderBy("id").Build(),        4, Check: FirstValue("Ada"));
        yield return new SqlPattern("select_table_alias",      PatternKind.Query, d => For(d).Select("u.name").From("users", "u").OrderBy("u.id").Build(),       4, Check: FirstValue("Ada"));
        yield return new SqlPattern("select_sourceless_count", PatternKind.Query, d => For(d).Select().Count("*").Build(),                                       1, Check: FirstValue(1L));

        // ---- aggregates ----
        yield return new SqlPattern("count_star",      PatternKind.Query, d => For(d).Select().Count("*", "n").From("users").Build(),                             1, Check: FirstValue(4L));
        yield return new SqlPattern("count_column",    PatternKind.Query, d => For(d).Select().Count("email").From("users").Build(),                              1, Check: FirstValue(3L));
        yield return new SqlPattern("sum_min_max_avg", PatternKind.Query, d => For(d).Select().Sum("age").Min("age").Max("age").Avg("age").From("users").Build(), 1, Check: FirstValue(158L));
        yield return new SqlPattern("aggregate_alias", PatternKind.Query, d => For(d).Select().Max("age", "oldest").From("users").Build(),                        1, Check: FirstValue(65L));

        // ---- joins ----
        yield return new SqlPattern("join_inner",           PatternKind.Query, d => For(d).Select("u.name", "o.total").From("users", "u").InnerJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Build(),                                                                                 3);
        yield return new SqlPattern("join_left",            PatternKind.Query, d => For(d).Select("u.name", "o.total").From("users", "u").LeftJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Build(),                                                                                  5);
        yield return new SqlPattern("join_right",           PatternKind.Query, d => For(d).Select("u.name", "o.total").From("users", "u").RightJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Build(),                                                                                 4);
        yield return new SqlPattern("join_full",            PatternKind.Query, d => For(d).Select("u.name", "o.total").From("users", "u").FullJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Build(),                                                                                  6, d => Only(d, SqlDialects.SupportsFullOuterJoin));
        yield return new SqlPattern("join_cross",           PatternKind.Query, d => For(d).Select("u.id",   "o.id").From("users", "u").CrossJoin("orders", "o").Build(),                                                                                                                          16);
        yield return new SqlPattern("join_on_inline_value", PatternKind.Query, d => For(d).Select("u.id").From("users", "u").InnerJoin("orders", "o").On("o.user_id").EqualTo(1).Build(),                                                                                                         8);
        yield return new SqlPattern("join_on_param",        PatternKind.Query, d => For(d).Select("u.id").From("users", "u").InnerJoin("orders", "o").On("o.user_id").EqualTo((object)3).Build(),                                                                                                 4);
        yield return new SqlPattern("join_chain",           PatternKind.Query, d => For(d).Select("u.name").From("users", "u").InnerJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").LeftJoin("orders", "o2").On("o2.id").EqualToColumn("o.id").Where("o.total").GreaterThan(5).Build(), 2);

        // ---- WHERE ----
        yield return new SqlPattern("where_equal_inline",           PatternKind.Query, d => For(d).Select("*").From("users").Where("id").EqualTo(1).Build(),                                                                                                                                                                                      1);
        yield return new SqlPattern("where_equal_param",            PatternKind.Query, d => For(d).Select("*").From("users").Where("name").EqualTo((object)"Ada").Build(),                                                                                                                                                                        1);
        yield return new SqlPattern("where_equal_value_param",      PatternKind.Query, d => For(d).Select("*").From("users").Where("name").EqualTo(SqlValue.Param("Bob")).Build(),                                                                                                                                                                1);
        yield return new SqlPattern("where_equal_inline_string",    PatternKind.Query, d => For(d).Select("*").From("users").Where("name").EqualTo(SqlValue.Inline("O'Brien")).Build(),                                                                                                                                                           1);
        yield return new SqlPattern("where_equal_true",             PatternKind.Query, d => For(d).Select("*").From("users").Where("active").EqualTo(true).Build(),                                                                                                                                                                               3);
        yield return new SqlPattern("where_equal_false",            PatternKind.Query, d => For(d).Select("*").From("users").Where("active").EqualTo(false).Build(),                                                                                                                                                                              1);
        yield return new SqlPattern("where_not_equal",              PatternKind.Query, d => For(d).Select("*").From("users").Where("id").NotEqualTo(1).Build(),                                                                                                                                                                                   3);
        yield return new SqlPattern("where_not_equal_param",        PatternKind.Query, d => For(d).Select("*").From("users").Where("name").NotEqualTo((object)"Ada").Build(),                                                                                                                                                                     3);
        yield return new SqlPattern("where_greater",                PatternKind.Query, d => For(d).Select("*").From("users").Where("age").GreaterThan(30).Build(),                                                                                                                                                                                3);
        yield return new SqlPattern("where_greater_param",          PatternKind.Query, d => For(d).Select("*").From("users").Where("age").GreaterThan((object)40).Build(),                                                                                                                                                                        1);
        yield return new SqlPattern("where_greater_or_equal",       PatternKind.Query, d => For(d).Select("*").From("users").Where("age").GreaterOrEqual(40).Build(),                                                                                                                                                                             2);
        yield return new SqlPattern("where_greater_or_equal_param", PatternKind.Query, d => For(d).Select("*").From("users").Where("age").GreaterOrEqual((object)36).Build(),                                                                                                                                                                     3);
        yield return new SqlPattern("where_less",                   PatternKind.Query, d => For(d).Select("*").From("users").Where("age").LessThan(18).Build(),                                                                                                                                                                                   1);
        yield return new SqlPattern("where_less_param",             PatternKind.Query, d => For(d).Select("*").From("users").Where("age").LessThan((object)37).Build(),                                                                                                                                                                           2);
        yield return new SqlPattern("where_less_or_equal",          PatternKind.Query, d => For(d).Select("*").From("users").Where("age").LessOrEqual(36).Build(),                                                                                                                                                                                2);
        yield return new SqlPattern("where_less_or_equal_param",    PatternKind.Query, d => For(d).Select("*").From("users").Where("age").LessOrEqual((object)17).Build(),                                                                                                                                                                        1);
        yield return new SqlPattern("where_value_comparisons",      PatternKind.Query, d => For(d).Select("*").From("users").Where("age").GreaterThan(SqlValue.Of(16)).And("age").LessThan(SqlValue.Param(66)).And("age").NotEqualTo(SqlValue.Of(40)).And("age").GreaterOrEqual(SqlValue.Of(17)).And("age").LessOrEqual(SqlValue.Of(65)).Build(), 3);
        yield return new SqlPattern("where_is_null",                PatternKind.Query, d => For(d).Select("*").From("users").Where("email").IsNull().Build(),                                                                                                                                                                                     1);
        yield return new SqlPattern("where_is_not_null",            PatternKind.Query, d => For(d).Select("*").From("users").Where("email").IsNotNull().Build(),                                                                                                                                                                                  3);
        yield return new SqlPattern("where_like_inline",            PatternKind.Query, d => For(d).Select("*").From("users").Where("name").Like("A%").Build(),                                                                                                                                                                                    1);
        yield return new SqlPattern("where_like_escaped_quote",     PatternKind.Query, d => For(d).Select("*").From("users").Where("name").Like("O'%").Build(),                                                                                                                                                                                   1);
        yield return new SqlPattern("where_like_param",             PatternKind.Query, d => For(d).Select("*").From("users").Where("name").Like(SqlValue.Param("B%")).Build(),                                                                                                                                                                    1);
        yield return new SqlPattern("where_in",                     PatternKind.Query, d => For(d).Select("*").From("users").Where("id").In(1, 3).Build(),                                                                                                                                                                                        2);
        yield return new SqlPattern("where_in_params",              PatternKind.Query, d => For(d).Select("*").From("users").Where("name").InParams("Ada", "Cy", "Nobody").Build(),                                                                                                                                                               2);
        yield return new SqlPattern("where_in_strings",             PatternKind.Query, d => For(d).Select("*").From("users").Where("name").In("Ada", "O'Brien", "Nobody").Build(),                                                                                                                                                                2);
        yield return new SqlPattern("where_not_in_strings",         PatternKind.Query, d => For(d).Select("*").From("users").Where("name").NotIn("Ada", "Bob").Build(),                                                                                                                                                                           2);
        yield return new SqlPattern("where_in_doubles",             PatternKind.Query, d => For(d).Select("*").From("users").Where("age").In(36.0, 40.5, 65d).Build(),                                                                                                                                                                            2);
        yield return new SqlPattern("where_in_decimals",            PatternKind.Query, d => For(d).Select("*").From("users").Where("age").In(17m, 40m).Build(),                                                                                                                                                                                   2);
        yield return new SqlPattern("where_not_in_unsigned",        PatternKind.Query, d => For(d).Select("*").From("users").Where("id").NotIn(1UL, 2UL).Build(),                                                                                                                                                                                 2);
        yield return new SqlPattern("where_between",                PatternKind.Query, d => For(d).Select("*").From("users").Where("age").Between(18, 40).Build(),                                                                                                                                                                                2);
        yield return new SqlPattern("where_and",                    PatternKind.Query, d => For(d).Select("*").From("users").Where("active").EqualTo(true).And("age").GreaterThan(50).Build(),                                                                                                                                                    1);
        yield return new SqlPattern("where_or",                     PatternKind.Query, d => For(d).Select("*").From("users").Where("age").LessThan(18).Or("age").GreaterThan(60).Build(),                                                                                                                                                         2);
        yield return new SqlPattern("where_equal_to_column",        PatternKind.Query, d => For(d).Select("u.id").From("users", "u").Where("u.id").EqualToColumn("u.id").Build(),                                                                                                                                                                 4);

        // ---- GROUP BY / HAVING ----
        yield return new SqlPattern("group_by",             PatternKind.Query, d => For(d).Select("user_id").Count("*", "n").From("orders").GroupBy("user_id").Build(),                              3);
        yield return new SqlPattern("group_by_multiple",    PatternKind.Query, d => For(d).Select("active", "age").From("users").GroupBy("active", "age").Build(),                                   4);
        yield return new SqlPattern("group_by_after_where", PatternKind.Query, d => For(d).Select("user_id").From("orders").Where("total").GreaterThan(5).GroupBy("user_id").Build(),                2);
        yield return new SqlPattern("having_count",         PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").GreaterThan(1).Build(),           1);
        yield return new SqlPattern("having_sum_param",     PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Sum("total").GreaterThan((object)6).Build(), 2);
        yield return new SqlPattern("having_comparisons",   PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("id").LessOrEqual(1).Build(),          2);
        yield return new SqlPattern("having_column",        PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Column("user_id").EqualTo(1).Build(),        1);

        // ---- ORDER BY / paging ----
        yield return new SqlPattern("order_by",             PatternKind.Query, d => For(d).Select("name").From("users").OrderBy("age").Build(),                                               4, Check: FirstValue("Bob"));
        yield return new SqlPattern("order_by_asc",         PatternKind.Query, d => For(d).Select("name").From("users").OrderBy("age").Asc().Build(),                                         4, Check: FirstValue("Bob"));
        yield return new SqlPattern("order_by_desc",        PatternKind.Query, d => For(d).Select("name").From("users").OrderByDesc("age").Build(),                                           4, Check: FirstValue("Cy"));
        yield return new SqlPattern("order_by_desc_call",   PatternKind.Query, d => For(d).Select("name").From("users").OrderBy("age").Desc().Build(),                                        4, Check: FirstValue("Cy"));
        yield return new SqlPattern("order_then_by",        PatternKind.Query, d => For(d).Select("name").From("users").OrderByDesc("active").ThenBy("age").Build(),                          4, Check: FirstValue("Ada"));
        yield return new SqlPattern("order_then_by_desc",   PatternKind.Query, d => For(d).Select("name").From("users").OrderBy("active").ThenByDesc("age").Build(),                          4, Check: FirstValue("Bob"));
        yield return new SqlPattern("order_by_after_where", PatternKind.Query, d => For(d).Select("name").From("users").Where("active").EqualTo(true).OrderByDesc("age").Build(),             3, Check: FirstValue("Cy"));
        yield return new SqlPattern("order_by_after_group", PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").OrderByDesc("user_id").Build(),                3, Check: FirstValue(99L));
        yield return new SqlPattern("limit",                PatternKind.Query, d => For(d).Select("id").From("users").OrderBy("id").Limit(2).Build(),                                         2, Check: FirstValue(1L));
        yield return new SqlPattern("offset",               PatternKind.Query, d => For(d).Select("id").From("users").OrderBy("id").Offset(1).Build(),                                        3, Check: FirstValue(2L));
        yield return new SqlPattern("limit_offset",         PatternKind.Query, d => For(d).Select("id").From("users").OrderBy("id").Limit(2).Offset(1).Build(),                               2, Check: FirstValue(2L));
        yield return new SqlPattern("offset_fetch_next",    PatternKind.Query, d => For(d).Select("id").From("users").OrderBy("id").Offset(2).FetchNext(1).Build(),                           1, Check: FirstValue(3L));
        yield return new SqlPattern("fetch_next",           PatternKind.Query, d => For(d).Select("id").From("users").OrderBy("id").FetchNext(3).Build(),                                     3, Check: FirstValue(1L));
        yield return new SqlPattern("limit_after_where",    PatternKind.Query, d => For(d).Select("id").From("users").Where("active").EqualTo(true).OrderBy("id").Limit(1).Offset(1).Build(), 1, Check: FirstValue(3L));
        yield return new SqlPattern("limit_after_group",    PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").OrderBy("user_id").Limit(1).Build(),           1, Check: FirstValue(1L));

        yield return new SqlPattern("limit_without_order",
                                    PatternKind.Query,
                                    d => For(d).Select("id").From("users").Limit(2).Build(),
                                    2,
                                    d => d.RequiresOrderByForOffset()
                                             ? SqlBuildError.MissingRequiredClause
                                             : null);

        yield return new SqlPattern("top",               PatternKind.Query, d => For(d).Select().Top(2).Column("id").From("users").OrderBy("id").Build(), 2, Check: FirstValue(1L));
        yield return new SqlPattern("top_without_order", PatternKind.Query, d => For(d).Select().Top(3).Column("id").From("users").Build(),               3);

        // ---- set operations ----
        yield return new SqlPattern("union",                         PatternKind.Query, d => For(d).Select("name").From("users").Where("active").EqualTo(true).Union().Select("name").From("orders").Build(),                                                                                                                                                         7);
        yield return new SqlPattern("union_all",                     PatternKind.Query, d => For(d).Select("name").From("users").UnionAll().Select("name").From("orders").Build(),                                                                                                                                                                                    8);
        yield return new SqlPattern("union_after_group",             PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").Union().Select("id").From("users").Build(),                                                                                                                                                                   5);
        yield return new SqlPattern("union_aliases_are_per_operand", PatternKind.Query, d => For(d).Select("*").From<User>("u").InnerJoin<Order>("o").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).UnionAll().Select("*").From<User>("x").InnerJoin<Order>("y").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).Build(), 6);

        // ---- typed (ISqlTable) overloads ----
        yield return new SqlPattern("typed_select_where",          PatternKind.Query, d => For(d).Select<User>(nameof(User.Name)).From<User>().Where<User>(nameof(User.Active)).EqualTo(true).Build(), 3);
        yield return new SqlPattern("typed_column_and_or",         PatternKind.Query, d => For(d).Select<User>(nameof(User.ID)).Column<User>(nameof(User.Name)).From<User>().Where<User>(nameof(User.ID)).EqualTo(1).Or<User>(nameof(User.Email)).IsNull().Build(), 2);
        yield return new SqlPattern("typed_join_alias",            PatternKind.Query, d => For(d).Select("*").From<User>("u").InnerJoin<Order>("o").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).Build(), 3);
        yield return new SqlPattern("typed_join_no_alias",         PatternKind.Query, d => For(d).Select("*").From<User>().LeftJoin<Order>().On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).Where<User>(nameof(User.Active)).EqualTo(true).Build(), 4);
        yield return new SqlPattern("typed_join_right",            PatternKind.Query, d => For(d).Select("*").From<User>("u").RightJoin<Order>("o").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).Build(), 4);
        yield return new SqlPattern("typed_join_full",             PatternKind.Query, d => For(d).Select("*").From<User>("u").FullJoin<Order>("o").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).Build(), 6, d => Only(d, SqlDialects.SupportsFullOuterJoin));
        yield return new SqlPattern("typed_where_equal_to_column", PatternKind.Query, d => For(d).Select("*").From<User>("u").InnerJoin<Order>("o").On<Order>(nameof(Order.UserID)).EqualToColumn<User>(nameof(User.ID)).Where<Order>(nameof(Order.Name)).EqualTo((object)"a").And<User>(nameof(User.ID)).EqualToColumn<Order>(nameof(Order.UserID)).Build(), 1);
        yield return new SqlPattern("typed_aggregates",            PatternKind.Query, d => For(d).Select().Count<User>(nameof(User.ID), "n").Min<User>(nameof(User.ID)).Max<User>(nameof(User.ID)).Sum<User>(nameof(User.ID)).Avg<User>(nameof(User.ID)).From<User>().Build(), 1, Check: FirstValue(4L));
        yield return new SqlPattern("typed_group_having",          PatternKind.Query, d => For(d).Select<Order>(nameof(Order.UserID)).Count<Order>(nameof(Order.ID), "n").From<Order>().GroupBy<Order>(nameof(Order.UserID)).Having().Count<Order>(nameof(Order.ID)).GreaterThan(1).Build(), 1);
        yield return new SqlPattern("typed_having_sum_column",     PatternKind.Query, d => For(d).Select<Order>(nameof(Order.UserID)).From<Order>().GroupBy<Order>(nameof(Order.UserID)).Having().Sum<Order>(nameof(Order.ID)).GreaterOrEqual(3).Build(), 3);
        yield return new SqlPattern("typed_having_column",         PatternKind.Query, d => For(d).Select<Order>(nameof(Order.UserID)).From<Order>().GroupBy<Order>(nameof(Order.UserID)).Having().Column<Order>(nameof(Order.UserID)).LessThan(50).Build(), 2);
        yield return new SqlPattern("typed_order_limit",           PatternKind.Query, d => For(d).Select<User>(nameof(User.Name)).From<User>().OrderByDesc<User>(nameof(User.Name)).Limit(1).Build(), 1, Check: FirstValue("O'Brien"));
        yield return new SqlPattern("typed_order_after_where",     PatternKind.Query, d => For(d).Select<User>(nameof(User.Name)).From<User>().Where<User>(nameof(User.Active)).EqualTo(true).OrderBy<User>(nameof(User.ID)).Build(), 3, Check: FirstValue("Ada"));
        yield return new SqlPattern("typed_group_order",           PatternKind.Query, d => For(d).Select<Order>(nameof(Order.UserID)).From<Order>().GroupBy<Order>(nameof(Order.UserID)).OrderByDesc<Order>(nameof(Order.UserID)).Build(), 3, Check: FirstValue(99L));
        yield return new SqlPattern("typed_where_group",           PatternKind.Query, d => For(d).Select<Order>(nameof(Order.UserID)).From<Order>().Where<Order>(nameof(Order.ID)).LessThan(4).GroupBy<Order>(nameof(Order.UserID)).Build(), 2);

        // ---- INSERT ----
        yield return new SqlPattern("insert_values_mixed",     PatternKind.Mutation,      d => For(d).Insert().Into("users", "name", "email", "active", "age").Values(SqlValue.Param("Dee"), SqlValue.Inline("dee@x.io"), SqlValue.Of(true), SqlValue.Of(30)).Build(), 1);
        yield return new SqlPattern("insert_values_params",    PatternKind.Mutation,      d => For(d).Insert().Into("users", "name", "email").ValuesParams("Eve", "eve@x.io").Build(),                                                                                 1);
        yield return new SqlPattern("insert_null_and_raw",     PatternKind.Mutation,      d => For(d).Insert().Into("users", "name", "email", "age").Values(SqlValue.Inline("Fay"), SqlValue.Null, SqlValue.Raw("40 + 1")).Build(),                                    1);
        yield return new SqlPattern("insert_param_null",       PatternKind.Mutation,      d => For(d).Insert().Into("users", "name", "email").Values(SqlValue.Param("Gil"), SqlValue.Param(null)).Build(),                                                             1);
        yield return new SqlPattern("insert_multi_row",        PatternKind.Mutation,      d => For(d).Insert().Into("users", "name", "age").Values(SqlValue.Param("Hal"), SqlValue.Of(1)).Values(SqlValue.Param("Ivy"), SqlValue.Of(2)).Build(),                       2, d => Only(d, SqlDialects.SupportsMultiRowValues));
        yield return new SqlPattern("insert_typed",            PatternKind.Mutation,      d => For(d).Insert().Into<User>(nameof(User.Name), nameof(User.Email)).ValuesParams("Jo", "jo@x.io").Build(),                                                                1);
        yield return new SqlPattern("insert_returning",        PatternKind.MutationQuery, d => For(d).Insert().Into("users", "name").ValuesParams("Kim").Returning("id", "name").Build(),                                                                              1, d => Only(d, SqlDialects.SupportsReturning));
        yield return new SqlPattern("insert_output",           PatternKind.MutationQuery, d => For(d).Insert().Into("users", "name").ValuesParams("Lee").Output("id", "name").Build(),                                                                                 1, d => Only(d, SqlDialects.SupportsOutputClause));
        yield return new SqlPattern("insert_output_multi_row", PatternKind.MutationQuery, d => For(d).Insert().Into("users", "name").ValuesParams("Max").Values(SqlValue.Param("Ned")).Output("id").Build(),                                                           2, d => Only(d, SqlDialects.SupportsOutputClause));

        // ---- UPDATE ----
        yield return new SqlPattern("update_where",              PatternKind.Mutation,      d => For(d).Update("users").Set("age",   1).Where("id").EqualTo(1).Build(),                                                                                                       1);
        yield return new SqlPattern("update_multi_set",          PatternKind.Mutation,      d => For(d).Update("users").Set("age",   2).SetParam("email", "z@x.io").Set("name", SqlValue.Inline("Bobby")).Where("name").EqualTo((object)"Bob").Build(),                       1);
        yield return new SqlPattern("update_set_null",           PatternKind.Mutation,      d => For(d).Update("users").Set("email", SqlValue.Null).Where("email").IsNotNull().Build(),                                                                                       3);
        yield return new SqlPattern("update_first_set_param",    PatternKind.Mutation,      d => For(d).Update("users").SetParam("email", "p@x.io").Where("id").In(1, 2).Build(),                                                                                             2);
        yield return new SqlPattern("update_first_set_value",    PatternKind.Mutation,      d => For(d).Update("users").Set("age", SqlValue.Param(5)).Where("active").EqualTo(false).Build(),                                                                                 1);
        yield return new SqlPattern("update_and_or",             PatternKind.Mutation,      d => For(d).Update("users").Set("age", 3).Where("active").EqualTo(true).And("age").GreaterThan(50).Or("id").EqualTo(2).Build(),                                                   2);
        yield return new SqlPattern("update_where_comparisons",  PatternKind.Mutation,      d => For(d).Update("users").Set("age", 9).Where("age").GreaterThan((object)17).And("age").LessThan((object)65).And("id").NotEqualTo((object)99).And("id").NotEqualTo(99).Build(), 2);
        yield return new SqlPattern("update_where_predicates",   PatternKind.Mutation,      d => For(d).Update("users").Set("age", 4).Where("name").Like("C%").Or("email").IsNull().Or("age").Between(39, 41).Or("id").EqualToColumn("age").Build(),                          3);
        yield return new SqlPattern("update_no_where",           PatternKind.Mutation,      d => For(d).Update("orders").Set("total", 0).Build(),                                                                                                                             4);
        yield return new SqlPattern("update_typed",              PatternKind.Mutation,      d => For(d).Update<User>().SetParam<User>(nameof(User.Name), "Zed").Where<User>(nameof(User.ID)).EqualTo(2).And<User>(nameof(User.Active)).EqualTo(false).Build(),                1);
        yield return new SqlPattern("update_typed_value",        PatternKind.Mutation,      d => For(d).Update<User>().Set<User>(nameof(User.Email), SqlValue.Null).Where<User>(nameof(User.ID)).LessThan(3).Or<User>(nameof(User.ID)).GreaterThan((object)3).Build(),        3);
        yield return new SqlPattern("update_returning",          PatternKind.MutationQuery, d => For(d).Update("users").Set("age", 7).Where("id").EqualTo(1).Returning("id", "age").Build(),                                                                                  1, d => Only(d, SqlDialects.SupportsReturning));
        yield return new SqlPattern("update_returning_no_where", PatternKind.MutationQuery, d => For(d).Update("orders").Set("total", 1).Returning("id").Build(),                                                                                                             4, d => Only(d, SqlDialects.SupportsReturning));
        yield return new SqlPattern("update_output",             PatternKind.MutationQuery, d => For(d).Update("users").Set("age", 8).Where("active").EqualTo(true).Output("id", "age").Build(),                                                                              3, d => Only(d, SqlDialects.SupportsOutputClause));
        yield return new SqlPattern("update_output_no_where",    PatternKind.MutationQuery, d => For(d).Update("orders").Set("total", 2).Output("id").Build(),                                                                                                                4, d => Only(d, SqlDialects.SupportsOutputClause));

        // ---- DELETE ----
        yield return new SqlPattern("delete_where",              PatternKind.Mutation,      d => For(d).DeleteFrom("orders").Where("id").EqualTo(4).Build(),                                                           1);
        yield return new SqlPattern("delete_in",                 PatternKind.Mutation,      d => For(d).DeleteFrom("orders").Where("id").In(1, 2).Build(),                                                             2);
        yield return new SqlPattern("delete_predicates",         PatternKind.Mutation,      d => For(d).DeleteFrom("users").Where("email").IsNull().Or("name").Like("C%").Or("age").Between(39, 41).Build(),           3);
        yield return new SqlPattern("delete_and",                PatternKind.Mutation,      d => For(d).DeleteFrom("orders").Where("user_id").EqualTo((object)1).And("total").GreaterThan(15).Build(),                 1);
        yield return new SqlPattern("delete_all",                PatternKind.Mutation,      d => For(d).DeleteFrom("orders").Build(),                                                                                  4);
        yield return new SqlPattern("delete_typed",              PatternKind.Mutation,      d => For(d).Delete<Order>().Where<Order>(nameof(Order.ID)).EqualTo(3).Or<Order>(nameof(Order.UserID)).EqualTo(99).Build(), 2);
        yield return new SqlPattern("delete_returning",          PatternKind.MutationQuery, d => For(d).DeleteFrom("orders").Where("id").EqualTo(1).Returning("id", "total").Build(),                                  1, d => Only(d, SqlDialects.SupportsReturning));
        yield return new SqlPattern("delete_returning_no_where", PatternKind.MutationQuery, d => For(d).DeleteFrom("orders").Returning("id").Build(),                                                                  4, d => Only(d, SqlDialects.SupportsReturning));
        yield return new SqlPattern("delete_output",             PatternKind.MutationQuery, d => For(d).DeleteFrom("orders").Where("user_id").EqualTo(1).Output("id").Build(),                                         2, d => Only(d, SqlDialects.SupportsOutputClause));
        yield return new SqlPattern("delete_output_no_where",    PatternKind.MutationQuery, d => For(d).DeleteFrom("orders").Output("id").Build(),                                                                     4, d => Only(d, SqlDialects.SupportsOutputClause));

        // ---- every stage's exits (each stage offers ordering / paging / set operations of its own) ----
        Func<SqlDialectKind, SqlBuildError?> needsOrderBy = static d => d.RequiresOrderByForOffset()
                                                                            ? SqlBuildError.MissingRequiredClause
                                                                            : null;

        yield return new SqlPattern("from_offset",               PatternKind.Query,    d => For(d).Select("id").From("users").Offset(3).Build(), 1, needsOrderBy);
        yield return new SqlPattern("from_fetch_next",           PatternKind.Query,    d => For(d).Select("id").From("users").FetchNext(2).Build(), 2, needsOrderBy);
        yield return new SqlPattern("from_union",                PatternKind.Query,    d => For(d).Select("id").From("users").Union().Select("id").From("orders").Build(), 4);
        yield return new SqlPattern("where_union_all",           PatternKind.Query,    d => For(d).Select("id").From("users").Where("id").EqualTo(1).UnionAll().Select("id").From("orders").Where("id").EqualTo(1).Build(), 2);
        yield return new SqlPattern("where_limit",               PatternKind.Query,    d => For(d).Select("id").From("users").Where("active").EqualTo(true).Limit(2).Build(), 2, needsOrderBy);
        yield return new SqlPattern("where_offset",              PatternKind.Query,    d => For(d).Select("id").From("users").Where("active").EqualTo(true).Offset(2).Build(), 1, needsOrderBy);
        yield return new SqlPattern("where_fetch_next",          PatternKind.Query,    d => For(d).Select("id").From("users").Where("active").EqualTo(true).FetchNext(1).Build(), 1, needsOrderBy);
        yield return new SqlPattern("where_typed_order",         PatternKind.Query,    d => For(d).Select<User>(nameof(User.Name)).From<User>().Where<User>(nameof(User.Active)).EqualTo(true).OrderByDesc<User>(nameof(User.ID)).Build(), 3, Check: FirstValue("O'Brien"));
        yield return new SqlPattern("group_typed_order",         PatternKind.Query,    d => For(d).Select<Order>(nameof(Order.UserID)).From<Order>().GroupBy<Order>(nameof(Order.UserID)).OrderBy<Order>(nameof(Order.UserID)).Build(), 3, Check: FirstValue(1L));
        yield return new SqlPattern("group_limit",               PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Limit(2).Build(), 2, needsOrderBy);
        yield return new SqlPattern("group_offset",              PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Offset(1).Build(), 2, needsOrderBy);
        yield return new SqlPattern("group_fetch_next",          PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").FetchNext(1).Build(), 1, needsOrderBy);
        yield return new SqlPattern("group_union_all",           PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").UnionAll().Select("id").From("users").Build(), 7);
        yield return new SqlPattern("group_typed_multiple",      PatternKind.Query,    d => For(d).Select<User>(nameof(User.Active), nameof(User.Name)).From<User>().GroupBy<User>(nameof(User.Active), nameof(User.Name)).Build(), 4);
        yield return new SqlPattern("having_less_param",         PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").LessThan((object)2).Build(), 2);
        yield return new SqlPattern("having_equal_param",        PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").EqualTo((object)2).Build(), 1);
        yield return new SqlPattern("having_inline_comparisons", PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Sum("total").LessThan(30).Build(), 2);
        yield return new SqlPattern("having_equal_inline",       PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").EqualTo(1).Build(), 2);
        yield return new SqlPattern("having_greater_or_equal",   PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Sum("total").GreaterOrEqual(7).Build(), 2);
        yield return new SqlPattern("having_order_by",           PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").GreaterThan(0).OrderBy("user_id").Build(), 3, Check: FirstValue(1L));
        yield return new SqlPattern("having_order_by_desc",      PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").GreaterThan(0).OrderByDesc("user_id").Build(), 3, Check: FirstValue(99L));
        yield return new SqlPattern("having_offset_fetch",       PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").GreaterThan(0).OrderBy("user_id").Offset(1).FetchNext(1).Build(), 1, Check: FirstValue(3L));
        yield return new SqlPattern("tail_offset",               PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").GreaterThan(0).Offset(1).Build(), 2, needsOrderBy);
        yield return new SqlPattern("tail_fetch_next",           PatternKind.Query,    d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count("*").GreaterThan(0).FetchNext(1).Build(), 1, needsOrderBy);
        yield return new SqlPattern("select_column_star",        PatternKind.Query,    d => For(d).Select().Column("*").From("users").Build(), 4);
        yield return new SqlPattern("select_typed_column_first", PatternKind.Query,    d => For(d).Select().Column<User>(nameof(User.Email)).From<User>().Where<User>(nameof(User.Email)).IsNotNull().Build(), 3);
        yield return new SqlPattern("join_many_tables",          PatternKind.Query,    d => For(d).Select("u.id").From("users", "u").InnerJoin("orders", "o1").On("o1.id").EqualToColumn("u.id").InnerJoin("orders", "o2").On("o2.id").EqualToColumn("u.id").InnerJoin("orders", "o3").On("o3.id").EqualToColumn("u.id").InnerJoin("orders", "o4").On("o4.id").EqualToColumn("u.id").Build(), 4);
        yield return new SqlPattern("where_in_params_many",      PatternKind.Query,    d => For(d).Select("*").From("users").Where("id").InParams(1, 2, 3, 4, 5, 6, 7).Build(), 4);
        yield return new SqlPattern("mutation_where_value",      PatternKind.Mutation, d => For(d).Update("users").Set("age", 50).Where("name").EqualTo(SqlValue.Param("Ada")).Build(), 1);
        yield return new SqlPattern("mutation_where_bool",       PatternKind.Mutation, d => For(d).DeleteFrom("users").Where("active").EqualTo(false).Build(), 1);
        yield return new SqlPattern("mutation_where_value_span", PatternKind.Mutation, d => For(d).DeleteFrom("users").Where("name").EqualTo(SqlValue.Inline("Cy".AsSpan())).Or("age").EqualTo(SqlValue.Raw("17".AsSpan())).Build(), 2);

        // ---- options ----
        yield return new SqlPattern("option_no_terminator", PatternKind.Query, d => SqlBuilder.For(d, new SqlBuilderOptions { AppendStatementTerminator = false }).Select("id").From("users").Build(),                                                                                              4);
        yield return new SqlPattern("option_tiny_buffer",   PatternKind.Query, d => SqlBuilder.For(d, new SqlBuilderOptions { InitialBufferSize         = 1 }).Select("id", "name", "email", "active", "age").From("users").Where("name").Like("%da").Or("name").Like("%ob").OrderBy("id").Build(), 2);
    }
}
