// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/25/2026

using System.ComponentModel;



namespace Jakar.SqlBuilder;


/// <summary> A stage a comparison continues to. Lets one <see cref="SqlComparison{TNext}"/> serve WHERE, ON, HAVING and predicate groups while keeping each one's type-state. </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISqlContinuation<out TSelf>
    where TSelf : allows ref struct
{
    static abstract TSelf Continue( SqlWriter writer );
}



/// <summary> Writes a predicate (for a group, <c>CASE WHEN</c>, <c>FILTER</c>, <c>CHECK</c>, ...): <c>p =&gt; p.Column("age").GreaterThan(18).And("country").EqualTo(param: "US")</c>. </summary>
public delegate SqlPredicateChain SqlPredicateFn( SqlPredicate predicate );



/// <summary>
///     The right-hand side of a comparison, shared by every predicate position. Inline values (<see cref="long"/>, <see cref="bool"/>, <see cref="SqlValue"/>) are written into the SQL;
///     <c>param:</c> values are bound. Dialect-specific operators are written natively or emulated: <c>ILIKE</c> as <c>LOWER(x) LIKE LOWER(p)</c>, <c>IS DISTINCT FROM</c> as
///     <c>IS NOT</c> (SQLite), <c>&lt;=&gt;</c> (MySQL) or <c>DECODE</c> (Oracle).
/// </summary>
public ref struct SqlComparison<TNext>
    where TNext : ISqlContinuation<TNext>, allows ref struct
{
    private          SqlWriter __w;
    private readonly int       __leftStart;

    internal SqlComparison( SqlWriter writer, int leftStart )
    {
        __w         = writer;
        __leftStart = leftStart;
    }

    private TNext Done() => TNext.Continue(__w);

    private TNext Inline( string op, long value )
    {
        __w.CheckCompared(value);
        SqlOps.Op(ref __w, op, value);
        return Done();
    }
    private TNext Inline( string op, bool value )
    {
        __w.CheckCompared(value);
        SqlOps.Op(ref __w, op, value);
        return Done();
    }
    private TNext Value( string op, SqlValue value )
    {
        __w.CheckCompared(in value);
        SqlOps.Op(ref __w, op, value);
        return Done();
    }
    private TNext Bound( string op, object? param )
    {
        __w.CheckCompared(param);
        SqlOps.OpParam(ref __w, op, param);
        return Done();
    }
    private TNext Expression( string op, SqlExprFn expression )
    {
        __w.ClearCompared();
        __w.Word(op);
        SqlExpr.Write(ref __w, expression);
        return Done();
    }


    // ---- comparisons ----

    public TNext EqualTo( long      value ) => Inline(SqlOps.EQUAL, value);
    public TNext EqualTo( bool      value ) => Inline(SqlOps.EQUAL, value);
    public TNext EqualTo( SqlValue  value ) => Value(SqlOps.EQUAL, value);
    public TNext EqualTo( object?   param ) => Bound(SqlOps.EQUAL, param);
    public TNext EqualTo( SqlExprFn expression ) => Expression(SqlOps.EQUAL, expression);

    public TNext NotEqualTo( long      value ) => Inline(SqlOps.NOT_EQUAL, value);
    public TNext NotEqualTo( bool      value ) => Inline(SqlOps.NOT_EQUAL, value);
    public TNext NotEqualTo( SqlValue  value ) => Value(SqlOps.NOT_EQUAL, value);
    public TNext NotEqualTo( object?   param ) => Bound(SqlOps.NOT_EQUAL, param);
    public TNext NotEqualTo( SqlExprFn expression ) => Expression(SqlOps.NOT_EQUAL, expression);

    public TNext GreaterThan( long      value ) => Inline(SqlOps.GREATER, value);
    public TNext GreaterThan( SqlValue  value ) => Value(SqlOps.GREATER, value);
    public TNext GreaterThan( object?   param ) => Bound(SqlOps.GREATER, param);
    public TNext GreaterThan( SqlExprFn expression ) => Expression(SqlOps.GREATER, expression);

    public TNext GreaterOrEqual( long      value ) => Inline(SqlOps.GREATER_EQUAL, value);
    public TNext GreaterOrEqual( SqlValue  value ) => Value(SqlOps.GREATER_EQUAL, value);
    public TNext GreaterOrEqual( object?   param ) => Bound(SqlOps.GREATER_EQUAL, param);
    public TNext GreaterOrEqual( SqlExprFn expression ) => Expression(SqlOps.GREATER_EQUAL, expression);

    public TNext LessThan( long      value ) => Inline(SqlOps.LESS, value);
    public TNext LessThan( SqlValue  value ) => Value(SqlOps.LESS, value);
    public TNext LessThan( object?   param ) => Bound(SqlOps.LESS, param);
    public TNext LessThan( SqlExprFn expression ) => Expression(SqlOps.LESS, expression);

    public TNext LessOrEqual( long      value ) => Inline(SqlOps.LESS_EQUAL, value);
    public TNext LessOrEqual( SqlValue  value ) => Value(SqlOps.LESS_EQUAL, value);
    public TNext LessOrEqual( object?   param ) => Bound(SqlOps.LESS_EQUAL, param);
    public TNext LessOrEqual( SqlExprFn expression ) => Expression(SqlOps.LESS_EQUAL, expression);

    public TNext EqualToColumn( string rawColumn )
    {
        __w.ClearCompared();
        SqlOps.OpColumn(ref __w, SqlOps.EQUAL, rawColumn);
        return Done();
    }
    public TNext EqualToColumn<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.ClearCompared();
        SqlOps.OpTypedColumn<T>(ref __w, SqlOps.EQUAL, propertyName);
        return Done();
    }
    public TNext NotEqualToColumn( string rawColumn )
    {
        __w.ClearCompared();
        SqlOps.OpColumn(ref __w, SqlOps.NOT_EQUAL, rawColumn);
        return Done();
    }
    public TNext NotEqualToColumn<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.ClearCompared();
        SqlOps.OpTypedColumn<T>(ref __w, SqlOps.NOT_EQUAL, propertyName);
        return Done();
    }


    // ---- NULL / DISTINCT FROM ----

    public TNext IsNull()
    {
        __w.CheckCompared<object?>(null, true);
        SqlOps.Null(ref __w, false);
        return Done();
    }
    public TNext IsNotNull()
    {
        __w.ClearCompared();
        SqlOps.Null(ref __w, true);
        return Done();
    }

    public TNext IsDistinctFrom( SqlValue     value )      => Distinct(true,  ( scoped ref SqlWriter w ) => value.Write(ref w));
    public TNext IsDistinctFrom( object?      param )      => Distinct(true,  ( scoped ref SqlWriter w ) => w.Parameter(param));
    public TNext IsDistinctFrom( SqlExprFn    expression ) => Distinct(true,  ( scoped ref SqlWriter w ) => SqlExpr.Write(ref w, expression));
    public TNext IsNotDistinctFrom( SqlValue  value )      => Distinct(false, ( scoped ref SqlWriter w ) => value.Write(ref w));
    public TNext IsNotDistinctFrom( object?   param )      => Distinct(false, ( scoped ref SqlWriter w ) => w.Parameter(param));
    public TNext IsNotDistinctFrom( SqlExprFn expression ) => Distinct(false, ( scoped ref SqlWriter w ) => SqlExpr.Write(ref w, expression));

    private TNext Distinct( bool distinct, RightSide right )
    {
        __w.ClearCompared();

        switch ( __w.Dialect )
        {
            case SqlDialectKind.Sqlite:
                __w.Word(distinct ? "IS NOT" : "IS");
                right(ref __w);
                return Done();

            case SqlDialectKind.MySql:
                if ( distinct ) { __w.InsertAt(__leftStart, "NOT ("); }

                __w.Word("<=>");
                right(ref __w);
                if ( distinct ) { __w.Append(')'); }

                return Done();

            case SqlDialectKind.Oracle:
                __w.InsertAt(__leftStart, "DECODE(");
                __w.Comma();
                right(ref __w);
                __w.Append(", 0, 1)");
                __w.Word(distinct ? "= 1" : "= 0");
                return Done();

            default:
                __w.Word(distinct ? "IS DISTINCT FROM" : "IS NOT DISTINCT FROM");
                right(ref __w);
                return Done();
        }
    }

    private delegate void RightSide( scoped ref SqlWriter writer );


    // ---- patterns ----

    public TNext Like( string   inline, char? escape = null ) => Pattern("LIKE",     SqlValue.Inline(inline), escape);
    public TNext Like( SqlValue pattern, char? escape = null ) => Pattern("LIKE",     pattern,                escape);
    public TNext NotLike( string   inline, char? escape = null ) => Pattern("NOT LIKE", SqlValue.Inline(inline), escape);
    public TNext NotLike( SqlValue pattern, char? escape = null ) => Pattern("NOT LIKE", pattern,                escape);

    /// <summary> Case-insensitive <c>LIKE</c>: <c>ILIKE</c> on PostgreSQL, <c>LOWER(x) LIKE LOWER(pattern)</c> elsewhere. </summary>
    public TNext ILike( string   inline, char? escape = null ) => CaseInsensitive(false, SqlValue.Inline(inline), escape);
    public TNext ILike( SqlValue pattern, char? escape = null ) => CaseInsensitive(false, pattern,                escape);
    public TNext NotILike( string   inline, char? escape = null ) => CaseInsensitive(true, SqlValue.Inline(inline), escape);
    public TNext NotILike( SqlValue pattern, char? escape = null ) => CaseInsensitive(true, pattern,                escape);

    /// <summary> SQLite's <c>GLOB</c> (case-sensitive, <c>*</c> / <c>?</c> wildcards). </summary>
    public TNext Glob( string inline )
    {
        __w.Require(__w.Dialect.SupportsGlob(), "GLOB");
        return Pattern("GLOB", SqlValue.Inline(inline), null);
    }
    public TNext Glob( SqlValue pattern )
    {
        __w.Require(__w.Dialect.SupportsGlob(), "GLOB");
        return Pattern("GLOB", pattern, null);
    }

    /// <summary> <c>SIMILAR TO</c> (PostgreSQL, Firebird). </summary>
    public TNext SimilarTo( string inline, char? escape = null )
    {
        __w.Require(__w.Dialect.SupportsSimilarTo(), "SIMILAR TO");
        return Pattern("SIMILAR TO", SqlValue.Inline(inline), escape);
    }
    public TNext NotSimilarTo( string inline, char? escape = null )
    {
        __w.Require(__w.Dialect.SupportsSimilarTo(), "SIMILAR TO");
        return Pattern("NOT SIMILAR TO", SqlValue.Inline(inline), escape);
    }

    private TNext Pattern( string op, SqlValue pattern, char? escape )
    {
        __w.CheckCompared(in pattern);
        __w.Word(op);
        pattern.Write(ref __w);
        WriteEscape(escape);
        return Done();
    }

    private TNext CaseInsensitive( bool negate, SqlValue pattern, char? escape )
    {
        __w.ClearCompared();

        if ( __w.Dialect.SupportsIlike() )
        {
            __w.Word(negate ? "NOT ILIKE" : "ILIKE");
            pattern.Write(ref __w);
            WriteEscape(escape);
            return Done();
        }

        __w.InsertAt(__leftStart, "LOWER(");
        __w.Append(')');
        __w.Word(negate ? "NOT LIKE" : "LIKE");
        __w.Word("LOWER");
        __w.OpenCall();
        pattern.Write(ref __w);
        __w.CloseParen();
        WriteEscape(escape);
        return Done();
    }

    private void WriteEscape( char? escape )
    {
        if ( escape is not { } c ) { return; }

        __w.Word("ESCAPE");
        __w.InlineString(c.ToString());
    }


    // ---- sets and ranges ----

    public TNext In( params ReadOnlySpan<long> values )
    {
        __w.ClearCompared();
        SqlOps.In(ref __w, values);
        return Done();
    }
    public TNext InParams( params object?[] values )
    {
        __w.ClearCompared();
        SqlOps.InParams(ref __w, values);
        return Done();
    }
    public TNext InValues( params ReadOnlySpan<SqlValue> values )
    {
        __w.ClearCompared();
        SqlOps.InValues(ref __w, "IN", values);
        return Done();
    }
    public TNext In<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.ClearCompared();
        __w.Word("IN");
        Subqueries.Write(ref __w, query, true);
        return Done();
    }

    public TNext NotIn( params ReadOnlySpan<long> values )
    {
        __w.ClearCompared();
        __w.Word("NOT");
        SqlOps.In(ref __w, values);
        return Done();
    }
    public TNext NotInParams( params object?[] values )
    {
        __w.ClearCompared();
        __w.Word("NOT");
        SqlOps.InParams(ref __w, values);
        return Done();
    }
    public TNext NotInValues( params ReadOnlySpan<SqlValue> values )
    {
        __w.ClearCompared();
        SqlOps.InValues(ref __w, "NOT IN", values);
        return Done();
    }
    public TNext NotIn<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.ClearCompared();
        __w.Word("NOT IN");
        Subqueries.Write(ref __w, query, true);
        return Done();
    }

    /// <summary> <c>= ANY(@array)</c> with an array parameter (PostgreSQL). </summary>
    public TNext Any( object arrayParam )
    {
        __w.ClearCompared();
        __w.Require(__w.Dialect.SupportsAnyArray(), "= ANY(array)");
        __w.Word("= ANY");
        __w.OpenCall();
        __w.Parameter(arrayParam ?? throw new ArgumentNullException(nameof(arrayParam)));
        __w.CloseParen();
        return Done();
    }

    public TNext Between( long low, long high )
    {
        __w.CheckCompared(low);
        SqlOps.Between(ref __w, low, high);
        return Done();
    }
    public TNext Between( SqlValue low, SqlValue high )
    {
        __w.CheckCompared(in low);
        SqlOps.Between(ref __w, "BETWEEN", low, high);
        return Done();
    }
    public TNext NotBetween( long low, long high ) => NotBetween(SqlValue.Of(low), SqlValue.Of(high));
    public TNext NotBetween( SqlValue low, SqlValue high )
    {
        __w.CheckCompared(in low);
        SqlOps.Between(ref __w, "NOT BETWEEN", low, high);
        return Done();
    }


    // ---- JSON ----

    /// <summary> JSON containment: <c>left @&gt; other</c> (PostgreSQL) or <c>JSON_CONTAINS(left, other)</c> (MySQL). </summary>
    public TNext JsonContains( SqlExprFn other )
    {
        __w.ClearCompared();
        __w.Require(__w.Dialect.SupportsJsonContains(), "JSON containment");

        if ( __w.Dialect is SqlDialectKind.MySql )
        {
            __w.InsertAt(__leftStart, "JSON_CONTAINS(");
            __w.Comma();
            SqlExpr.Write(ref __w, other);
            __w.Append(')');
            return Done();
        }

        __w.Word("@>");
        SqlExpr.Write(ref __w, other);
        return Done();
    }
}



/// <summary> The start of a predicate inside a group, <c>CASE WHEN</c>, <c>FILTER</c> or <c>CHECK</c>. </summary>
public ref struct SqlPredicate
{
    private SqlWriter __w;

    internal SqlPredicate( SqlWriter writer ) => __w = writer;

    public SqlComparison<SqlPredicateChain> Column( string column ) => Predicates.Column<SqlPredicateChain>(ref __w, null, column);
    public SqlComparison<SqlPredicateChain> Column<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<SqlPredicateChain, T>(ref __w, null, propertyName);
    public SqlComparison<SqlPredicateChain> Expr( SqlExprFn expression ) => Predicates.Expr<SqlPredicateChain>(ref __w, null, expression);

    public SqlPredicateChain Group( SqlPredicateFn predicate ) => Predicates.Group<SqlPredicateChain>(ref __w, null, predicate, false);
    public SqlPredicateChain Not( SqlPredicateFn   predicate ) => Predicates.Group<SqlPredicateChain>(ref __w, null, predicate, true);
    public SqlPredicateChain Exists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<SqlPredicateChain, TEnd>(ref __w, null, query, false);
    public SqlPredicateChain NotExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<SqlPredicateChain, TEnd>(ref __w, null, query, true);

    /// <summary> <c>TRUE</c> / <c>FALSE</c> as a predicate (<c>1 = 1</c> / <c>1 = 0</c> where there's no boolean literal). </summary>
    public SqlPredicateChain Always( bool value ) => Predicates.Constant<SqlPredicateChain>(ref __w, null, value);

    /// <summary> A trusted SQL predicate, written as given (the audited injection boundary). </summary>
    public SqlPredicateChain Raw( string sql ) => Predicates.Raw<SqlPredicateChain>(ref __w, null, sql);
}



/// <summary> After a complete predicate in a group: chain more with <c>AND</c> / <c>OR</c>, or return it. </summary>
public ref struct SqlPredicateChain : ISqlContinuation<SqlPredicateChain>
{
    private SqlWriter __w;

    internal SqlPredicateChain( SqlWriter writer ) => __w = writer;

    internal readonly SqlWriter Writer => __w;

    static SqlPredicateChain ISqlContinuation<SqlPredicateChain>.Continue( SqlWriter writer ) => new(writer);

    public SqlComparison<SqlPredicateChain> And( string column ) => Predicates.Column<SqlPredicateChain>(ref __w, "AND", column);
    public SqlComparison<SqlPredicateChain> Or( string  column ) => Predicates.Column<SqlPredicateChain>(ref __w, "OR",  column);
    public SqlComparison<SqlPredicateChain> And<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<SqlPredicateChain, T>(ref __w, "AND", propertyName);
    public SqlComparison<SqlPredicateChain> Or<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<SqlPredicateChain, T>(ref __w, "OR", propertyName);
    public SqlComparison<SqlPredicateChain> AndExpr( SqlExprFn expression ) => Predicates.Expr<SqlPredicateChain>(ref __w, "AND", expression);
    public SqlComparison<SqlPredicateChain> OrExpr( SqlExprFn  expression ) => Predicates.Expr<SqlPredicateChain>(ref __w, "OR",  expression);

    public SqlPredicateChain And( SqlPredicateFn    predicate ) => Predicates.Group<SqlPredicateChain>(ref __w, "AND", predicate, false);
    public SqlPredicateChain Or( SqlPredicateFn     predicate ) => Predicates.Group<SqlPredicateChain>(ref __w, "OR",  predicate, false);
    public SqlPredicateChain AndNot( SqlPredicateFn predicate ) => Predicates.Group<SqlPredicateChain>(ref __w, "AND", predicate, true);
    public SqlPredicateChain OrNot( SqlPredicateFn  predicate ) => Predicates.Group<SqlPredicateChain>(ref __w, "OR",  predicate, true);
    public SqlPredicateChain AndExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<SqlPredicateChain, TEnd>(ref __w, "AND", query, false);
    public SqlPredicateChain AndNotExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<SqlPredicateChain, TEnd>(ref __w, "AND", query, true);
    public SqlPredicateChain OrExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<SqlPredicateChain, TEnd>(ref __w, "OR", query, false);
}



/// <summary> The shared left-hand sides of predicates; each stage passes its keyword (<c>WHERE</c>, <c>AND</c>, <c>ON</c>, ...) and continuation type. </summary>
internal static class Predicates
{
    /// <summary> Writes a whole predicate (no surrounding parentheses). </summary>
    /// <exception cref="SqlBuildException"> It wrote nothing (<see cref="SqlBuildError.EmptyPredicate"/>). </exception>
    public static void Write( scoped ref SqlWriter writer, SqlPredicateFn predicate )
    {
        ArgumentNullException.ThrowIfNull(predicate);
        int               start  = writer.Length;
        SqlPredicateChain result = predicate(new SqlPredicate(writer));
        if ( result.Writer.Length <= start ) { throw writer.Error(SqlBuildError.EmptyPredicate, "A predicate wrote nothing."); }

        writer = result.Writer;
        writer.MarkClause(ClauseFlags.Predicate);
    }

    private static int Begin( scoped ref SqlWriter writer, string? keyword )
    {
        if ( keyword is not null ) { writer.Word(keyword); }

        writer.Separate();
        return writer.Length;
    }

    public static SqlComparison<TNext> Column<TNext>( scoped ref SqlWriter writer, string? keyword, string column )
        where TNext : ISqlContinuation<TNext>, allows ref struct
    {
        int start = Begin(ref writer, keyword);
        writer.ColumnRef(column);
        writer.ClearCompared();
        return new SqlComparison<TNext>(writer, start);
    }

    public static SqlComparison<TNext> Typed<TNext, T>( scoped ref SqlWriter writer, string? keyword, string propertyName )
        where TNext : ISqlContinuation<TNext>, allows ref struct
        where T : ISqlTable<T>
    {
        int        start  = Begin(ref writer, keyword);
        ISqlColumn column = SqlOps.TypedColumn<T>(ref writer, propertyName);
        writer.SetComparedColumn(column);
        return new SqlComparison<TNext>(writer, start);
    }

    public static SqlComparison<TNext> Expr<TNext>( scoped ref SqlWriter writer, string? keyword, SqlExprFn expression )
        where TNext : ISqlContinuation<TNext>, allows ref struct
    {
        int start = Begin(ref writer, keyword);
        SqlExpr.Write(ref writer, expression);
        writer.ClearCompared();
        return new SqlComparison<TNext>(writer, start);
    }

    public static TNext Group<TNext>( scoped ref SqlWriter writer, string? keyword, SqlPredicateFn predicate, bool negate )
        where TNext : ISqlContinuation<TNext>, allows ref struct
    {
        if ( keyword is not null ) { writer.Word(keyword); }

        if ( negate ) { writer.Word("NOT"); }

        writer.OpenParen();
        Write(ref writer, predicate);
        writer.CloseParen();
        return TNext.Continue(writer);
    }

    public static TNext Exists<TNext, TEnd>( scoped ref SqlWriter writer, string? keyword, SqlSubquery<TEnd> query, bool negate )
        where TNext : ISqlContinuation<TNext>, allows ref struct
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        if ( keyword is not null ) { writer.Word(keyword); }

        writer.Word(negate ? "NOT EXISTS" : "EXISTS");
        Subqueries.Write(ref writer, query, true);
        writer.MarkClause(ClauseFlags.Predicate);
        return TNext.Continue(writer);
    }

    public static TNext Constant<TNext>( scoped ref SqlWriter writer, string? keyword, bool value )
        where TNext : ISqlContinuation<TNext>, allows ref struct
    {
        if ( keyword is not null ) { writer.Word(keyword); }

        if ( writer.Dialect.SupportsBooleanLiteral() ) { writer.Word(value ? "TRUE" : "FALSE"); }
        else { writer.Word(value ? "1 = 1" : "1 = 0"); }

        return TNext.Continue(writer);
    }

    public static TNext Raw<TNext>( scoped ref SqlWriter writer, string? keyword, string sql )
        where TNext : ISqlContinuation<TNext>, allows ref struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        if ( keyword is not null ) { writer.Word(keyword); }

        writer.InlineRaw(sql);
        return TNext.Continue(writer);
    }
}



/// <summary> A query that can stand inside another one (subquery, derived table, CTE body, <c>INSERT ... SELECT</c>). Every SELECT stage that can end a query implements it. </summary>
public interface ISqlQueryEnd
{
    [EditorBrowsable(EditorBrowsableState.Never)] SqlWriter EndQuery();
}



/// <summary> Writes a nested SELECT and returns the stage it ended on: <c>q =&gt; q.Select("id").From("users").Where("active").EqualTo(true)</c> (no <c>Build()</c>). </summary>
public delegate TEnd SqlSubquery<out TEnd>( SqlRoot query )
    where TEnd : ISqlQueryEnd, allows ref struct;



internal static class Subqueries
{
    /// <summary> Writes <paramref name="query"/> in its own scope (tables, paging, locking), optionally in parentheses. </summary>
    public static void Write<TEnd>( scoped ref SqlWriter writer, SqlSubquery<TEnd> query, bool parenthesize )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        ArgumentNullException.ThrowIfNull(query);
        if ( parenthesize ) { writer.OpenParen(); }

        SqlWriter.Scope saved = writer.BeginScope();
        TEnd            end   = query(SqlRoot.Nested(writer));
        writer = end.EndQuery();
        writer.EndScope(saved);
        if ( parenthesize ) { writer.CloseParen(); }
    }
}
