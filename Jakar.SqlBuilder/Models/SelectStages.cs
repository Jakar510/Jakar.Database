// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Dialect-aware paging emission. </summary>
internal static class Paging
{
    public static void Limit( scoped ref SqlWriter writer, long count )
    {
        switch ( writer.Dialect )
        {
            case SqlDialectKind.SqlServer:
                writer.Word("OFFSET");
                writer.InlineNumber(0);
                writer.Word("ROWS FETCH NEXT");
                writer.InlineNumber(count);
                writer.Word("ROWS ONLY");
                return;

            default:
                writer.Word("LIMIT");
                writer.InlineNumber(count);
                return;
        }
    }
    public static void Offset( scoped ref SqlWriter writer, long count )
    {
        writer.Word("OFFSET");
        writer.InlineNumber(count);
        if ( writer.Dialect is SqlDialectKind.SqlServer ) { writer.Word("ROWS"); }
    }
    public static void FetchNext( scoped ref SqlWriter writer, long count )
    {
        if ( writer.Dialect is SqlDialectKind.SqlServer )
        {
            writer.Word("FETCH NEXT");
            writer.InlineNumber(count);
            writer.Word("ROWS ONLY");
            return;
        }

        writer.Word("LIMIT");
        writer.InlineNumber(count);
    }
}



/// <summary> After <c>SELECT</c> projection. Add more projection items, then <c>FROM</c>. </summary>
public ref struct SelectStage
{
    private SqlWriter __w;
    private bool      __has;

    internal SelectStage( SqlWriter writer, bool hasProjection )
    {
        __w   = writer;
        __has = hasProjection;
    }

    public SelectStage Top( long count )
    {
        if ( __w.Dialect is SqlDialectKind.SqlServer )
        {
            __w.Word("TOP");
            __w.OpenCall();
            __w.InlineNumber(count);
            __w.CloseParen();
        }

        return new SelectStage(__w, __has);
    }

    public SelectStage Column( string name )
    {
        if ( __has ) { __w.Comma(); }

        if ( name is "*" ) { __w.Star(); }
        else { __w.ColumnRef(name); }

        return new SelectStage(__w, true);
    }
    public SelectStage Column<T>( string propertyName )
        where T : ISqlTable<T>
    {
        ISqlColumn column = TypedColumns.Resolve<T>(propertyName, in __w);
        if ( __has ) { __w.Comma(); }

        __w.QualifiedColumn(null, column.GetColumnName(__w.Dialect), true);
        return new SelectStage(__w, true);
    }
    public SelectStage As( string alias )
    {
        __w.Word("AS");
        __w.Identifier(alias);
        return new SelectStage(__w, __has);
    }

    public SelectStage Count( string column, string? @as = null ) => Aggregate("COUNT", column, @as);
    public SelectStage Sum( string   column, string? @as = null ) => Aggregate("SUM",   column, @as);
    public SelectStage Avg( string   column, string? @as = null ) => Aggregate("AVG",   column, @as);
    public SelectStage Min( string   column, string? @as = null ) => Aggregate("MIN",   column, @as);
    public SelectStage Max( string   column, string? @as = null ) => Aggregate("MAX",   column, @as);

    public SelectStage Count<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("COUNT", propertyName, @as);
    public SelectStage Sum<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("SUM", propertyName, @as);
    public SelectStage Avg<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("AVG", propertyName, @as);
    public SelectStage Min<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("MIN", propertyName, @as);
    public SelectStage Max<T>( string propertyName, string? @as = null )
        where T : ISqlTable<T> => AggregateTyped<T>("MAX", propertyName, @as);

    private SelectStage Aggregate( string func, string column, string? @as )
    {
        if ( __has ) { __w.Comma(); }

        __w.Word(func);
        __w.OpenCall();

        if ( column is "*" ) { __w.Append('*'); }
        else { __w.ColumnRef(column); }

        __w.CloseParen();
        AppendAlias(@as);
        return new SelectStage(__w, true);
    }
    private SelectStage AggregateTyped<T>( string func, string propertyName, string? @as )
        where T : ISqlTable<T>
    {
        ISqlColumn column = TypedColumns.Resolve<T>(propertyName, in __w);
        if ( __has ) { __w.Comma(); }

        __w.Word(func);
        __w.OpenCall();
        __w.QualifiedColumn(null, column.GetColumnName(__w.Dialect), true);
        __w.CloseParen();
        AppendAlias(@as);
        return new SelectStage(__w, true);
    }
    private void AppendAlias( string? @as )
    {
        if ( @as is not { Length: > 0 } ) { return; }

        __w.Word("AS");
        __w.Identifier(@as);
    }

    public FromStage From( string table, string? alias = null )
    {
        __w.Word("FROM");
        __w.Identifier(table);
        if ( alias is { Length: > 0 } ) { __w.Identifier(alias); }

        __w.MarkClause(ClauseFlags.From);
        return new FromStage(__w);
    }
    public FromStage From<T>( string? alias = null )
        where T : ISqlTable<T>
    {
        __w.Word("FROM");
        __w.Identifier(T.SqlTableName);
        if ( alias is { Length: > 0 } ) { __w.Identifier(alias); }

        __w.MarkClause(ClauseFlags.From);
        return new FromStage(__w);
    }

    public SqlResult Build() => __w.Build();
}



/// <summary> After <c>FROM</c>: joins, filtering, grouping, ordering, paging, set-ops, terminate. </summary>
public ref struct FromStage
{
    private SqlWriter __w;

    internal FromStage( SqlWriter writer ) => __w = writer;

    public JoinStage InnerJoin( string table, string? alias = null ) => Join("INNER JOIN", table, alias);
    public JoinStage LeftJoin( string  table, string? alias = null ) => Join("LEFT JOIN",  table, alias);
    public JoinStage RightJoin( string table, string? alias = null ) => Join("RIGHT JOIN", table, alias);
    public JoinStage FullJoin( string  table, string? alias = null ) => Join("FULL JOIN",  table, alias);
    public JoinStage InnerJoin<T>( string? alias = null )
        where T : ISqlTable<T> => Join("INNER JOIN", T.SqlTableName, alias);
    public JoinStage LeftJoin<T>( string? alias = null )
        where T : ISqlTable<T> => Join("LEFT JOIN", T.SqlTableName, alias);
    public JoinStage RightJoin<T>( string? alias = null )
        where T : ISqlTable<T> => Join("RIGHT JOIN", T.SqlTableName, alias);
    public JoinStage FullJoin<T>( string? alias = null )
        where T : ISqlTable<T> => Join("FULL JOIN", T.SqlTableName, alias);

    public FromStage CrossJoin( string table, string? alias = null )
    {
        __w.Word("CROSS JOIN");
        __w.Identifier(table);
        if ( alias is { Length: > 0 } ) { __w.Identifier(alias); }

        __w.MarkClause(ClauseFlags.Join);
        return new FromStage(__w);
    }

    private JoinStage Join( string keyword, string table, string? alias )
    {
        if ( keyword.StartsWith("FULL", StringComparison.Ordinal) && !SqlDialects.SupportsFullOuterJoin(__w.Dialect) ) { throw new SqlBuildException(SqlBuildError.UnsupportedFeature, $"FULL OUTER JOIN is not supported by {__w.Dialect}.", __w.Snapshot(), __w.Dialect); }

        __w.Word(keyword);
        __w.Identifier(table);
        if ( alias is { Length: > 0 } ) { __w.Identifier(alias); }

        __w.MarkClause(ClauseFlags.Join);
        return new JoinStage(__w);
    }

    public SelectWhereComparison Where( string column )
    {
        __w.Word("WHERE");
        __w.ColumnRef(column);
        __w.MarkClause(ClauseFlags.Where);
        return new SelectWhereComparison(__w);
    }
    public SelectWhereComparison Where<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word("WHERE");
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.MarkClause(ClauseFlags.Where);
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
    public OrderStage OrderBy<T>( string propertyName )
        where T : ISqlTable<T> => Tail.OrderByTyped<T>(ref __w, propertyName, false);
    public OrderStage OrderByDesc<T>( string propertyName )
        where T : ISqlTable<T> => Tail.OrderByTyped<T>(ref __w, propertyName, true);

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

    public SqlRoot Union()
    {
        __w.Word("UNION");
        return new SqlRoot(__w);
    }
    public SqlRoot UnionAll()
    {
        __w.Word("UNION ALL");
        return new SqlRoot(__w);
    }

    public SqlResult Build() => __w.Build();
}



/// <summary> Awaiting a join <c>ON</c> predicate. </summary>
public ref struct JoinStage
{
    private SqlWriter __w;

    internal JoinStage( SqlWriter writer ) => __w = writer;

    public JoinOnComparison On( string column )
    {
        __w.Word("ON");
        __w.ColumnRef(column);
        __w.MarkClause(ClauseFlags.On);
        return new JoinOnComparison(__w);
    }
    public JoinOnComparison On<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word("ON");
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.MarkClause(ClauseFlags.On);
        return new JoinOnComparison(__w);
    }
}



/// <summary> The right-hand side of a join <c>ON</c> predicate. </summary>
public ref struct JoinOnComparison
{
    private SqlWriter __w;

    internal JoinOnComparison( SqlWriter writer ) => __w = writer;

    public FromStage EqualToColumn( string rawColumn )
    {
        SqlOps.OpColumn(ref __w, SqlOps.EQUAL, rawColumn);
        return new FromStage(__w);
    }
    public FromStage EqualToColumn<T>( string propertyName )
        where T : ISqlTable<T>
    {
        SqlOps.OpTypedColumn<T>(ref __w, SqlOps.EQUAL, propertyName);
        return new FromStage(__w);
    }
    public FromStage EqualTo( long value )
    {
        SqlOps.Op(ref __w, SqlOps.EQUAL, value);
        return new FromStage(__w);
    }
    public FromStage EqualTo( object? param )
    {
        SqlOps.OpParam(ref __w, SqlOps.EQUAL, param);
        return new FromStage(__w);
    }
}
