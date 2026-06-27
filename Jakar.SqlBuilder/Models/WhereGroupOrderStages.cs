// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Shared GROUP BY / ORDER BY emission. </summary>
internal static class Tail
{
    public static void GroupBy( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns )
    {
        writer.Word("GROUP BY");
        bool has = false;
        foreach ( string column in columns )
        {
            if ( has ) { writer.Comma(); }

            writer.ColumnRef(column);
            has = true;
        }

        writer.MarkClause(ClauseFlags.GroupBy);
    }
    public static void GroupByTyped<T>( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T>
    {
        writer.Word("GROUP BY");
        bool has = false;
        foreach ( string propertyName in propertyNames )
        {
            if ( has ) { writer.Comma(); }

            SqlOps.TypedColumn<T>(ref writer, propertyName);
            has = true;
        }

        writer.MarkClause(ClauseFlags.GroupBy);
    }

    public static OrderStage OrderBy( scoped ref SqlWriter writer, string column, bool descending )
    {
        writer.Word("ORDER BY");
        writer.ColumnRef(column);
        if ( descending ) { writer.Word("DESC"); }

        writer.MarkClause(ClauseFlags.OrderBy);
        return new OrderStage(writer);
    }
    public static OrderStage OrderByTyped<T>( scoped ref SqlWriter writer, string propertyName, bool descending )
        where T : ISqlTable<T>
    {
        writer.Word("ORDER BY");
        SqlOps.TypedColumn<T>(ref writer, propertyName);
        if ( descending ) { writer.Word("DESC"); }

        writer.MarkClause(ClauseFlags.OrderBy);
        return new OrderStage(writer);
    }
}



/// <summary> The right-hand side of a <c>WHERE</c>/<c>AND</c>/<c>OR</c> predicate. </summary>
public ref struct SelectWhereComparison
{
    private SqlWriter __w;

    internal SelectWhereComparison( SqlWriter writer ) => __w = writer;

    private SelectWhereStage Done() => new(__w);

    public SelectWhereStage EqualTo( long     value ) { SqlOps.Op(ref __w, SqlOps.EQUAL, value); return Done(); }
    public SelectWhereStage EqualTo( bool     value ) { SqlOps.Op(ref __w, SqlOps.EQUAL, value); return Done(); }
    public SelectWhereStage EqualTo( SqlValue value ) { SqlOps.Op(ref __w, SqlOps.EQUAL, value); return Done(); }
    public SelectWhereStage EqualTo( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.EQUAL, param); return Done(); }

    public SelectWhereStage NotEqualTo( long    value ) { SqlOps.Op(ref __w, SqlOps.NOT_EQUAL, value); return Done(); }
    public SelectWhereStage NotEqualTo( SqlValue value ) { SqlOps.Op(ref __w, SqlOps.NOT_EQUAL, value); return Done(); }
    public SelectWhereStage NotEqualTo( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.NOT_EQUAL, param); return Done(); }

    public SelectWhereStage GreaterThan( long    value ) { SqlOps.Op(ref __w, SqlOps.GREATER, value); return Done(); }
    public SelectWhereStage GreaterThan( SqlValue value ) { SqlOps.Op(ref __w, SqlOps.GREATER, value); return Done(); }
    public SelectWhereStage GreaterThan( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.GREATER, param); return Done(); }

    public SelectWhereStage GreaterOrEqual( long    value ) { SqlOps.Op(ref __w, SqlOps.GREATER_EQUAL, value); return Done(); }
    public SelectWhereStage GreaterOrEqual( SqlValue value ) { SqlOps.Op(ref __w, SqlOps.GREATER_EQUAL, value); return Done(); }
    public SelectWhereStage GreaterOrEqual( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.GREATER_EQUAL, param); return Done(); }

    public SelectWhereStage LessThan( long    value ) { SqlOps.Op(ref __w, SqlOps.LESS, value); return Done(); }
    public SelectWhereStage LessThan( SqlValue value ) { SqlOps.Op(ref __w, SqlOps.LESS, value); return Done(); }
    public SelectWhereStage LessThan( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.LESS, param); return Done(); }

    public SelectWhereStage LessOrEqual( long    value ) { SqlOps.Op(ref __w, SqlOps.LESS_EQUAL, value); return Done(); }
    public SelectWhereStage LessOrEqual( SqlValue value ) { SqlOps.Op(ref __w, SqlOps.LESS_EQUAL, value); return Done(); }
    public SelectWhereStage LessOrEqual( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.LESS_EQUAL, param); return Done(); }

    public SelectWhereStage EqualToColumn( string rawColumn ) { SqlOps.OpColumn(ref __w, SqlOps.EQUAL, rawColumn); return Done(); }
    public SelectWhereStage EqualToColumn<T>( string propertyName ) where T : ISqlTable<T> { SqlOps.OpTypedColumn<T>(ref __w, SqlOps.EQUAL, propertyName); return Done(); }

    public SelectWhereStage IsNull()    { SqlOps.Null(ref __w, false); return Done(); }
    public SelectWhereStage IsNotNull() { SqlOps.Null(ref __w, true);  return Done(); }

    public SelectWhereStage Like( string   inlinePattern ) { SqlOps.Like(ref __w, SqlValue.Inline(inlinePattern)); return Done(); }
    public SelectWhereStage Like( SqlValue pattern )       { SqlOps.Like(ref __w, pattern); return Done(); }

    public SelectWhereStage In( params ReadOnlySpan<long> values ) { SqlOps.In(ref __w, values); return Done(); }
    public SelectWhereStage InParams( params object?[]    values ) { SqlOps.InParams(ref __w, values); return Done(); }

    public SelectWhereStage Between( long low, long high ) { SqlOps.Between(ref __w, low, high); return Done(); }
}



/// <summary> After a complete predicate: chain more with <c>AND</c>/<c>OR</c>, or move on to grouping/ordering/paging/set-ops. </summary>
public ref struct SelectWhereStage
{
    private SqlWriter __w;

    internal SelectWhereStage( SqlWriter writer ) => __w = writer;

    public SelectWhereComparison And( string column )
    {
        __w.Word("AND");
        __w.ColumnRef(column);
        return new SelectWhereComparison(__w);
    }
    public SelectWhereComparison Or( string column )
    {
        __w.Word("OR");
        __w.ColumnRef(column);
        return new SelectWhereComparison(__w);
    }
    public SelectWhereComparison And<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word("AND");
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        return new SelectWhereComparison(__w);
    }
    public SelectWhereComparison Or<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word("OR");
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        return new SelectWhereComparison(__w);
    }

    public GroupByStage GroupBy( params ReadOnlySpan<string> columns )
    {
        Tail.GroupBy(ref __w, columns);
        return new GroupByStage(__w);
    }
    public GroupByStage GroupBy<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T>
    {
        Tail.GroupByTyped<T>(ref __w, propertyNames);
        return new GroupByStage(__w);
    }

    public OrderStage OrderBy( string     column ) => Tail.OrderBy(ref __w, column, false);
    public OrderStage OrderByDesc( string column ) => Tail.OrderBy(ref __w, column, true);
    public OrderStage OrderBy<T>( string     propertyName ) where T : ISqlTable<T> => Tail.OrderByTyped<T>(ref __w, propertyName, false);
    public OrderStage OrderByDesc<T>( string propertyName ) where T : ISqlTable<T> => Tail.OrderByTyped<T>(ref __w, propertyName, true);

    public TailStage Limit( long count )    { Paging.Limit(ref __w, count);     return new TailStage(__w); }
    public TailStage Offset( long count )    { Paging.Offset(ref __w, count);    return new TailStage(__w); }
    public TailStage FetchNext( long count ) { Paging.FetchNext(ref __w, count); return new TailStage(__w); }

    public SqlRoot Union()    { __w.Word("UNION");     return new SqlRoot(__w); }
    public SqlRoot UnionAll() { __w.Word("UNION ALL"); return new SqlRoot(__w); }

    public SqlResult Build() => __w.Build();
}



/// <summary> After <c>GROUP BY</c>: optionally <c>HAVING</c>, then ordering/paging/set-ops/terminate. </summary>
public ref struct GroupByStage
{
    private SqlWriter __w;

    internal GroupByStage( SqlWriter writer ) => __w = writer;

    public HavingStage Having()
    {
        __w.Word("HAVING");
        __w.MarkClause(ClauseFlags.Having);
        return new HavingStage(__w);
    }

    public OrderStage OrderBy( string     column ) => Tail.OrderBy(ref __w, column, false);
    public OrderStage OrderByDesc( string column ) => Tail.OrderBy(ref __w, column, true);
    public OrderStage OrderBy<T>( string     propertyName ) where T : ISqlTable<T> => Tail.OrderByTyped<T>(ref __w, propertyName, false);
    public OrderStage OrderByDesc<T>( string propertyName ) where T : ISqlTable<T> => Tail.OrderByTyped<T>(ref __w, propertyName, true);

    public TailStage Limit( long count )    { Paging.Limit(ref __w, count);     return new TailStage(__w); }
    public TailStage Offset( long count )    { Paging.Offset(ref __w, count);    return new TailStage(__w); }
    public TailStage FetchNext( long count ) { Paging.FetchNext(ref __w, count); return new TailStage(__w); }

    public SqlRoot Union()    { __w.Word("UNION");     return new SqlRoot(__w); }
    public SqlRoot UnionAll() { __w.Word("UNION ALL"); return new SqlRoot(__w); }

    public SqlResult Build() => __w.Build();
}



/// <summary> Left-hand side of a <c>HAVING</c> predicate (typically an aggregate). </summary>
public ref struct HavingStage
{
    private SqlWriter __w;

    internal HavingStage( SqlWriter writer ) => __w = writer;

    public HavingComparison Column( string column )
    {
        __w.ColumnRef(column);
        return new HavingComparison(__w);
    }
    public HavingComparison Column<T>( string propertyName )
        where T : ISqlTable<T>
    {
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        return new HavingComparison(__w);
    }
    public HavingComparison Count( string column ) => Aggregate("COUNT", column);
    public HavingComparison Sum( string   column ) => Aggregate("SUM",   column);
    public HavingComparison Count<T>( string propertyName ) where T : ISqlTable<T> => AggregateTyped<T>("COUNT", propertyName);
    public HavingComparison Sum<T>( string   propertyName ) where T : ISqlTable<T> => AggregateTyped<T>("SUM",   propertyName);

    private HavingComparison Aggregate( string func, string column )
    {
        __w.Word(func);
        __w.OpenCall();
        if ( column is "*" ) { __w.Append('*'); }
        else { __w.ColumnRef(column); }

        __w.CloseParen();
        return new HavingComparison(__w);
    }
    private HavingComparison AggregateTyped<T>( string func, string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word(func);
        __w.OpenCall();
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.CloseParen();
        return new HavingComparison(__w);
    }
}



/// <summary> Right-hand side of a <c>HAVING</c> predicate. </summary>
public ref struct HavingComparison
{
    private SqlWriter __w;

    internal HavingComparison( SqlWriter writer ) => __w = writer;

    private TailStage Done() => new(__w);

    public TailStage GreaterThan( long    value ) { SqlOps.Op(ref __w, SqlOps.GREATER, value); return Done(); }
    public TailStage GreaterThan( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.GREATER, param); return Done(); }
    public TailStage LessThan( long    value ) { SqlOps.Op(ref __w, SqlOps.LESS, value); return Done(); }
    public TailStage LessThan( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.LESS, param); return Done(); }
    public TailStage EqualTo( long    value ) { SqlOps.Op(ref __w, SqlOps.EQUAL, value); return Done(); }
    public TailStage EqualTo( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.EQUAL, param); return Done(); }
    public TailStage GreaterOrEqual( long value ) { SqlOps.Op(ref __w, SqlOps.GREATER_EQUAL, value); return Done(); }
    public TailStage LessOrEqual( long    value ) { SqlOps.Op(ref __w, SqlOps.LESS_EQUAL, value); return Done(); }
}



/// <summary> After an <c>ORDER BY</c> column: direction, more keys, paging, terminate. </summary>
public ref struct OrderStage
{
    private SqlWriter __w;

    internal OrderStage( SqlWriter writer ) => __w = writer;

    public OrderStage Asc()  { __w.Word("ASC");  return new OrderStage(__w); }
    public OrderStage Desc() { __w.Word("DESC"); return new OrderStage(__w); }

    public OrderStage ThenBy( string     column ) { __w.Comma(); __w.ColumnRef(column); return new OrderStage(__w); }
    public OrderStage ThenByDesc( string column ) { __w.Comma(); __w.ColumnRef(column); __w.Word("DESC"); return new OrderStage(__w); }

    public TailStage Limit( long count )    { Paging.Limit(ref __w, count);     return new TailStage(__w); }
    public TailStage Offset( long count )    { Paging.Offset(ref __w, count);    return new TailStage(__w); }
    public TailStage FetchNext( long count ) { Paging.FetchNext(ref __w, count); return new TailStage(__w); }

    public SqlResult Build() => __w.Build();
}



/// <summary> Generic tail: ordering and paging available before termination. </summary>
public ref struct TailStage
{
    private SqlWriter __w;

    internal TailStage( SqlWriter writer ) => __w = writer;

    public OrderStage OrderBy( string     column ) => Tail.OrderBy(ref __w, column, false);
    public OrderStage OrderByDesc( string column ) => Tail.OrderBy(ref __w, column, true);

    public TailStage Offset( long count )    { Paging.Offset(ref __w, count);    return new TailStage(__w); }
    public TailStage FetchNext( long count ) { Paging.FetchNext(ref __w, count); return new TailStage(__w); }

    public SqlResult Build() => __w.Build();
}
