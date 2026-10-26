// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Collections.Generic;
using System.Linq;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests.Execution;


/// <summary>
///     The SPECIFICATION §7 grammar beyond the core patterns: SELECT extensions, CTEs, set operations, locking, predicates and expressions, INSERT / UPDATE / DELETE extensions, MERGE
///     and DDL. Same contract as <see cref="SqlPatterns"/>: every engine returns the same answer, or the dialect rejects the pattern at build time.
/// </summary>
public static class SpecPatterns
{
    private const SqlDialectKind PG   = SqlDialectKind.PostgreSql;
    private const SqlDialectKind MS   = SqlDialectKind.SqlServer;
    private const SqlDialectKind LITE = SqlDialectKind.Sqlite;
    private const SqlDialectKind MY   = SqlDialectKind.MySql;
    private const SqlDialectKind ORA  = SqlDialectKind.Oracle;
    private const SqlDialectKind FB   = SqlDialectKind.Firebird;

    private static SqlRoot For( SqlDialectKind dialect ) => SqlBuilder.For(dialect);

    /// <summary> Rejected (<see cref="SqlBuildError.UnsupportedFeature"/>) everywhere but <paramref name="supported"/>. </summary>
    private static Func<SqlDialectKind, SqlBuildError?> OnlyOn( params SqlDialectKind[] supported ) => d => supported.Contains(d)
                                                                                                                 ? null
                                                                                                                 : SqlBuildError.UnsupportedFeature;

    /// <summary> Rejected (<see cref="SqlBuildError.UnsupportedFeature"/>) by <paramref name="unsupported"/>. </summary>
    private static Func<SqlDialectKind, SqlBuildError?> NotOn( params SqlDialectKind[] unsupported ) => d => unsupported.Contains(d)
                                                                                                                  ? SqlBuildError.UnsupportedFeature
                                                                                                                  : null;

    private static Func<SqlDialectKind, bool> Engines( params SqlDialectKind[] dialects ) => dialects.Contains;

    private static Func<IReadOnlyList<object?[]>, string?> First( object? expected ) => rows =>
                                                                                         {
                                                                                             object? actual = rows.Count > 0 ? rows[0][0] : "<no rows>";
                                                                                             bool same = expected switch
                                                                                                         {
                                                                                                             null        => actual is null or DBNull,
                                                                                                             long number => actual is not null and not DBNull && long.Parse(Convert.ToString(actual, System.Globalization.CultureInfo.InvariantCulture)!.Split('.')[0], System.Globalization.CultureInfo.InvariantCulture) == number,
                                                                                                             string text => string.Equals(actual?.ToString()?.Trim(), text, StringComparison.Ordinal),
                                                                                                             _           => Equals(actual, expected)
                                                                                                         };

                                                                                             return same
                                                                                                        ? null
                                                                                                        : $"first value was '{actual}', expected '{expected ?? "NULL"}'";
                                                                                         };

    private static IEnumerable<SqlResult> Drop( SqlDialectKind d, params string[] tables )
    {
        foreach ( string table in tables ) { yield return For(d).DropTable(table).Build(); }
    }


    public static IEnumerable<SqlPattern> All() => Select().Concat(Grouping()).Concat(Ordering()).Concat(Ctes()).Concat(Locking()).Concat(Predicates()).Concat(Expressions()).Concat(Inserts()).Concat(Updates()).Concat(Deletes()).Concat(Merges()).Concat(Ddl());


    // ---- §7.1 projection and sources ----

    private static IEnumerable<SqlPattern> Select()
    {
        yield return new SqlPattern("spec_distinct_on", PatternKind.Query, d => For(d).SelectDistinctOn(["user_id"], "user_id", "total").From("orders").OrderBy("user_id").ThenByDesc("total").Build(), 3, OnlyOn(PG));
        yield return new SqlPattern("spec_top_percent", PatternKind.Query, d => For(d).Select("id").Top(50, percent: true).From("users").OrderBy("id").Build(), 2, OnlyOn(MS, ORA));
        yield return new SqlPattern("spec_top_with_ties", PatternKind.Query, d => For(d).Select("user_id").Top(1, withTies: true).From("orders").OrderBy("user_id").Build(), 2, OnlyOn(MS, PG, ORA));
        yield return new SqlPattern("spec_scalar_subquery", PatternKind.Query,
                                    d => For(d).Select("id").Column(e => e.Subquery(q => q.Select().Count("*").From("orders").Where("orders.user_id").EqualToColumn("users.id")), "n").From("users").OrderBy("id").Build(), 4,
                                    Check: rows => Convert.ToInt64(rows[0][1]) == 2 ? null : $"Ada has {rows[0][1]} orders, expected 2");
        yield return new SqlPattern("spec_case", PatternKind.Query,
                                    d => For(d).Select().Column(e => e.Case(c => c.When(p => p.Column("age").GreaterThan(30), x => x.String("old")).Else(x => x.String("young"))), "k").From("users").OrderBy("id").Build(), 4,
                                    Check: First("old"));
        yield return new SqlPattern("spec_case_simple", PatternKind.Query,
                                    d => For(d).Select().Column(e => e.Case(x => x.Column("id"), c => c.WhenValue(x => x.Number(1), x => x.String("one")).Else(x => x.String("other"))), "k").From("users").OrderBy("id").Build(), 4,
                                    Check: First("one"));
        yield return new SqlPattern("spec_from_derived", PatternKind.Query, d => For(d).Select("a.id").From(q => q.Select("id").From("users").Where("active").EqualTo(true), "a").Build(), 3);
        yield return new SqlPattern("spec_from_values", PatternKind.Query, d => For(d).Select("v.n").FromValues("v", ["n"], r => r.Row(SqlValue.Of(1)).Row(SqlValue.Of(2)).Row(SqlValue.Of(3))).Build(), 3);
        yield return new SqlPattern("spec_from_function", PatternKind.Query, d => For(d).Select("*").FromFunction("generate_series", "g", e => e.Number(1), e => e.Number(3)).Build(), 3, RunsOn: Engines(PG, MS));
        yield return new SqlPattern("spec_cross_apply", PatternKind.Query, d => For(d).Select("u.id", "o.total").From("users", "u").CrossApply(q => q.Select("total").From("orders").Where("orders.user_id").EqualToColumn("u.id"), "o").Build(), 3, NotOn(LITE));
        yield return new SqlPattern("spec_outer_apply", PatternKind.Query, d => For(d).Select("u.id", "o.total").From("users", "u").OuterApply(q => q.Select("total").From("orders").Where("orders.user_id").EqualToColumn("u.id"), "o").Build(), 5, NotOn(LITE));
        yield return new SqlPattern("spec_comma_join", PatternKind.Query, d => For(d).Select("u.id").From("users", "u").AndFrom("orders", "o").Where("u.id").EqualToColumn("o.user_id").Build(), 3);
        yield return new SqlPattern("spec_natural_join", PatternKind.Query, d => For(d).Select("*").From("users").NaturalJoin("orders").Build(), 0, NotOn(MS));
        yield return new SqlPattern("spec_using_join", PatternKind.Query, d => For(d).Select("id").From("users").InnerJoin("orders").Using("id").Build(), 4, NotOn(MS));
        yield return new SqlPattern("spec_self_join", PatternKind.Query, d => For(d).Select("a.id").From("users", "a").InnerJoin("users", "b").On("a.id").EqualToColumn("b.id").Build(), 4);
        yield return new SqlPattern("spec_join_derived", PatternKind.Query, d => For(d).Select("u.id").From("users", "u").InnerJoin(q => q.Select("user_id").From("orders"), "o").On("o.user_id").EqualToColumn("u.id").Build(), 3);
        yield return new SqlPattern("spec_table_hints", PatternKind.Query, d => For(d).Select("id").From("users").WithHints("NOLOCK").Build(), 4, OnlyOn(MS));
    }


    // ---- §7.1 grouping, windows, set operations ----

    private static IEnumerable<SqlPattern> Grouping()
    {
        yield return new SqlPattern("spec_rollup", PatternKind.Query, d => For(d).Select("user_id").Sum("total", "s").From("orders").GroupByRollup("user_id").Build(), 4, NotOn(LITE, FB));
        yield return new SqlPattern("spec_cube", PatternKind.Query, d => For(d).Select("user_id").Sum("total", "s").From("orders").GroupByCube("user_id").Build(), 4, OnlyOn(PG, MS, ORA));
        yield return new SqlPattern("spec_grouping_sets", PatternKind.Query, d => For(d).Select("user_id").Sum("total", "s").From("orders").GroupByGroupingSets(["user_id"], []).Build(), 4, OnlyOn(PG, MS, ORA));
        yield return new SqlPattern("spec_group_by_expr", PatternKind.Query, d => For(d).Select().Column(e => e.Modulo(x => x.Column("id"), x => x.Number(2)), "parity").Count("*", "n").From("users").GroupByExpr(e => e.Modulo(x => x.Column("id"), x => x.Number(2))).Build(), 2);
        yield return new SqlPattern("spec_having_lambda", PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having(h => h.Count().GreaterThan(1)).Build(), 1);
        yield return new SqlPattern("spec_having_chain", PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having().Count().GreaterThan(0).And().Sum("total").GreaterThan(6).Build(), 2);
        yield return new SqlPattern("spec_having_group", PatternKind.Query, d => For(d).Select("user_id").From("orders").GroupBy("user_id").Having(h => h.Group(p => p.Expr(e => e.Count()).GreaterThan(1).OrExpr(e => e.Sum(x => x.Column("total"))).LessThan(6))).Build(), 2);

        yield return new SqlPattern("spec_row_number", PatternKind.Query,
                                    d => For(d).Select("id").Column(e => e.RowNumber().Over(w => w.PartitionBy("user_id").OrderByDesc("total")), "rn").From("orders").OrderBy("id").Build(), 4,
                                    Check: rows => Convert.ToInt64(rows[0][1]) == 2 ? null : $"order 1's rank was {rows[0][1]}, expected 2");
        yield return new SqlPattern("spec_rank_functions", PatternKind.Query,
                                    d => For(d).Select("id").Column(e => e.Rank().Over(w => w.OrderBy("user_id")), "r").Column(e => e.DenseRank().Over(w => w.OrderBy("user_id")), "dr").Column(e => e.Ntile(2).Over(w => w.OrderBy("id")), "nt").From("orders").Build(), 4);
        yield return new SqlPattern("spec_lag_lead", PatternKind.Query,
                                    d => For(d).Select("id").Column(e => e.Lag(x => x.Column("total")).Over(w => w.OrderBy("id")), "prev").Column(e => e.Lead(x => x.Column("total"), 1, x => x.Number(0)).Over(w => w.OrderBy("id")), "next").From("orders").OrderBy("id").Build(), 4,
                                    Check: rows => rows[0][1] is null or DBNull ? null : $"the first LAG was {rows[0][1]}, expected NULL");
        yield return new SqlPattern("spec_first_last_value", PatternKind.Query,
                                    d => For(d).Select("id").Column(e => e.FirstValue(x => x.Column("total")).Over(w => w.OrderBy("id").Rows(SqlFrameBound.UnboundedPreceding, SqlFrameBound.UnboundedFollowing)), "f").Column(e => e.LastValue(x => x.Column("total")).Over(w => w.OrderBy("id").Rows(SqlFrameBound.UnboundedPreceding, SqlFrameBound.UnboundedFollowing)), "l").From("orders").Build(), 4);
        yield return new SqlPattern("spec_nth_value", PatternKind.Query, d => For(d).Select("id").Column(e => e.NthValue(x => x.Column("total"), 2).Over(w => w.OrderBy("id").Rows(SqlFrameBound.UnboundedPreceding, SqlFrameBound.UnboundedFollowing)), "n").From("orders").Build(), 4, NotOn(MS));
        yield return new SqlPattern("spec_running_sum_rows", PatternKind.Query,
                                    d => For(d).Select().Column(e => e.Sum(x => x.Column("total")).Over(w => w.OrderBy("id").Rows(SqlFrameBound.UnboundedPreceding, SqlFrameBound.CurrentRow)), "s").From("orders").OrderBy("id").Build(), 4, Check: First(10L));
        yield return new SqlPattern("spec_range_frame", PatternKind.Query, d => For(d).Select().Column(e => e.Sum(x => x.Column("total")).Over(w => w.OrderBy("id").Range(SqlFrameBound.UnboundedPreceding, SqlFrameBound.CurrentRow)), "s").From("orders").Build(), 4);
        yield return new SqlPattern("spec_groups_frame", PatternKind.Query, d => For(d).Select().Column(e => e.Sum(x => x.Column("total")).Over(w => w.OrderBy("user_id").Groups(SqlFrameBound.Preceding(1), SqlFrameBound.CurrentRow)), "s").From("orders").Build(), 4, OnlyOn(PG, LITE, ORA));
        yield return new SqlPattern("spec_named_window", PatternKind.Query, d => For(d).Select("id").Column(e => e.RowNumber().Over("w"), "rn").From("users").Window("w", w => w.OrderBy("id")).Build(), 4);
        yield return new SqlPattern("spec_aggregate_filter", PatternKind.Query, d => For(d).Select().Column(e => e.Count(filter: p => p.Column("active").EqualTo(true)), "n").From("users").Build(), 1, Check: First(3L));
        yield return new SqlPattern("spec_count_distinct", PatternKind.Query, d => For(d).Select().CountDistinct("user_id", "n").From("orders").Build(), 1, Check: First(3L));
        yield return new SqlPattern("spec_string_agg", PatternKind.Query, d => For(d).Select().Column(e => e.StringAgg(x => x.Column("name"), ",", x => x.Column("name")), "s").From("orders").Build(), 1, NotOn(FB), First("a,b,c,d"));
        yield return new SqlPattern("spec_string_agg_unordered", PatternKind.Query, d => For(d).Select().Column(e => e.StringAgg(x => x.Column("name"), ","), "s").From("orders").Where("id").EqualTo(1).Build(), 1, Check: First("a"));
        yield return new SqlPattern("spec_array_agg", PatternKind.Query, d => For(d).Select().Column(e => e.ArrayAgg(x => x.Column("id"), x => x.Column("id")), "a").From("orders").Build(), 1, OnlyOn(PG));

        yield return new SqlPattern("spec_intersect", PatternKind.Query, d => For(d).Select("id").From("users").Intersect().Select("user_id").From("orders").Build(), 2, NotOn(FB));
        yield return new SqlPattern("spec_except", PatternKind.Query, d => For(d).Select("id").From("users").Except().Select("user_id").From("orders").Build(), 2, NotOn(FB));
        yield return new SqlPattern("spec_intersect_all", PatternKind.Query, d => For(d).Select("user_id").From("orders").IntersectAll().Select("id").From("users").Build(), 2, OnlyOn(PG, MY, ORA));
        yield return new SqlPattern("spec_except_all", PatternKind.Query, d => For(d).Select("user_id").From("orders").ExceptAll().Select("id").From("users").Build(), 2, OnlyOn(PG, MY, ORA));
        yield return new SqlPattern("spec_union_order_limit", PatternKind.Query, d => For(d).Select("id").From("users").Union().Select("user_id").From("orders").OrderBy("id").Limit(2).Build(), 2, NotOn(FB), First(1L));
        yield return new SqlPattern("spec_union_order_by_position", PatternKind.Query, d => For(d).Select("id").From("users").Union().Select("user_id").From("orders").OrderBy(e => e.Number(1), true).Limit(2).Build(), 2, Check: First(99L));
    }


    // ---- §7.1 ordering ----

    private static IEnumerable<SqlPattern> Ordering()
    {
        yield return new SqlPattern("spec_nulls_first", PatternKind.Query, d => For(d).Select("email").From("users").OrderBy("email").NullsFirst().Build(), 4, Check: First(null));
        yield return new SqlPattern("spec_nulls_last", PatternKind.Query, d => For(d).Select("email").From("users").OrderByDesc("email").NullsLast().Build(), 4, Check: First("ob@x.io"));
        yield return new SqlPattern("spec_order_by_expr", PatternKind.Query, d => For(d).Select("id").From("orders").OrderBy(e => e.Column("total").Times().Number(-1)).Build(), 4, Check: First(2L));
        yield return new SqlPattern("spec_order_then_by_expr", PatternKind.Query, d => For(d).Select("user_id").From("orders").OrderBy("user_id").ThenBy(e => e.Column("total"), true).Build(), 4, Check: First(1L));
        yield return new SqlPattern("spec_order_asc_desc_calls", PatternKind.Query, d => For(d).Select("id").From("users").OrderBy("active").Desc().ThenBy("id").Asc().Build(), 4);
    }


    // ---- §7.1 CTEs ----

    private static IEnumerable<SqlPattern> Ctes()
    {
        yield return new SqlPattern("spec_cte", PatternKind.Query, d => For(d).With("a").As(q => q.Select("id").From("users").Where("active").EqualTo(true)).Select("id").From("a").Build(), 3);
        yield return new SqlPattern("spec_cte_columns", PatternKind.Query, d => For(d).With("a", "x").As(q => q.Select("id").From("users")).Select("x").From("a").Build(), 4);
        yield return new SqlPattern("spec_cte_multiple", PatternKind.Query,
                                    d => For(d).With("a").As(q => q.Select("id").From("users").Where("active").EqualTo(true)).With("b").As(q => q.Select("user_id").From("orders")).Select("a.id").From("a").InnerJoin("b").On("a.id").EqualToColumn("b.user_id").Build(), 3);
        yield return new SqlPattern("spec_cte_recursive", PatternKind.Query,
                                    d => For(d).WithRecursive("n", "x").As(q => q.Select().Column(e => e.Number(1)).From("users").Where("id").EqualTo(1).UnionAll().Select().Column(e => e.Column("x").Plus().Number(1)).From("n").Where("x").LessThan(5)).Select("x").From("n").Build(), 5);
        yield return new SqlPattern("spec_cte_materialized", PatternKind.Query, d => For(d).With("a").AsMaterialized(q => q.Select("id").From("users")).Select("id").From("a").Build(), 4, OnlyOn(PG, LITE));
        yield return new SqlPattern("spec_cte_not_materialized", PatternKind.Query, d => For(d).With("a").AsNotMaterialized(q => q.Select("id").From("users")).Select("id").From("a").Build(), 4, OnlyOn(PG, LITE));
        yield return new SqlPattern("spec_cte_delete", PatternKind.Mutation, d => For(d).With("a").As(q => q.Select("id").From("users").Where("active").EqualTo(true)).DeleteFrom("orders").Where("user_id").In(q => q.Select("id").From("a")).Build(), 3, NotOn(ORA, FB));
        yield return new SqlPattern("spec_cte_update", PatternKind.Mutation, d => For(d).With("a").As(q => q.Select("id").From("users").Where("active").EqualTo(true)).Update("orders").Set("total", 0).Where("user_id").In(q => q.Select("id").From("a")).Build(), 3, NotOn(ORA, FB));
        yield return new SqlPattern("spec_cte_insert", PatternKind.Mutation,
                                    d => For(d).With("a").As(q => q.Select("id", "name").From("users").Where("active").EqualTo(false)).Insert().Into("orders", "user_id", "name", "total").Select(q => q.Select("id", "name").Column(e => e.Number(0)).From("a")).Build(), 1,
                                    NotOn(MY, ORA, FB));
    }


    // ---- §7.1 locking ----

    private static IEnumerable<SqlPattern> Locking()
    {
        yield return new SqlPattern("spec_for_update", PatternKind.MutationQuery, d => For(d).Select("id").From("users").Where("id").EqualTo(1).ForUpdate().Build(), 1, NotOn(MS, LITE));
        yield return new SqlPattern("spec_for_update_order_limit", PatternKind.MutationQuery, d => For(d).Select("id").From("users").OrderBy("id").Limit(1).ForUpdate().Build(), 1, NotOn(MS, LITE, ORA));
        yield return new SqlPattern("spec_for_share", PatternKind.MutationQuery, d => For(d).Select("id").From("users").Where("id").EqualTo(1).ForShare().Build(), 1, OnlyOn(PG, MY));
        yield return new SqlPattern("spec_skip_locked", PatternKind.MutationQuery, d => For(d).Select("id").From("users").Where("id").EqualTo(1).ForUpdate().SkipLocked().Build(), 1, NotOn(MS, LITE));
        yield return new SqlPattern("spec_nowait", PatternKind.MutationQuery, d => For(d).Select("id").From("users").Where("id").EqualTo(1).ForUpdate().NoWait().Build(), 1, OnlyOn(PG, MY, ORA));
    }


    // ---- §7.6 predicates ----

    private static IEnumerable<SqlPattern> Predicates()
    {
        yield return new SqlPattern("spec_is_distinct_from", PatternKind.Query, d => For(d).Select("id").From("users").Where("email").IsDistinctFrom(SqlValue.Null).Build(), 3);
        yield return new SqlPattern("spec_is_not_distinct_from", PatternKind.Query, d => For(d).Select("id").From("users").Where("email").IsNotDistinctFrom(SqlValue.Null).Build(), 1);
        yield return new SqlPattern("spec_is_distinct_from_param", PatternKind.Query, d => For(d).Select("id").From("users").Where("email").IsDistinctFrom("ada@x.io").Build(), 3);
        yield return new SqlPattern("spec_ilike", PatternKind.Query, d => For(d).Select("id").From("users").Where("name").ILike("ADA").Build(), 1);
        yield return new SqlPattern("spec_not_ilike", PatternKind.Query, d => For(d).Select("id").From("users").Where("name").NotILike("ada").Build(), 3);
        yield return new SqlPattern("spec_glob", PatternKind.Query, d => For(d).Select("id").From("users").Where("name").Glob("A*").Build(), 1, OnlyOn(LITE));
        yield return new SqlPattern("spec_similar_to", PatternKind.Query, d => For(d).Select("id").From("users").Where("name").SimilarTo("A%").Build(), 1, OnlyOn(PG, FB));
        yield return new SqlPattern("spec_not_similar_to", PatternKind.Query, d => For(d).Select("id").From("users").Where("name").NotSimilarTo("A%").Build(), 3, OnlyOn(PG, FB));
        yield return new SqlPattern("spec_any_array", PatternKind.Query, d => For(d).Select("id").From("users").Where("id").Any(new[] { 1, 2 }).Build(), 2, OnlyOn(PG));
        yield return new SqlPattern("spec_like_escape", PatternKind.Query, d => For(d).Select("id").From("users").Where("email").Like("%x.io", '!').Build(), 3);
        yield return new SqlPattern("spec_not_like", PatternKind.Query, d => For(d).Select("id").From("users").Where("name").NotLike("A%").Build(), 3);
        yield return new SqlPattern("spec_in_subquery", PatternKind.Query, d => For(d).Select("id").From("users").Where("id").In(q => q.Select("user_id").From("orders")).Build(), 2);
        yield return new SqlPattern("spec_not_in_subquery", PatternKind.Query, d => For(d).Select("id").From("users").Where("id").NotIn(q => q.Select("user_id").From("orders")).Build(), 2);
        yield return new SqlPattern("spec_not_in", PatternKind.Query, d => For(d).Select("id").From("users").Where("id").NotIn(1, 2).Build(), 2);
        yield return new SqlPattern("spec_not_in_params", PatternKind.Query, d => For(d).Select("id").From("users").Where("id").NotInParams(1, 2).Build(), 2);
        yield return new SqlPattern("spec_in_values", PatternKind.Query, d => For(d).Select("id").From("users").Where("name").InValues(SqlValue.Inline("Ada"), SqlValue.Inline("Cy")).Build(), 2);
        yield return new SqlPattern("spec_between_values", PatternKind.Query, d => For(d).Select("id").From("users").Where("age").Between(SqlValue.Of(30), SqlValue.Of(50)).Build(), 2);
        yield return new SqlPattern("spec_not_between", PatternKind.Query, d => For(d).Select("id").From("users").Where("age").NotBetween(30, 50).Build(), 2);
        yield return new SqlPattern("spec_exists", PatternKind.Query, d => For(d).Select("id").From("users").WhereExists(q => q.Select("id").From("orders").Where("orders.user_id").EqualToColumn("users.id")).Build(), 2);
        yield return new SqlPattern("spec_not_exists", PatternKind.Query, d => For(d).Select("id").From("users").WhereNotExists(q => q.Select("id").From("orders").Where("orders.user_id").EqualToColumn("users.id")).Build(), 2);
        yield return new SqlPattern("spec_and_exists", PatternKind.Query, d => For(d).Select("id").From("users").Where("active").EqualTo(true).AndExists(q => q.Select("id").From("orders").Where("orders.user_id").EqualToColumn("users.id")).Build(), 2);
        yield return new SqlPattern("spec_where_not", PatternKind.Query, d => For(d).Select("id").From("users").WhereNot(p => p.Column("active").EqualTo(true)).Build(), 1);
        yield return new SqlPattern("spec_nested_groups", PatternKind.Query,
                                    d => For(d).Select("id").From("users").Where(g => g.Column("age").GreaterThan(18).And("active").EqualTo(true)).Or(g => g.Column("name").EqualTo(SqlValue.Inline("Bob"))).Build(), 4);
        yield return new SqlPattern("spec_group_not_exists", PatternKind.Query, d => For(d).Select("id").From("users").Where(p => p.NotExists(q => q.Select("id").From("orders").Where("orders.user_id").EqualToColumn("users.id")).And("active").EqualTo(true)).Build(), 1);
        yield return new SqlPattern("spec_where_expression", PatternKind.Query, d => For(d).Select("id").From("users").Where(e => e.Column("age").Plus().Number(1)).GreaterThan(40).Build(), 2);
        yield return new SqlPattern("spec_where_expression_rhs", PatternKind.Query, d => For(d).Select("id").From("orders").Where("total").GreaterThan(e => e.Number(2).Times().Number(4)).Build(), 2);
        yield return new SqlPattern("spec_always", PatternKind.Query, d => For(d).Select("id").From("users").Where(p => p.Always(true)).Build(), 4);
        yield return new SqlPattern("spec_never", PatternKind.Query, d => For(d).Select("id").From("users").Where(p => p.Always(false)).Build(), 0);
        yield return new SqlPattern("spec_dates", PatternKind.Query, d => For(d).Select("id").From("users").Where(e => e.CurrentDate()).GreaterThan(e => e.AddInterval(x => x.CurrentDate(), -1, SqlDatePart.Day)).Build(), 4);
        yield return new SqlPattern("spec_timestamp", PatternKind.Query, d => For(d).Select("id").From("users").Where(e => e.CurrentTimestamp()).GreaterThan(e => e.AddInterval(x => x.CurrentTimestamp(), -2, SqlDatePart.Hour)).Build(), 4);
    }


    // ---- §7.6 expressions ----

    private static IEnumerable<SqlPattern> Expressions()
    {
        yield return new SqlPattern("spec_arithmetic", PatternKind.Query, d => For(d).Select().Column(e => e.Column("total").Times().Number(2).Minus().Number(1), "t").From("orders").OrderBy("id").Build(), 4, Check: First(19L));
        yield return new SqlPattern("spec_modulo_negate", PatternKind.Query, d => For(d).Select().Column(e => e.Negate(x => x.Modulo(y => y.Column("total"), y => y.Number(3))), "m").From("orders").OrderBy("id").Build(), 4, Check: First(-1L));
        yield return new SqlPattern("spec_concat", PatternKind.Query, d => For(d).Select().Column(e => e.Concat(x => x.Column("name"), x => x.String("!")), "c").From("users").OrderBy("id").Build(), 4, Check: First("Ada!"));
        yield return new SqlPattern("spec_coalesce", PatternKind.Query, d => For(d).Select().Column(e => e.Coalesce(x => x.Column("email"), x => x.String("none")), "e").From("users").Where("id").EqualTo(2).Build(), 1, Check: First("none"));
        yield return new SqlPattern("spec_nullif", PatternKind.Query, d => For(d).Select().Column(e => e.NullIf(x => x.Column("age"), x => x.Number(36)), "a").From("users").Where("id").EqualTo(1).Build(), 1, Check: First(null));
        yield return new SqlPattern("spec_cast", PatternKind.Query, d => For(d).Select().Column(e => e.Cast(x => x.Column("age"), SqlType.VarChar(10)), "a").From("users").Where("id").EqualTo(1).Build(), 1, Check: First("36"));
        yield return new SqlPattern("spec_cast_int", PatternKind.Query, d => For(d).Select().Column(e => e.Cast(x => x.String("42"), SqlType.Int), "a").From("users").Where("id").EqualTo(1).Build(), 1, Check: First(42L));
        yield return new SqlPattern("spec_string_functions", PatternKind.Query,
                                    d => For(d).Select().Column(e => e.Length(x => x.Upper(y => y.Trim(z => z.Lower(w => w.Column("name"))))), "l").From("users").Where("id").EqualTo(1).Build(), 1, Check: First(3L));
        yield return new SqlPattern("spec_abs_round", PatternKind.Query, d => For(d).Select().Column(e => e.Abs(x => x.Round(y => y.Number(-2.4m))), "r").From("users").Where("id").EqualTo(1).Build(), 1, Check: First(2L));
        yield return new SqlPattern("spec_param_expression", PatternKind.Query, d => For(d).Select("id").From("orders").Where(e => e.Column("total").Plus().Param(5)).GreaterThan(20).Build(), 1);
        yield return new SqlPattern("spec_group_expression", PatternKind.Query, d => For(d).Select().Column(e => e.Group(x => x.Column("total").Plus().Number(1)).Times().Number(2), "t").From("orders").OrderBy("id").Build(), 4, Check: First(22L));
        yield return new SqlPattern("spec_typed_inline_values", PatternKind.Query,
                                    d => For(d).Select("id").From("users").Where("id").InValues(SqlValue.Of(1m), SqlValue.Of(2.0)).Build(), 2);
    }


    // ---- §7.2 INSERT ----

    private static IEnumerable<SqlPattern> Inserts()
    {
        yield return new SqlPattern("spec_insert_select", PatternKind.Mutation, d => For(d).Insert().Into("orders", "user_id", "name", "total").Select(q => q.Select("id", "name", "age").From("users")).Build(), 4);
        yield return new SqlPattern("spec_insert_default_keyword", PatternKind.Mutation, d => For(d).Insert().Into("users", "name", "email").Values(SqlValue.Inline("D"), SqlValue.Default).Build(), 1, NotOn(LITE));
        yield return new SqlPattern("spec_on_conflict_do_nothing", PatternKind.Mutation, d => For(d).Insert().Into("users", "id", "name").Values(SqlValue.Of(1), SqlValue.Inline("X")).OnConflict("id").DoNothing().Build(), 0,
                                    OnlyOn(PG, LITE, MY), RowsFor: d => d is MY ? 1 : null);
        yield return new SqlPattern("spec_on_conflict_update_excluded", PatternKind.Mutation,
                                    d => For(d).Insert().Into("users", "id", "name").Values(SqlValue.Of(1), SqlValue.Inline("Zed")).OnConflict("id").DoUpdateSet("name", fromExcluded: true).Build(), 1, OnlyOn(PG, LITE, MY), RowsFor: d => d is MY ? 2 : null);
        yield return new SqlPattern("spec_on_conflict_update_value", PatternKind.Mutation,
                                    d => For(d).Insert().Into("users", "id", "name").Values(SqlValue.Of(1), SqlValue.Inline("Zed")).OnConflict("id").DoUpdateSet("age", SqlValue.Of(1)).SetExcluded("name").Build(), 1, OnlyOn(PG, LITE, MY),
                                    RowsFor: d => d is MY ? 2 : null);
        yield return new SqlPattern("spec_on_conflict_update_where", PatternKind.Mutation,
                                    d => For(d).Insert().Into("users", "id", "name").Values(SqlValue.Of(1), SqlValue.Inline("Zed")).OnConflict("id").DoUpdateSet("age", SqlValue.Of(1)).Where(p => p.Column("users.age").GreaterThan(100)).Build(), 0, OnlyOn(PG, LITE));
        yield return new SqlPattern("spec_on_conflict_returning", PatternKind.MutationQuery,
                                    d => For(d).Insert().Into("users", "id", "name").Values(SqlValue.Of(1), SqlValue.Inline("Zed")).OnConflict("id").DoUpdateSet("name", fromExcluded: true).Returning("id").Build(), 1, OnlyOn(PG, LITE));
        yield return new SqlPattern("spec_overriding_system_value", PatternKind.Mutation, d => For(d).Insert().Into("users", "id", "name").OverridingSystemValue().Values(SqlValue.Of(100), SqlValue.Inline("Q")).Build(), 1, OnlyOn(PG));
        yield return new SqlPattern("spec_insert_select_output", PatternKind.MutationQuery, d => For(d).Insert().Into("orders", "user_id", "name", "total").Select(q => q.Select("id", "name", "age").From("users")).Output("id").Build(), 4, OnlyOn(MS));
        yield return new SqlPattern("spec_insert_select_returning", PatternKind.MutationQuery, d => For(d).Insert().Into("orders", "user_id", "name", "total").Select(q => q.Select("id", "name", "age").From("users")).Returning("id").Build(), 4, OnlyOn(PG, LITE, FB));
    }


    // ---- §7.3 UPDATE ----

    private static IEnumerable<SqlPattern> Updates()
    {
        yield return new SqlPattern("spec_update_set_expression", PatternKind.Mutation, d => For(d).Update("orders").Set("total", e => e.Column("total").Plus().Number(1)).Where("id").EqualTo(1).Build(), 1);
        yield return new SqlPattern("spec_update_set_default", PatternKind.Mutation, d => For(d).Update("users").Set("email", SqlValue.Default).Where("id").EqualTo(1).Build(), 1, NotOn(LITE));
        yield return new SqlPattern("spec_update_set_row", PatternKind.Mutation, d => For(d).Update("orders").SetRow(["name", "total"], SqlValue.Inline("z"), SqlValue.Of(1)).Where("id").EqualTo(1).Build(), 1);
        yield return new SqlPattern("spec_update_set_row_subquery", PatternKind.Mutation, d => For(d).Update("orders").SetRow(["name", "total"], q => q.Select("name", "age").From("users").Where("id").EqualTo(1)).Where("id").EqualTo(1).Build(), 1, OnlyOn(PG, LITE));
        yield return new SqlPattern("spec_update_alias", PatternKind.Mutation, d => For(d).Update("orders", "o").Set("total", SqlValue.Of(0)).Where("o.id").EqualTo(1).Build(), 1);
        yield return new SqlPattern("spec_update_from", PatternKind.Mutation,
                                    d => For(d).Update("orders", "o").Set("total", SqlValue.Of(0)).From("users", "u").Where("u.id").EqualToColumn("o.user_id").And("u.active").EqualTo(true).Build(), 3, NotOn(ORA, FB));
        yield return new SqlPattern("spec_update_from_no_alias", PatternKind.Mutation, d => For(d).Update("orders").Set("total", SqlValue.Of(0)).From("users", "u").Where("u.id").EqualToColumn("orders.user_id").Build(), 3, NotOn(ORA, FB));
        yield return new SqlPattern("spec_update_from_two_sources", PatternKind.Mutation,
                                    d => For(d).Update("orders", "o").Set("total", SqlValue.Of(0)).From("users", "u").And("users", "v").Where("u.id").EqualToColumn("o.user_id").And("v.id").EqualToColumn("u.id").Build(), 3, NotOn(ORA, FB));
        yield return new SqlPattern("spec_update_where_lambda", PatternKind.Mutation, d => For(d).Update("orders").Set("total", 0).Where(p => p.Column("id").EqualTo(1).Or("id").EqualTo(2)).Build(), 2);
        yield return new SqlPattern("spec_update_alias_output", PatternKind.MutationQuery, d => For(d).Update("orders", "o").Set("total", SqlValue.Of(0)).Where("o.id").EqualTo(1).Output("id").Build(), 1, OnlyOn(MS));
        yield return new SqlPattern("spec_update_from_returning", PatternKind.MutationQuery, d => For(d).Update("orders", "o").Set("total", SqlValue.Of(0)).From("users", "u").Where("u.id").EqualToColumn("o.user_id").Returning("o.id").Build(), 3, OnlyOn(PG, LITE));
    }


    // ---- §7.4 DELETE ----

    private static IEnumerable<SqlPattern> Deletes()
    {
        yield return new SqlPattern("spec_delete_alias", PatternKind.Mutation, d => For(d).DeleteFrom("orders", "o").Where("o.id").EqualTo(1).Build(), 1);
        yield return new SqlPattern("spec_delete_using", PatternKind.Mutation,
                                    d => For(d).DeleteFrom("orders", "o").Using("users", "u").Where("u.id").EqualToColumn("o.user_id").And("u.active").EqualTo(true).Build(), 3, OnlyOn(PG, MS, MY));
        yield return new SqlPattern("spec_delete_using_no_alias", PatternKind.Mutation,
                                    d => For(d).DeleteFrom("orders").Using("users", "u").Where("u.id").EqualToColumn("orders.user_id").And("u.active").EqualTo(true).Build(), 3, OnlyOn(PG, MS, MY));
        yield return new SqlPattern("spec_delete_where_lambda", PatternKind.Mutation, d => For(d).DeleteFrom("orders").Where(p => p.Column("total").LessThan(8)).Build(), 2);
        yield return new SqlPattern("spec_delete_where_exists", PatternKind.Mutation, d => For(d).DeleteFrom("orders").Where("total").GreaterThan(0).AndNotExists(q => q.Select("id").From("users").Where("users.id").EqualToColumn("orders.user_id")).Build(), 1);
        yield return new SqlPattern("spec_delete_alias_output", PatternKind.MutationQuery, d => For(d).DeleteFrom("orders", "o").Where("o.id").EqualTo(1).Output("id").Build(), 1, OnlyOn(MS));
        yield return new SqlPattern("spec_delete_using_returning", PatternKind.MutationQuery, d => For(d).DeleteFrom("orders", "o").Using("users", "u").Where("u.id").EqualToColumn("o.user_id").Returning("o.id").Build(), 3, OnlyOn(PG));
    }


    // ---- §7.5 MERGE ----

    private static SqlSubquery<GroupByStage> MaxTotals => q => q.Select("user_id").Max("total", "m").From("orders").GroupBy("user_id");

    private static IEnumerable<SqlPattern> Merges()
    {
        Func<SqlDialectKind, SqlBuildError?> merge = OnlyOn(PG, MS, ORA, FB);

        yield return new SqlPattern("spec_merge_update_insert", PatternKind.Mutation,
                                    d => For(d).MergeInto("users", "t").Using(MaxTotals, "s").On(p => p.Column("t.id").EqualToColumn("s.user_id")).WhenMatchedThenUpdate(s => s.Set("age", e => e.Column("s.m"))).WhenNotMatchedThenInsert(["name", "age"], [e => e.String("new"), e => e.Column("s.m")]).Build(), 3,
                                    merge);
        yield return new SqlPattern("spec_merge_table_source", PatternKind.Mutation,
                                    d => For(d).MergeInto("orders", "t").Using("users", "s").On(p => p.Column("t.id").EqualToColumn("s.id")).WhenMatchedThenUpdate(s => s.Set("total", SqlValue.Of(0))).Build(), 4, merge);
        yield return new SqlPattern("spec_merge_conditional", PatternKind.Mutation,
                                    d => For(d).MergeInto("users", "t").Using(MaxTotals, "s").On(p => p.Column("t.id").EqualToColumn("s.user_id")).WhenMatchedThenUpdate(s => s.Set("age", e => e.Column("s.m")).Set("active", SqlValue.Of(true)), p => p.Column("s.m").GreaterThan(15)).WhenNotMatchedThenInsert(["name"], [e => e.String("new")], p => p.Column("s.m").LessThan(0)).Build(), 1,
                                    merge);
        yield return new SqlPattern("spec_merge_delete", PatternKind.Mutation,
                                    d => For(d).MergeInto("users", "t").Using(MaxTotals, "s").On(p => p.Column("t.id").EqualToColumn("s.user_id")).WhenMatchedThenDelete(p => p.Column("s.m").GreaterThan(15)).WhenNotMatchedThenInsert(["name"], [e => e.String("new")]).Build(), 2,
                                    OnlyOn(PG, MS, FB));
        yield return new SqlPattern("spec_merge_not_matched_by_source", PatternKind.Mutation,
                                    d => For(d).MergeInto("users", "t").Using(MaxTotals, "s").On(p => p.Column("t.id").EqualToColumn("s.user_id")).WhenMatchedThenUpdate(s => s.Set("age", SqlValue.Of(1))).WhenNotMatchedBySourceThenDelete().Build(), 4, OnlyOn(MS));
        yield return new SqlPattern("spec_merge_output", PatternKind.MutationQuery,
                                    d => For(d).MergeInto("orders", "t").Using("users", "s").On(p => p.Column("t.id").EqualToColumn("s.id")).WhenMatchedThenUpdate(s => s.Set("total", SqlValue.Of(0))).Output("id").Build(), 4, OnlyOn(MS));
        yield return new SqlPattern("spec_merge_param", PatternKind.Mutation,
                                    d => For(d).MergeInto("orders", "t").Using("users", "s").On(p => p.Column("t.id").EqualToColumn("s.id").And("s.id").EqualTo(1)).WhenMatchedThenUpdate(s => s.SetParam("total", 99)).Build(), 1, merge);
    }


    // ---- §7.5 DDL ----

    private static IEnumerable<SqlPattern> Ddl()
    {
        yield return new SqlPattern("spec_create_table", PatternKind.Ddl,
                                    d => For(d).CreateTable("ddl_full")
                                               .Column("id", SqlType.BigInt, c => c.Identity().PrimaryKey())
                                               .Column("name", SqlType.VarChar(50), c => c.NotNull().Unique())
                                               .Column("n", SqlType.Int, c => c.Default(SqlValue.Of(0)).NotNull().Check(p => p.Column("n").GreaterOrEqual(0)))
                                               .Column("user_id", SqlType.Int, c => c.Nullable().References("users", "id", SqlReferentialAction.Cascade))
                                               .Build(), 0, Cleanup: d => Drop(d, "ddl_full"));
        yield return new SqlPattern("spec_create_table_types", PatternKind.Ddl,
                                    d => For(d).CreateTable("ddl_types")
                                               .Column("b", SqlType.Boolean).Column("s", SqlType.SmallInt).Column("i", SqlType.Int).Column("l", SqlType.BigInt).Column("r", SqlType.Real).Column("f", SqlType.Double)
                                               .Column("m", SqlType.Decimal(10, 2)).Column("c", SqlType.Char(3)).Column("v", SqlType.VarChar(20)).Column("t", SqlType.Text).Column("d", SqlType.Date).Column("tm", SqlType.Time)
                                               .Column("ts", SqlType.Timestamp).Column("tz", SqlType.TimestampTz).Column("g", SqlType.Guid).Column("bl", SqlType.Blob).Column("bn", SqlType.Binary(4)).Column("vb", SqlType.VarBinary(8))
                                               .Build(), 0, Cleanup: d => Drop(d, "ddl_types"));
        yield return new SqlPattern("spec_create_table_json", PatternKind.Ddl, d => For(d).CreateTable("ddl_json").Column("j", SqlType.Json).Build(), 0, Cleanup: d => Drop(d, "ddl_json"), RunsOn: d => d is not FB);
        yield return new SqlPattern("spec_create_table_defaults", PatternKind.Ddl,
                                    d => For(d).CreateTable("ddl_def")
                                               .Column("n", SqlType.Int, c => c.Default(SqlValue.Of(5)).NotNull())
                                               .Column("s", SqlType.VarChar(10), c => c.Default(SqlValue.Inline("x")))
                                               .Column("t", SqlType.Timestamp, c => c.Default(e => e.CurrentTimestamp()))
                                               .Build(), 0, Cleanup: d => Drop(d, "ddl_def"));
        yield return new SqlPattern("spec_create_table_constraints", PatternKind.Ddl,
                                    d => For(d).CreateTable("ddl_con")
                                               .Column("a", SqlType.Int, c => c.NotNull())
                                               .Column("b", SqlType.Int, c => c.NotNull())
                                               .Column("u", SqlType.Int)
                                               .PrimaryKey("a", "b")
                                               .Constraint("ddl_con_u").Unique("u")
                                               .Check(p => p.Column("a").GreaterOrEqual(0))
                                               .Constraint("ddl_con_fk").ForeignKey(["u"], "users", ["id"], SqlReferentialAction.SetNull)
                                               .Build(), 0, Cleanup: d => Drop(d, "ddl_con"));
        yield return new SqlPattern("spec_create_table_if_not_exists", PatternKind.Ddl, d => For(d).CreateTable("ddl_ine", true).Column("a", SqlType.Int).Build(), 0, NotOn(FB),
                                    Setup: d => [For(d).CreateTable("ddl_ine").Column("a", SqlType.Int).Build()], Cleanup: d => Drop(d, "ddl_ine"));
        yield return new SqlPattern("spec_create_table_typed", PatternKind.Ddl, d => For(d).CreateTable<Widget>().Build(), 0, Cleanup: d => Drop(d, Widget.SqlTableName));
        yield return new SqlPattern("spec_create_table_typed_extra", PatternKind.Ddl, d => For(d).CreateTable<Widget>().Constraint("widgets_uq").Unique("code", "owner_id").Build(), 0, Cleanup: d => Drop(d, Widget.SqlTableName));

        Func<SqlDialectKind, IEnumerable<SqlResult>> table = d => [For(d).CreateTable("ddl_alt").Column("a", SqlType.Int, c => c.NotNull()).Column("b", SqlType.Int).Build()];
        Func<SqlDialectKind, IEnumerable<SqlResult>> drop  = d => Drop(d, "ddl_alt", "ddl_alt2");

        yield return new SqlPattern("spec_alter_add_column", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").AddColumn("c", SqlType.VarChar(20), c => c.Nullable()).Build(), 0, Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_add_column_default", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").AddColumn("c", SqlType.Int, c => c.Default(SqlValue.Of(0)).NotNull()).Build(), 0, Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_drop_column", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").DropColumn("b").Build(), 0, Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_drop_column_if_exists", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").DropColumn("zz", true).Build(), 0, OnlyOn(PG, MS), Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_column_type", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").AlterColumnType("b", SqlType.BigInt).Build(), 0, NotOn(LITE), Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_rename_column", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").RenameColumn("b", "c").Build(), 0, Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_rename_table", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").RenameTo("ddl_alt2").Build(), 0, NotOn(FB), Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_add_primary_key", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").AddPrimaryKey("ddl_alt_pk", "a").Build(), 0, NotOn(LITE), Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_add_unique", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").AddUnique("ddl_alt_u", "b").Build(), 0, NotOn(LITE), Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_add_check", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").AddCheck("ddl_alt_ck", p => p.Column("a").GreaterThan(-1)).Build(), 0, NotOn(LITE), Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_add_foreign_key", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").AddForeignKey("ddl_alt_fk", ["b"], "users", ["id"]).Build(), 0, NotOn(LITE), Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_alter_drop_constraint", PatternKind.Ddl, d => For(d).AlterTable("ddl_alt").DropConstraint("ddl_alt_u").Build(), 0, NotOn(LITE),
                                    Setup: d => [..table(d), For(d).AlterTable("ddl_alt").AddUnique("ddl_alt_u", "b").Build()], Cleanup: drop);

        yield return new SqlPattern("spec_drop_table", PatternKind.Ddl, d => For(d).DropTable("ddl_alt").Build(), 0, Setup: table, Cleanup: drop);
        yield return new SqlPattern("spec_drop_table_if_exists", PatternKind.Ddl, d => For(d).DropTable("ddl_none", ifExists: true).Build(), 0, NotOn(FB));
        yield return new SqlPattern("spec_drop_table_cascade", PatternKind.Ddl, d => For(d).DropTable("ddl_alt", cascade: true).Build(), 0, OnlyOn(PG, ORA, MY), Setup: table, Cleanup: drop);

        Func<SqlDialectKind, IEnumerable<SqlResult>> dropView = d => [For(d).DropView("ddl_v").Build()];
        yield return new SqlPattern("spec_create_view", PatternKind.Ddl, d => For(d).CreateView("ddl_v", q => q.Select("id", "name").From("users").Where("active").EqualTo(true)).Build(), 0, Cleanup: dropView);
        yield return new SqlPattern("spec_create_view_columns", PatternKind.Ddl, d => For(d).CreateView("ddl_v", q => q.Select("id", "name").From("users"), "x", "y").Build(), 0, Cleanup: dropView);
        yield return new SqlPattern("spec_create_or_replace_view", PatternKind.Ddl, d => For(d).CreateOrReplaceView("ddl_v", q => q.Select("id").From("users")).Build(), 0, NotOn(LITE),
                                    Setup: d => [For(d).CreateView("ddl_v", q => q.Select("id").From("users")).Build()], Cleanup: dropView);
        yield return new SqlPattern("spec_drop_view", PatternKind.Ddl, d => For(d).DropView("ddl_v").Build(), 0, Setup: d => [For(d).CreateView("ddl_v", q => q.Select("id").From("users")).Build()], Cleanup: dropView);
        yield return new SqlPattern("spec_drop_view_if_exists", PatternKind.Ddl, d => For(d).DropView("ddl_none_v", true).Build(), 0, NotOn(FB));

        Func<SqlDialectKind, IEnumerable<SqlResult>> index     = d => [For(d).CreateTable("ddl_ix_t").Column("a", SqlType.Int).Column("b", SqlType.Int).Build()];
        Func<SqlDialectKind, IEnumerable<SqlResult>> dropIndex = d => Drop(d, "ddl_ix_t");

        yield return new SqlPattern("spec_create_index", PatternKind.Ddl, d => For(d).CreateIndex("ddl_ix", "ddl_ix_t").Columns("a").Build(), 0, Setup: index, Cleanup: dropIndex);
        yield return new SqlPattern("spec_create_unique_index", PatternKind.Ddl, d => For(d).CreateIndex("ddl_ix", "ddl_ix_t", unique: true).Columns("a", "b").Build(), 0, Setup: index, Cleanup: dropIndex);
        yield return new SqlPattern("spec_create_index_desc", PatternKind.Ddl, d => For(d).CreateIndex("ddl_ix", "ddl_ix_t").Columns(("a", true), ("b", false)).Build(), 0, NotOn(FB), Setup: index, Cleanup: dropIndex);
        yield return new SqlPattern("spec_create_index_if_not_exists", PatternKind.Ddl, d => For(d).CreateIndex("ddl_ix", "ddl_ix_t", ifNotExists: true).Columns("a").Build(), 0, NotOn(MY, FB),
                                    Setup: d => [..index(d), For(d).CreateIndex("ddl_ix", "ddl_ix_t").Columns("a").Build()], Cleanup: dropIndex);
        yield return new SqlPattern("spec_create_index_include", PatternKind.Ddl, d => For(d).CreateIndex("ddl_ix", "ddl_ix_t").Columns("a").Include("b").Build(), 0, OnlyOn(PG, MS), Setup: index, Cleanup: dropIndex);
        yield return new SqlPattern("spec_create_index_partial", PatternKind.Ddl, d => For(d).CreateIndex("ddl_ix", "ddl_ix_t").Columns("a").Where(p => p.Column("a").GreaterThan(0)).Build(), 0, OnlyOn(PG, LITE, MS), Setup: index, Cleanup: dropIndex);
        yield return new SqlPattern("spec_create_index_using", PatternKind.Ddl, d => For(d).CreateIndex("ddl_ix", "ddl_ix_t").Using("btree").Columns("a").Build(), 0, OnlyOn(PG), Setup: index, Cleanup: dropIndex);
        yield return new SqlPattern("spec_drop_index", PatternKind.Ddl, d => For(d).DropIndex("ddl_ix", "ddl_ix_t").Build(), 0,
                                    Setup: d => [..index(d), For(d).CreateIndex("ddl_ix", "ddl_ix_t").Columns("a").Build()], Cleanup: dropIndex);
        yield return new SqlPattern("spec_drop_index_if_exists", PatternKind.Ddl, d => For(d).DropIndex("ddl_none_ix", "ddl_ix_t", true).Build(), 0, NotOn(MY, FB), Setup: index, Cleanup: dropIndex);
    }
}



/// <summary> A table whose metadata has real per-dialect types, keys, defaults, a check and a foreign key, for <see cref="SqlRoot.CreateTable{T}"/>. </summary>
public sealed class Widget : ISqlTable<Widget>
{
    private static readonly ISqlColumn[] __columns =
    [
        new DdlColumn("ID",      "id",       typeof(long),   SqlType.BigInt,      isPrimaryKey: true, isIdentity: true),
        new DdlColumn("Code",    "code",     typeof(string), SqlType.VarChar(20), unique: true),
        new DdlColumn("Quantity", "qty",     typeof(int),    SqlType.Int,         defaultSql: "0", checkSql: d => $"{TestData.Q(d, "qty")} >= 0"),
        new DdlColumn("Note",    "note",     typeof(string), SqlType.VarChar(100), isNullable: true),
        new DdlColumn("OwnerID", "owner_id", typeof(int),    SqlType.Int,         isNullable: true, foreignTable: "users", onDelete: SqlReferentialAction.SetNull)
    ];

    public static string                    SqlTableName => "widgets";
    public static IReadOnlyList<ISqlColumn> SqlColumns   => __columns;

    public static bool TrySqlColumn( string propertyName, out ISqlColumn? column )
    {
        column = Array.Find(__columns, c => c.PropertyName == propertyName);
        return column is not null;
    }
}



/// <summary> An <see cref="ISqlColumn"/> with every DDL member, typed through <see cref="SqlType"/>. </summary>
public sealed class DdlColumn( string propertyName, string columnName, Type clrType, SqlType type, bool isNullable = false, bool isPrimaryKey = false, bool isIdentity = false, bool unique = false, string? defaultSql = null, Func<SqlDialectKind, string>? checkSql = null, string? foreignTable = null, SqlReferentialAction onDelete = SqlReferentialAction.NoAction ) : ISqlColumn
{
    public string               PropertyName                            => propertyName;
    public string               ColumnName                              => columnName;
    public Type                 ClrType                                 => clrType;
    public bool                 IsNullable                              => isNullable;
    public bool                 IsPrimaryKey                            => isPrimaryKey;
    public bool                 IsIdentity                              => isIdentity;
    public bool                 IsUnique                                => unique;
    public string?              ForeignTable                            => foreignTable;
    public SqlReferentialAction OnDelete                                => onDelete;
    public string               GetTypeName( SqlDialectKind   dialect ) => type.ColumnName(dialect);
    public string?              GetDefaultSql( SqlDialectKind dialect ) => defaultSql;
    public string?              GetCheckSql( SqlDialectKind   dialect ) => checkSql?.Invoke(dialect);
}
