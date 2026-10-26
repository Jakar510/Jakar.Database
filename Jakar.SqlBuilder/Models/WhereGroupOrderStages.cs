// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> After a complete <c>WHERE</c> predicate: chain more with <c>AND</c>/<c>OR</c>, or move on to grouping, windows, ordering, paging, set operations or locking. </summary>
public ref struct SelectWhereStage : ISqlContinuation<SelectWhereStage>, ISqlQueryEnd
{
    private SqlWriter __w;

    internal SelectWhereStage( SqlWriter writer ) => __w = writer;

    static SelectWhereStage ISqlContinuation<SelectWhereStage>.Continue( SqlWriter writer ) => new(writer);

    // ---- AND / OR ----

    public SqlComparison<SelectWhereStage> And( string column ) => Predicates.Column<SelectWhereStage>(ref __w, "AND", column);
    public SqlComparison<SelectWhereStage> Or( string  column ) => Predicates.Column<SelectWhereStage>(ref __w, "OR",  column);
    public SqlComparison<SelectWhereStage> And<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<SelectWhereStage, T>(ref __w, "AND", propertyName);
    public SqlComparison<SelectWhereStage> Or<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<SelectWhereStage, T>(ref __w, "OR", propertyName);
    public SqlComparison<SelectWhereStage> AndExpr( SqlExprFn expression ) => Predicates.Expr<SelectWhereStage>(ref __w, "AND", expression);
    public SqlComparison<SelectWhereStage> OrExpr( SqlExprFn  expression ) => Predicates.Expr<SelectWhereStage>(ref __w, "OR",  expression);

    public SelectWhereStage And( SqlPredicateFn    predicate ) => Predicates.Group<SelectWhereStage>(ref __w, "AND", predicate, false);
    public SelectWhereStage Or( SqlPredicateFn     predicate ) => Predicates.Group<SelectWhereStage>(ref __w, "OR",  predicate, false);
    public SelectWhereStage AndNot( SqlPredicateFn predicate ) => Predicates.Group<SelectWhereStage>(ref __w, "AND", predicate, true);
    public SelectWhereStage OrNot( SqlPredicateFn  predicate ) => Predicates.Group<SelectWhereStage>(ref __w, "OR",  predicate, true);
    public SelectWhereStage AndExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<SelectWhereStage, TEnd>(ref __w, "AND", query, false);
    public SelectWhereStage AndNotExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<SelectWhereStage, TEnd>(ref __w, "AND", query, true);
    public SelectWhereStage OrExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<SelectWhereStage, TEnd>(ref __w, "OR", query, false);


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


    // ---- paging / set operations / locking / terminals ----

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



/// <summary> Writes a <c>HAVING</c> predicate: <c>.Having(h =&gt; h.Count("o.id").GreaterThan(5))</c>. </summary>
public delegate HavingChain SqlHavingFn( HavingStage having );



/// <summary> After <c>GROUP BY</c>: optionally <c>HAVING</c>, then windows, ordering, paging, set operations, terminate. </summary>
public ref struct GroupByStage : ISqlQueryEnd
{
    private SqlWriter __w;

    internal GroupByStage( SqlWriter writer ) => __w = writer;

    /// <summary> <c>HAVING</c>, chain style: <c>.Having().Count("id").GreaterThan(5)</c>. </summary>
    public HavingStage Having()
    {
        __w.Word("HAVING");
        __w.MarkClause(ClauseFlags.Having);
        return new HavingStage(__w);
    }

    /// <summary> <c>HAVING</c>, lambda style: <c>.Having(h =&gt; h.Count("id").GreaterThan(5).And().Sum("total").LessThan(100))</c>. </summary>
    public HavingChain Having( SqlHavingFn having )
    {
        ArgumentNullException.ThrowIfNull(having);
        __w.Word("HAVING");
        __w.MarkClause(ClauseFlags.Having);
        int         start  = __w.Length;
        HavingChain result = having(new HavingStage(__w));
        if ( result.Writer.Length <= start ) { throw result.Writer.Error(SqlBuildError.EmptyPredicate, "HAVING wrote no predicate."); }

        return result;
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

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
    SqlWriter ISqlQueryEnd.EndQuery()                               => __w;
}



/// <summary> The left-hand side of a <c>HAVING</c> predicate (typically an aggregate). </summary>
public ref struct HavingStage
{
    private SqlWriter __w;

    internal HavingStage( SqlWriter writer ) => __w = writer;

    internal readonly SqlWriter Writer => __w;

    private SqlComparison<HavingChain> Left( int start ) => new(__w, start);

    private int Begin()
    {
        __w.Separate();
        return __w.Length;
    }

    public SqlComparison<HavingChain> Column( string column )
    {
        int start = Begin();
        __w.ColumnRef(column);
        return Left(start);
    }
    public SqlComparison<HavingChain> Column<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<HavingChain, T>(ref __w, null, propertyName);
    public SqlComparison<HavingChain> Expr( SqlExprFn expression ) => Predicates.Expr<HavingChain>(ref __w, null, expression);

    public SqlComparison<HavingChain> Count( string column = "*" ) => Aggregate("COUNT", column, false);
    public SqlComparison<HavingChain> Sum( string   column ) => Aggregate("SUM", column, false);
    public SqlComparison<HavingChain> Avg( string   column ) => Aggregate("AVG", column, false);
    public SqlComparison<HavingChain> Min( string   column ) => Aggregate("MIN", column, false);
    public SqlComparison<HavingChain> Max( string   column ) => Aggregate("MAX", column, false);
    public SqlComparison<HavingChain> CountDistinct( string column ) => Aggregate("COUNT", column, true);

    public SqlComparison<HavingChain> Count<T>( string propertyName )
        where T : ISqlTable<T> => AggregateTyped<T>("COUNT", propertyName);
    public SqlComparison<HavingChain> Sum<T>( string propertyName )
        where T : ISqlTable<T> => AggregateTyped<T>("SUM", propertyName);
    public SqlComparison<HavingChain> Avg<T>( string propertyName )
        where T : ISqlTable<T> => AggregateTyped<T>("AVG", propertyName);
    public SqlComparison<HavingChain> Min<T>( string propertyName )
        where T : ISqlTable<T> => AggregateTyped<T>("MIN", propertyName);
    public SqlComparison<HavingChain> Max<T>( string propertyName )
        where T : ISqlTable<T> => AggregateTyped<T>("MAX", propertyName);

    public HavingChain Group( SqlPredicateFn predicate ) => Predicates.Group<HavingChain>(ref __w, null, predicate, false);
    public HavingChain Not( SqlPredicateFn   predicate ) => Predicates.Group<HavingChain>(ref __w, null, predicate, true);

    private SqlComparison<HavingChain> Aggregate( string func, string column, bool distinct )
    {
        int start = Begin();
        __w.Word(func);
        __w.OpenCall();
        if ( distinct ) { __w.Word("DISTINCT"); }

        if ( column is "*" ) { __w.Append('*'); }
        else { __w.ColumnRef(column); }

        __w.CloseParen();
        __w.ClearCompared();
        return Left(start);
    }
    private SqlComparison<HavingChain> AggregateTyped<T>( string func, string propertyName )
        where T : ISqlTable<T>
    {
        int start = Begin();
        __w.Word(func);
        __w.OpenCall();
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.CloseParen();
        __w.ClearCompared();
        return Left(start);
    }
}



/// <summary> After a complete <c>HAVING</c> predicate: chain another with <c>And()</c> / <c>Or()</c>, or carry on to windows, ordering, paging, set operations, terminate. </summary>
public ref struct HavingChain : ISqlContinuation<HavingChain>, ISqlQueryEnd
{
    private SqlWriter __w;

    internal HavingChain( SqlWriter writer ) => __w = writer;

    internal readonly SqlWriter Writer => __w;

    static HavingChain ISqlContinuation<HavingChain>.Continue( SqlWriter writer ) => new(writer);

    public HavingStage And()
    {
        __w.Word("AND");
        return new HavingStage(__w);
    }
    public HavingStage Or()
    {
        __w.Word("OR");
        return new HavingStage(__w);
    }
    public HavingChain And( SqlPredicateFn predicate ) => Predicates.Group<HavingChain>(ref __w, "AND", predicate, false);
    public HavingChain Or( SqlPredicateFn  predicate ) => Predicates.Group<HavingChain>(ref __w, "OR",  predicate, false);

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

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
    SqlWriter ISqlQueryEnd.EndQuery()                               => __w;
}



/// <summary> After <c>WINDOW name AS (...)</c>: more windows, ordering, paging, set operations, terminate. </summary>
public ref struct WindowStage : ISqlQueryEnd
{
    private SqlWriter __w;

    internal WindowStage( SqlWriter writer ) => __w = writer;

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

    public SqlRoot Union()    => Clauses.SetOperation(ref __w, "UNION", false);
    public SqlRoot UnionAll() => Clauses.SetOperation(ref __w, "UNION", true);

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
    SqlWriter ISqlQueryEnd.EndQuery()                               => __w;
}



/// <summary> After an <c>ORDER BY</c> key: direction, <c>NULLS FIRST/LAST</c>, more keys, paging, locking, terminate. </summary>
public ref struct OrderStage : ISqlQueryEnd
{
    private SqlWriter __w;

    internal OrderStage( SqlWriter writer ) => __w = writer;

    public OrderStage Asc()
    {
        __w.Word("ASC");
        return new OrderStage(__w);
    }
    public OrderStage Desc()
    {
        __w.Word("DESC");
        return new OrderStage(__w);
    }

    /// <summary> <c>NULLS FIRST</c>; SQL Server and MySQL get an equivalent extra sort key in front of this one. </summary>
    public OrderStage NullsFirst()
    {
        __w.NullsOrder(true);
        return new OrderStage(__w);
    }
    public OrderStage NullsLast()
    {
        __w.NullsOrder(false);
        return new OrderStage(__w);
    }

    public OrderStage ThenBy( string     column ) => Clauses.OrderBy(ref __w, column, false);
    public OrderStage ThenByDesc( string column ) => Clauses.OrderBy(ref __w, column, true);
    public OrderStage ThenBy<T>( string propertyName )
        where T : ISqlTable<T> => Clauses.OrderByTyped<T>(ref __w, propertyName, false);
    public OrderStage ThenByDesc<T>( string propertyName )
        where T : ISqlTable<T> => Clauses.OrderByTyped<T>(ref __w, propertyName, true);
    public OrderStage ThenBy( SqlExprFn expression, bool descending = false ) => Clauses.OrderByExpr(ref __w, expression, descending);

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

    public LockStage ForUpdate() => Clauses.Lock(ref __w, SqlLockMode.Update);
    public LockStage ForShare()  => Clauses.Lock(ref __w, SqlLockMode.Share);

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
    SqlWriter ISqlQueryEnd.EndQuery()                               => __w;
}



/// <summary> After paging: more paging, ordering, locking, terminate. </summary>
public ref struct TailStage : ISqlQueryEnd
{
    private SqlWriter __w;

    internal TailStage( SqlWriter writer ) => __w = writer;

    public OrderStage OrderBy( string     column ) => Clauses.OrderBy(ref __w, column, false);
    public OrderStage OrderByDesc( string column ) => Clauses.OrderBy(ref __w, column, true);

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

    public LockStage ForUpdate() => Clauses.Lock(ref __w, SqlLockMode.Update);
    public LockStage ForShare()  => Clauses.Lock(ref __w, SqlLockMode.Share);

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
    SqlWriter ISqlQueryEnd.EndQuery()                               => __w;
}



/// <summary> After <c>FOR UPDATE</c> / <c>FOR SHARE</c> (written after paging when the query is built): <c>NOWAIT</c> / <c>SKIP LOCKED</c>, terminate. </summary>
public ref struct LockStage
{
    private SqlWriter __w;

    internal LockStage( SqlWriter writer ) => __w = writer;

    public LockStage NoWait()
    {
        __w.SetLock(SqlLockMode.NoWait);
        return new LockStage(__w);
    }
    public LockStage SkipLocked()
    {
        __w.SetLock(SqlLockMode.SkipLocked);
        return new LockStage(__w);
    }

    // Paging is recorded and written before the lock, so it may come after it in the chain.
    public LockStage Limit( long count )
    {
        Paging.Limit(ref __w, count);
        return new LockStage(__w);
    }
    public LockStage Offset( long count )
    {
        Paging.Offset(ref __w, count);
        return new LockStage(__w);
    }

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
}
