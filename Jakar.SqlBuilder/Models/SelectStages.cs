// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Paging is recorded on the writer and written when the query ends, in the dialect's form (<c>LIMIT</c> / <c>OFFSET</c>, <c>OFFSET ... ROWS FETCH NEXT ... ROWS ONLY</c>), whatever order it was called in. </summary>
internal static class Paging
{
    public static void Limit( scoped ref SqlWriter     writer, long count ) => writer.SetLimit(count);
    public static void Offset( scoped ref SqlWriter    writer, long count ) => writer.SetOffset(count);
    public static void FetchNext( scoped ref SqlWriter writer, long count ) => writer.SetLimit(count);
}



/// <summary> After <c>SELECT</c> projection. Add more projection items, then <c>FROM</c>. </summary>
public ref struct SelectStage : ISqlQueryEnd
{
    private          SqlWriter __w;
    private          bool      __has;
    private readonly int       __topAt;

    internal SelectStage( SqlWriter writer, bool hasProjection, int topAt )
    {
        __w     = writer;
        __has   = hasProjection;
        __topAt = topAt;
    }

    private SelectStage Next( bool has = true ) => new(__w, __has || has, __topAt);

    /// <summary>
    ///     SQL Server's <c>TOP (n) [PERCENT] [WITH TIES]</c>, written right after <c>SELECT</c> whenever it's called; every other dialect gets the equivalent row limit
    ///     (<c>LIMIT n</c> / <c>FETCH FIRST n [PERCENT] ROWS ONLY|WITH TIES</c>).
    /// </summary>
    public SelectStage Top( long count, bool percent = false, bool withTies = false )
    {
        if ( count < 0 ) { throw __w.Error(SqlBuildError.InvalidLiteral, $"TOP can't be negative ({count})."); }

        if ( percent ) { __w.Require(__w.Dialect.SupportsLimitPercent(), "TOP ... PERCENT"); }

        if ( withTies ) { __w.Require(__w.Dialect.SupportsWithTies(), "TOP ... WITH TIES"); }

        if ( __w.Dialect is SqlDialectKind.SqlServer )
        {
            string text = $" TOP ({count.ToString(System.Globalization.CultureInfo.InvariantCulture)}){( percent ? " PERCENT" : string.Empty )}{( withTies ? " WITH TIES" : string.Empty )}";
            __w.InsertAt(__topAt, text);
            __w.MarkClause(ClauseFlags.Top);
            if ( withTies ) { __w.RequireOrderBy("TOP ... WITH TIES"); }
        }
        else
        {
            __w.SetLimit(count);
            __w.SetLimitOptions(percent, withTies);
        }

        return new SelectStage(__w, __has, __topAt);
    }

    public SelectStage Column( string name )
    {
        if ( __has ) { __w.Comma(); }

        if ( name is "*" ) { __w.Star(); }
        else { __w.ColumnRef(name); }

        return Next();
    }
    public SelectStage Column<T>( string propertyName )
        where T : ISqlTable<T>
    {
        if ( __has ) { __w.Comma(); }

        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.ClearCompared();
        return Next();
    }

    /// <summary> An expression column: <c>.Column(e =&gt; e.RowNumber().Over(w =&gt; w.OrderBy("id")), as: "rn")</c>. </summary>
    public SelectStage Column( SqlExprFn expression, string? @as = null )
    {
        if ( __has ) { __w.Comma(); }

        SqlExpr.Write(ref __w, expression);
        AppendAlias(@as);
        return Next();
    }

    public SelectStage As( string alias )
    {
        if ( !__has ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "AS needs a projection item to alias."); }

        __w.Word("AS");
        __w.Identifier(alias);
        return Next();
    }

    public SelectStage Count( string column, string? @as = null ) => Aggregate("COUNT", column, @as, false);
    public SelectStage Sum( string   column, string? @as = null ) => Aggregate("SUM",   column, @as, false);
    public SelectStage Avg( string   column, string? @as = null ) => Aggregate("AVG",   column, @as, false);
    public SelectStage Min( string   column, string? @as = null ) => Aggregate("MIN",   column, @as, false);
    public SelectStage Max( string   column, string? @as = null ) => Aggregate("MAX",   column, @as, false);

    /// <summary> <c>COUNT(DISTINCT column)</c>. </summary>
    public SelectStage CountDistinct( string column, string? @as = null ) => Aggregate("COUNT", column, @as, true);

    public SelectStage Count<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("COUNT", propertyName, @as, false);
    public SelectStage Sum<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("SUM", propertyName, @as, false);
    public SelectStage Avg<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("AVG", propertyName, @as, false);
    public SelectStage Min<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("MIN", propertyName, @as, false);
    public SelectStage Max<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("MAX", propertyName, @as, false);
    public SelectStage CountDistinct<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("COUNT", propertyName, @as, true);

    private SelectStage Aggregate( string func, string column, string? @as, bool distinct )
    {
        if ( __has ) { __w.Comma(); }

        __w.Word(func);
        __w.OpenCall();
        if ( distinct ) { __w.Word("DISTINCT"); }

        if ( column is "*" )
        {
            if ( distinct ) { throw new ArgumentException("COUNT(DISTINCT *) isn't SQL; name a column.", nameof(column)); }

            __w.Append('*');
        }
        else { __w.ColumnRef(column); }

        __w.CloseParen();
        AppendAlias(@as);
        return Next();
    }
    private SelectStage AggregateTyped<T>( string func, string propertyName, string? @as, bool distinct )
        where T : ISqlTable<T>
    {
        if ( __has ) { __w.Comma(); }

        __w.Word(func);
        __w.OpenCall();
        if ( distinct ) { __w.Word("DISTINCT"); }

        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.ClearCompared();
        __w.CloseParen();
        AppendAlias(@as);
        return Next();
    }
    private void AppendAlias( string? @as )
    {
        if ( @as is not { Length: > 0 } ) { return; }

        __w.Word("AS");
        __w.Identifier(@as);
    }


    // ---- sources ----

    public FromStage From( string table, string? alias = null )
    {
        RequireProjection();
        Clauses.Source(ref __w, "FROM", table, alias);
        __w.MarkClause(ClauseFlags.From);
        return new FromStage(__w);
    }
    public FromStage From<T>( string? alias = null )
        where T : ISqlTable<T>
    {
        RequireProjection();
        Clauses.Source(ref __w, "FROM", T.SqlTableName, alias);
        __w.MarkClause(ClauseFlags.From);
        return new FromStage(__w);
    }

    /// <summary> A derived table: <c>FROM (SELECT ...) alias</c>. </summary>
    public FromStage From<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        RequireProjection();
        Clauses.DerivedTable(ref __w, "FROM", query, alias);
        __w.MarkClause(ClauseFlags.From);
        return new FromStage(__w);
    }

    /// <summary>
    ///     A <c>VALUES</c> table: <c>FROM (VALUES (...), (...)) alias (columns)</c>; MySQL writes <c>ROW(...)</c>, and SQLite and Firebird (which have no table value constructor) get the
    ///     equivalent <c>SELECT ... UNION ALL SELECT ...</c>.
    /// </summary>
    public FromStage FromValues( string alias, scoped ReadOnlySpan<string> columns, SqlValuesFn rows )
    {
        RequireProjection();
        __w.Word("FROM");
        SqlValuesRows.Write(ref __w, alias, columns, rows);
        __w.MarkClause(ClauseFlags.From);
        return new FromStage(__w);
    }

    /// <summary> A table function: <c>FROM name(arguments) alias</c> (e.g. <c>generate_series</c>). The function name is a trusted identifier. </summary>
    public FromStage FromFunction( string function, string alias, params ReadOnlySpan<SqlExprFn> arguments )
    {
        RequireProjection();
        __w.Word("FROM");
        SqlExpr expression = new SqlExpr(__w).Function(function, arguments);
        __w = expression.Writer;
        __w.Identifier(alias);
        __w.RegisterTable(alias, alias);
        __w.MarkClause(ClauseFlags.From);
        return new FromStage(__w);
    }

    private readonly void RequireProjection()
    {
        if ( !__has ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "SELECT needs at least one column or aggregate before FROM."); }
    }

    private void Sourceless()
    {
        if ( __w.IsBuilt ) { return; }

        RequireProjection();
        if ( __w.Dialect.SourcelessFromTable() is { } table )
        {
            __w.Word("FROM");
            __w.Word(table);
        }
    }

    /// <summary> A <c>SELECT</c> without a <c>FROM</c>; Oracle and Firebird read it from their one-row table (<c>DUAL</c> / <c>RDB$DATABASE</c>). </summary>
    public SqlResult Build()
    {
        Sourceless();
        return __w.Build();
    }

    public string BuildSql()
    {
        Sourceless();
        return __w.BuildSql();
    }

    public SqlParameterSet WriteTo( IBufferWriter<char> destination )
    {
        Sourceless();
        return __w.WriteTo(destination);
    }

    SqlWriter ISqlQueryEnd.EndQuery()
    {
        Sourceless();
        return __w;
    }

    /// <summary> A source-less operand of a set operation (e.g. the anchor of a recursive CTE: <c>SELECT 1 UNION ALL ...</c>). </summary>
    public SqlRoot Union()        => SetOperation("UNION",     false);
    public SqlRoot UnionAll()     => SetOperation("UNION",     true);
    public SqlRoot Intersect()    => SetOperation("INTERSECT", false);
    public SqlRoot Except()       => SetOperation("EXCEPT",    false);

    private SqlRoot SetOperation( string keyword, bool all )
    {
        Sourceless();
        return Clauses.SetOperation(ref __w, keyword, all);
    }
}



/// <summary> Rows for <see cref="SelectStage.FromValues"/>: <c>r =&gt; r.Row(SqlValue.Of(1), SqlValue.Inline("a")).Row(...)</c>. </summary>
public delegate SqlValuesRows SqlValuesFn( SqlValuesRows rows );



/// <summary> The rows of a <c>VALUES</c> table. Every row has one value per column. </summary>
public ref struct SqlValuesRows
{
    private          SqlWriter __w;
    private readonly string[]  __columns;
    private          int       __rows;

    private SqlValuesRows( SqlWriter writer, string[] columns )
    {
        __w       = writer;
        __columns = columns;
        __rows    = 0;
    }

    private readonly bool Emulated => __w.Dialect is SqlDialectKind.Sqlite or SqlDialectKind.Firebird;

    public SqlValuesRows Row( params ReadOnlySpan<SqlValue> values )
    {
        if ( values.Length != __columns.Length ) { throw new ArgumentException($"Each VALUES row needs {__columns.Length} values; got {values.Length}.", nameof(values)); }

        if ( Emulated )
        {
            if ( __rows > 0 ) { __w.Word("UNION ALL"); }

            __w.Word("SELECT");
            for ( int i = 0; i < values.Length; i++ )
            {
                if ( i > 0 ) { __w.Comma(); }

                values[i].Write(ref __w);
                if ( __rows is 0 )
                {
                    __w.Word("AS");
                    __w.Identifier(__columns[i]);
                }
            }

            if ( __w.Dialect is SqlDialectKind.Firebird ) { __w.Word("FROM RDB$DATABASE"); }
        }
        else
        {
            if ( __rows > 0 ) { __w.Comma(); }

            if ( __w.Dialect is SqlDialectKind.MySql ) { __w.Word("ROW"); }

            __w.OpenParen();
            for ( int i = 0; i < values.Length; i++ )
            {
                if ( i > 0 ) { __w.Comma(); }

                values[i].Write(ref __w);
            }

            __w.CloseParen();
        }

        __rows++;
        return this;
    }

    internal static void Write( scoped ref SqlWriter writer, string alias, scoped ReadOnlySpan<string> columns, SqlValuesFn rows )
    {
        ArgumentNullException.ThrowIfNull(rows);
        SqlWriter.RequireName(alias, nameof(alias));
        writer.RequireItems(columns.Length, "VALUES columns");

        writer.OpenParen();
        bool emulated = writer.Dialect is SqlDialectKind.Sqlite or SqlDialectKind.Firebird;
        if ( !emulated ) { writer.Word("VALUES"); }

        SqlValuesRows result = rows(new SqlValuesRows(writer, columns.ToArray()));
        if ( result.__rows is 0 ) { throw writer.Error(SqlBuildError.MissingRequiredClause, "VALUES needs at least one row."); }

        writer = result.__w;
        writer.CloseParen();
        writer.Identifier(alias);
        if ( !emulated ) { Clauses.WriteIdentifiers(ref writer, columns); }

        writer.RegisterTable(alias, alias);
    }
}



/// <summary> After <c>FROM</c>: joins, filtering, grouping, ordering, paging, set-ops, locking, terminate. </summary>
public ref struct FromStage : ISqlQueryEnd
{
    private SqlWriter __w;

    internal FromStage( SqlWriter writer ) => __w = writer;

    // ---- joins ----

    public JoinStage InnerJoin( string      table, string? alias = null ) => Join("INNER JOIN",       table, alias);
    public JoinStage LeftJoin( string       table, string? alias = null ) => Join("LEFT JOIN",        table, alias);
    public JoinStage RightJoin( string      table, string? alias = null ) => Join("RIGHT JOIN",       table, alias);
    public JoinStage FullJoin( string       table, string? alias = null ) => Join("FULL JOIN",        table, alias);
    public JoinStage LeftOuterJoin( string  table, string? alias = null ) => Join("LEFT OUTER JOIN",  table, alias);
    public JoinStage RightOuterJoin( string table, string? alias = null ) => Join("RIGHT OUTER JOIN", table, alias);
    public JoinStage FullOuterJoin( string  table, string? alias = null ) => Join("FULL OUTER JOIN",  table, alias);

    public JoinStage<T> InnerJoin<T>( string? alias = null )
        where T : ISqlTable<T> => JoinTyped<T>("INNER JOIN", alias);
    public JoinStage<T> LeftJoin<T>( string? alias = null )
        where T : ISqlTable<T> => JoinTyped<T>("LEFT JOIN", alias);
    public JoinStage<T> RightJoin<T>( string? alias = null )
        where T : ISqlTable<T> => JoinTyped<T>("RIGHT JOIN", alias);
    public JoinStage<T> FullJoin<T>( string? alias = null )
        where T : ISqlTable<T> => JoinTyped<T>("FULL JOIN", alias);
    public JoinStage<T> LeftOuterJoin<T>( string? alias = null )
        where T : ISqlTable<T> => JoinTyped<T>("LEFT OUTER JOIN", alias);
    public JoinStage<T> RightOuterJoin<T>( string? alias = null )
        where T : ISqlTable<T> => JoinTyped<T>("RIGHT OUTER JOIN", alias);
    public JoinStage<T> FullOuterJoin<T>( string? alias = null )
        where T : ISqlTable<T> => JoinTyped<T>("FULL OUTER JOIN", alias);

    /// <summary> Joins a derived table: <c>INNER JOIN (SELECT ...) alias ON ...</c>. </summary>
    public JoinStage InnerJoin<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct => JoinDerived("INNER JOIN", query, alias);
    public JoinStage LeftJoin<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct => JoinDerived("LEFT JOIN", query, alias);

    public FromStage CrossJoin( string table, string? alias = null )
    {
        Clauses.Source(ref __w, "CROSS JOIN", table, alias);
        __w.MarkClause(ClauseFlags.Join);
        return new FromStage(__w);
    }
    public FromStage CrossJoin<T>( string? alias = null )
        where T : ISqlTable<T> => CrossJoin(T.SqlTableName, alias);

    /// <summary> <c>NATURAL JOIN</c> (not SQL Server). </summary>
    public FromStage NaturalJoin( string table, string? alias = null ) => Natural("NATURAL JOIN", table, alias);
    public FromStage NaturalLeftJoin( string table, string? alias = null ) => Natural("NATURAL LEFT JOIN", table, alias);

    private FromStage Natural( string keyword, string table, string? alias )
    {
        __w.Require(__w.Dialect.SupportsNaturalJoin(), keyword);
        Clauses.Source(ref __w, keyword, table, alias);
        __w.MarkClause(ClauseFlags.Join);
        return new FromStage(__w);
    }

    /// <summary> Another source after a comma: <c>FROM a, b</c>. </summary>
    public FromStage AndFrom( string table, string? alias = null )
    {
        __w.Comma();
        __w.Identifier(table);
        if ( alias is { Length: > 0 } ) { __w.Identifier(alias); }

        __w.RegisterTable(table, alias);
        __w.MarkClause(ClauseFlags.Join);
        return new FromStage(__w);
    }
    public FromStage AndFrom<T>( string? alias = null )
        where T : ISqlTable<T> => AndFrom(T.SqlTableName, alias);

    /// <summary> A correlated derived table: <c>CROSS APPLY (...) alias</c> (SQL Server, Oracle) or <c>CROSS JOIN LATERAL (...) alias</c> (PostgreSQL, MySQL, Firebird). </summary>
    public FromStage CrossApply<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct => Apply(query, alias, false);

    /// <summary> <c>OUTER APPLY (...) alias</c> (SQL Server, Oracle) or <c>LEFT JOIN LATERAL (...) alias ON TRUE</c>. </summary>
    public FromStage OuterApply<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct => Apply(query, alias, true);

    private FromStage Apply<TEnd>( SqlSubquery<TEnd> query, string alias, bool outer )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.Require(__w.Dialect.SupportsLateral(), outer ? "OUTER APPLY / LEFT JOIN LATERAL" : "CROSS APPLY / JOIN LATERAL");
        bool apply = __w.Dialect is SqlDialectKind.SqlServer or SqlDialectKind.Oracle;

        Clauses.DerivedTable(ref __w, apply
                                          ? outer ? "OUTER APPLY" : "CROSS APPLY"
                                          : outer ? "LEFT JOIN LATERAL" : "CROSS JOIN LATERAL", query, alias);

        if ( outer && !apply ) { __w.Word(__w.Dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Firebird ? "ON TRUE" : "ON 1 = 1"); }

        __w.MarkClause(ClauseFlags.Join);
        return new FromStage(__w);
    }

    /// <summary> SQL Server table hints for the last source: <c>WITH (NOLOCK)</c>. </summary>
    public FromStage WithHints( params ReadOnlySpan<string> hints )
    {
        Clauses.TableHints(ref __w, hints);
        return new FromStage(__w);
    }

    private JoinStage Join( string keyword, string table, string? alias )
    {
        RequireFullJoin(keyword);
        Clauses.Source(ref __w, keyword, table, alias);
        __w.MarkClause(ClauseFlags.Join);
        return new JoinStage(__w);
    }
    private JoinStage<T> JoinTyped<T>( string keyword, string? alias )
        where T : ISqlTable<T>
    {
        RequireFullJoin(keyword);
        Clauses.Source(ref __w, keyword, T.SqlTableName, alias);
        __w.MarkClause(ClauseFlags.Join);
        return new JoinStage<T>(__w);
    }
    private JoinStage JoinDerived<TEnd>( string keyword, SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        Clauses.DerivedTable(ref __w, keyword, query, alias);
        __w.MarkClause(ClauseFlags.Join);
        return new JoinStage(__w);
    }
    private readonly void RequireFullJoin( string keyword )
    {
        if ( keyword.StartsWith("FULL", StringComparison.Ordinal) && !__w.Dialect.SupportsFullOuterJoin() ) { throw __w.Error(SqlBuildError.UnsupportedFeature, $"FULL OUTER JOIN is not supported by {__w.Dialect}."); }
    }


    // ---- WHERE ----

    public SqlComparison<SelectWhereStage> Where( string column )
    {
        __w.MarkClause(ClauseFlags.Where);
        return Predicates.Column<SelectWhereStage>(ref __w, "WHERE", column);
    }
    public SqlComparison<SelectWhereStage> Where<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.MarkClause(ClauseFlags.Where);
        return Predicates.Typed<SelectWhereStage, T>(ref __w, "WHERE", propertyName);
    }
    public SqlComparison<SelectWhereStage> Where( SqlExprFn expression )
    {
        __w.MarkClause(ClauseFlags.Where);
        return Predicates.Expr<SelectWhereStage>(ref __w, "WHERE", expression);
    }

    /// <summary> <c>WHERE ( ... )</c>: a predicate group, e.g. <c>.Where(g =&gt; g.Column("age").GreaterThan(18).And("country").EqualTo(param: "US"))</c>. </summary>
    public SelectWhereStage Where( SqlPredicateFn predicate )
    {
        __w.MarkClause(ClauseFlags.Where);
        return Predicates.Group<SelectWhereStage>(ref __w, "WHERE", predicate, false);
    }
    public SelectWhereStage WhereNot( SqlPredicateFn predicate )
    {
        __w.MarkClause(ClauseFlags.Where);
        return Predicates.Group<SelectWhereStage>(ref __w, "WHERE", predicate, true);
    }
    public SelectWhereStage WhereExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.MarkClause(ClauseFlags.Where);
        return Predicates.Exists<SelectWhereStage, TEnd>(ref __w, "WHERE", query, false);
    }
    public SelectWhereStage WhereNotExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.MarkClause(ClauseFlags.Where);
        return Predicates.Exists<SelectWhereStage, TEnd>(ref __w, "WHERE", query, true);
    }



    // ---- GROUP BY / WINDOW / ORDER BY ----

    public GroupByStage GroupBy( params ReadOnlySpan<string> columns )
    {
        Clauses.GroupBy(ref __w, columns);
        return new GroupByStage(__w);
    }
    public GroupByStage GroupBy<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T>
    {
        Clauses.GroupByTyped<T>(ref __w, propertyNames);
        return new GroupByStage(__w);
    }
    public GroupByStage GroupByExpr( params ReadOnlySpan<SqlExprFn> expressions )
    {
        Clauses.GroupByExpr(ref __w, expressions);
        return new GroupByStage(__w);
    }
    public GroupByStage GroupByRollup( params ReadOnlySpan<string> columns )
    {
        Clauses.Rollup(ref __w, columns);
        return new GroupByStage(__w);
    }
    public GroupByStage GroupByCube( params ReadOnlySpan<string> columns )
    {
        Clauses.Cube(ref __w, columns);
        return new GroupByStage(__w);
    }
    public GroupByStage GroupByGroupingSets( params ReadOnlySpan<string[]> sets )
    {
        Clauses.GroupingSets(ref __w, sets);
        return new GroupByStage(__w);
    }

    public WindowStage Window( string name, SqlWindowFn window )
    {
        Clauses.Window(ref __w, name, window);
        return new WindowStage(__w);
    }

    public OrderStage OrderBy( string     column ) => Clauses.OrderBy(ref __w, column, false);
    public OrderStage OrderByDesc( string column ) => Clauses.OrderBy(ref __w, column, true);
    public OrderStage OrderBy<T>( string propertyName )
        where T : ISqlTable<T> => Clauses.OrderByTyped<T>(ref __w, propertyName, false);
    public OrderStage OrderByDesc<T>( string propertyName )
        where T : ISqlTable<T> => Clauses.OrderByTyped<T>(ref __w, propertyName, true);
    public OrderStage OrderBy( SqlExprFn expression, bool descending = false ) => Clauses.OrderByExpr(ref __w, expression, descending);


    // ---- paging / set operations / locking ----

    public TailStage Limit( long count )
    {
        Paging.Limit(ref __w, count);
        return new TailStage(__w);
    }
    public TailStage Offset( long count )
    {
        Paging.Offset(ref __w, count);
        return new TailStage(__w);
    }
    public TailStage FetchNext( long count )
    {
        Paging.FetchNext(ref __w, count);
        return new TailStage(__w);
    }

    public SqlRoot Union()        => Clauses.SetOperation(ref __w, "UNION",     false);
    public SqlRoot UnionAll()     => Clauses.SetOperation(ref __w, "UNION",     true);
    public SqlRoot Intersect()    => Clauses.SetOperation(ref __w, "INTERSECT", false);
    public SqlRoot IntersectAll() => Clauses.SetOperation(ref __w, "INTERSECT", true);
    public SqlRoot Except()       => Clauses.SetOperation(ref __w, "EXCEPT",    false);
    public SqlRoot ExceptAll()    => Clauses.SetOperation(ref __w, "EXCEPT",    true);

    public LockStage ForUpdate() => Clauses.Lock(ref __w, SqlLockMode.Update);
    public LockStage ForShare()  => Clauses.Lock(ref __w, SqlLockMode.Share);

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
    SqlWriter ISqlQueryEnd.EndQuery()                               => __w;
}



/// <summary> Awaiting a join's <c>ON</c> predicate (or <c>USING</c>). </summary>
public ref struct JoinStage
{
    private SqlWriter __w;

    internal JoinStage( SqlWriter writer ) => __w = writer;

    public SqlComparison<JoinOnStage> On( string column ) => Joins.On(ref __w, column);
    public SqlComparison<JoinOnStage> On<T>( string propertyName )
        where T : ISqlTable<T> => Joins.On<T>(ref __w, propertyName);
    public SqlComparison<JoinOnStage> On( SqlExprFn expression ) => Joins.On(ref __w, expression);
    public JoinOnStage On( SqlPredicateFn predicate ) => Joins.On(ref __w, predicate);

    /// <summary> <c>USING (columns)</c> (not SQL Server). </summary>
    public FromStage Using( params ReadOnlySpan<string> columns ) => Joins.Using(ref __w, columns);
}



/// <summary>
///     Awaiting the <c>ON</c> predicate of a typed join. The bare <see cref="On(string)"/> resolves a property of the joined record <typeparamref name="T"/> (so
///     <c>.LeftJoin&lt;Order&gt;("o").On(nameof(Order.UserID))</c> writes <c>o.user_id</c>); anything else (e.g. <c>"o.user_id"</c>) is a raw column.
/// </summary>
public ref struct JoinStage<T>
    where T : ISqlTable<T>
{
    private SqlWriter __w;

    internal JoinStage( SqlWriter writer ) => __w = writer;

    public SqlComparison<JoinOnStage> On( string propertyOrColumn ) => T.TrySqlColumn(propertyOrColumn, out ISqlColumn? column) && column is not null
                                                                          ? Joins.On<T>(ref __w, propertyOrColumn)
                                                                          : Joins.On(ref __w, propertyOrColumn);
    public SqlComparison<JoinOnStage> On<TOther>( string propertyName )
        where TOther : ISqlTable<TOther> => Joins.On<TOther>(ref __w, propertyName);
    public SqlComparison<JoinOnStage> On( SqlExprFn expression ) => Joins.On(ref __w, expression);
    public JoinOnStage On( SqlPredicateFn predicate ) => Joins.On(ref __w, predicate);

    public FromStage Using( params ReadOnlySpan<string> columns ) => Joins.Using(ref __w, columns);
}



internal static class Joins
{
    private static void Mark( scoped ref SqlWriter writer ) => writer.MarkClause(ClauseFlags.On);

    public static SqlComparison<JoinOnStage> On( scoped ref SqlWriter writer, string column )
    {
        Mark(ref writer);
        return Predicates.Column<JoinOnStage>(ref writer, "ON", column);
    }
    public static SqlComparison<JoinOnStage> On<T>( scoped ref SqlWriter writer, string propertyName )
        where T : ISqlTable<T>
    {
        Mark(ref writer);
        return Predicates.Typed<JoinOnStage, T>(ref writer, "ON", propertyName);
    }
    public static SqlComparison<JoinOnStage> On( scoped ref SqlWriter writer, SqlExprFn expression )
    {
        Mark(ref writer);
        return Predicates.Expr<JoinOnStage>(ref writer, "ON", expression);
    }
    public static JoinOnStage On( scoped ref SqlWriter writer, SqlPredicateFn predicate )
    {
        Mark(ref writer);
        writer.Word("ON");
        Predicates.Write(ref writer, predicate);
        return new JoinOnStage(writer);
    }
    public static FromStage Using( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns )
    {
        writer.Require(writer.Dialect.SupportsUsingJoin(), "JOIN ... USING");
        writer.RequireItems(columns.Length, "USING");
        writer.Word("USING");
        Clauses.WriteIdentifiers(ref writer, columns);
        return new FromStage(writer);
    }
}



/// <summary> After a join's <c>ON</c> predicate: extend it with <c>AND</c> / <c>OR</c>, or carry on as after <c>FROM</c>. </summary>
public ref struct JoinOnStage : ISqlContinuation<JoinOnStage>, ISqlQueryEnd
{
    private SqlWriter __w;

    internal JoinOnStage( SqlWriter writer ) => __w = writer;

    static JoinOnStage ISqlContinuation<JoinOnStage>.Continue( SqlWriter writer ) => new(writer);

    private readonly FromStage From => new(__w);

    // ---- extend the ON predicate ----

    public SqlComparison<JoinOnStage> And( string column ) => Predicates.Column<JoinOnStage>(ref __w, "AND", column);
    public SqlComparison<JoinOnStage> Or( string  column ) => Predicates.Column<JoinOnStage>(ref __w, "OR",  column);
    public SqlComparison<JoinOnStage> And<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<JoinOnStage, T>(ref __w, "AND", propertyName);
    public SqlComparison<JoinOnStage> Or<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<JoinOnStage, T>(ref __w, "OR", propertyName);
    public SqlComparison<JoinOnStage> AndExpr( SqlExprFn expression ) => Predicates.Expr<JoinOnStage>(ref __w, "AND", expression);
    public SqlComparison<JoinOnStage> OrExpr( SqlExprFn  expression ) => Predicates.Expr<JoinOnStage>(ref __w, "OR",  expression);
    public JoinOnStage And( SqlPredicateFn predicate ) => Predicates.Group<JoinOnStage>(ref __w, "AND", predicate, false);
    public JoinOnStage Or( SqlPredicateFn  predicate ) => Predicates.Group<JoinOnStage>(ref __w, "OR",  predicate, false);


    // ---- everything after FROM ----

    public JoinStage InnerJoin( string      table, string? alias = null ) => From.InnerJoin(table, alias);
    public JoinStage LeftJoin( string       table, string? alias = null ) => From.LeftJoin(table, alias);
    public JoinStage RightJoin( string      table, string? alias = null ) => From.RightJoin(table, alias);
    public JoinStage FullJoin( string       table, string? alias = null ) => From.FullJoin(table, alias);
    public JoinStage LeftOuterJoin( string  table, string? alias = null ) => From.LeftOuterJoin(table, alias);
    public JoinStage RightOuterJoin( string table, string? alias = null ) => From.RightOuterJoin(table, alias);
    public JoinStage FullOuterJoin( string  table, string? alias = null ) => From.FullOuterJoin(table, alias);
    public JoinStage<T> InnerJoin<T>( string? alias = null )
        where T : ISqlTable<T> => From.InnerJoin<T>(alias);
    public JoinStage<T> LeftJoin<T>( string? alias = null )
        where T : ISqlTable<T> => From.LeftJoin<T>(alias);
    public JoinStage<T> RightJoin<T>( string? alias = null )
        where T : ISqlTable<T> => From.RightJoin<T>(alias);
    public JoinStage<T> FullJoin<T>( string? alias = null )
        where T : ISqlTable<T> => From.FullJoin<T>(alias);
    public JoinStage<T> LeftOuterJoin<T>( string? alias = null )
        where T : ISqlTable<T> => From.LeftOuterJoin<T>(alias);
    public JoinStage<T> RightOuterJoin<T>( string? alias = null )
        where T : ISqlTable<T> => From.RightOuterJoin<T>(alias);
    public JoinStage<T> FullOuterJoin<T>( string? alias = null )
        where T : ISqlTable<T> => From.FullOuterJoin<T>(alias);
    public JoinStage InnerJoin<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct => From.InnerJoin(query, alias);
    public JoinStage LeftJoin<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct => From.LeftJoin(query, alias);
    public FromStage CrossJoin( string table, string? alias = null ) => From.CrossJoin(table, alias);
    public FromStage CrossJoin<T>( string? alias = null )
        where T : ISqlTable<T> => From.CrossJoin<T>(alias);
    public FromStage NaturalJoin( string     table, string? alias = null ) => From.NaturalJoin(table, alias);
    public FromStage NaturalLeftJoin( string table, string? alias = null ) => From.NaturalLeftJoin(table, alias);
    public FromStage AndFrom( string         table, string? alias = null ) => From.AndFrom(table, alias);
    public FromStage AndFrom<T>( string? alias = null )
        where T : ISqlTable<T> => From.AndFrom<T>(alias);
    public FromStage CrossApply<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct => From.CrossApply(query, alias);
    public FromStage OuterApply<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct => From.OuterApply(query, alias);

    public SqlComparison<SelectWhereStage> Where( string column ) => From.Where(column);
    public SqlComparison<SelectWhereStage> Where<T>( string propertyName )
        where T : ISqlTable<T> => From.Where<T>(propertyName);
    public SqlComparison<SelectWhereStage> Where( SqlExprFn expression ) => From.Where(expression);
    public SelectWhereStage Where( SqlPredicateFn    predicate ) => From.Where(predicate);
    public SelectWhereStage WhereNot( SqlPredicateFn predicate ) => From.WhereNot(predicate);
    public SelectWhereStage WhereExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => From.WhereExists(query);
    public SelectWhereStage WhereNotExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => From.WhereNotExists(query);

    public GroupByStage GroupBy( params ReadOnlySpan<string> columns ) => From.GroupBy(columns);
    public GroupByStage GroupBy<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T> => From.GroupBy<T>(propertyNames);
    public GroupByStage GroupByExpr( params ReadOnlySpan<SqlExprFn> expressions ) => From.GroupByExpr(expressions);
    public GroupByStage GroupByRollup( params ReadOnlySpan<string>   columns )     => From.GroupByRollup(columns);
    public GroupByStage GroupByCube( params ReadOnlySpan<string>     columns )     => From.GroupByCube(columns);
    public GroupByStage GroupByGroupingSets( params ReadOnlySpan<string[]> sets )  => From.GroupByGroupingSets(sets);
    public WindowStage  Window( string name, SqlWindowFn window ) => From.Window(name, window);

    public OrderStage OrderBy( string     column ) => From.OrderBy(column);
    public OrderStage OrderByDesc( string column ) => From.OrderByDesc(column);
    public OrderStage OrderBy<T>( string propertyName )
        where T : ISqlTable<T> => From.OrderBy<T>(propertyName);
    public OrderStage OrderByDesc<T>( string propertyName )
        where T : ISqlTable<T> => From.OrderByDesc<T>(propertyName);
    public OrderStage OrderBy( SqlExprFn expression, bool descending = false ) => From.OrderBy(expression, descending);

    public TailStage Limit( long     count ) => From.Limit(count);
    public TailStage Offset( long    count ) => From.Offset(count);
    public TailStage FetchNext( long count ) => From.FetchNext(count);

    public SqlRoot Union()        => From.Union();
    public SqlRoot UnionAll()     => From.UnionAll();
    public SqlRoot Intersect()    => From.Intersect();
    public SqlRoot IntersectAll() => From.IntersectAll();
    public SqlRoot Except()       => From.Except();
    public SqlRoot ExceptAll()    => From.ExceptAll();

    public LockStage ForUpdate() => From.ForUpdate();
    public LockStage ForShare()  => From.ForShare();

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
    SqlWriter ISqlQueryEnd.EndQuery()                               => __w;
}
