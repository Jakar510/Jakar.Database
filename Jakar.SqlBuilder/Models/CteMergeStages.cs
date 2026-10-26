// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/25/2026

namespace Jakar.SqlBuilder;


/// <summary> After <c>WITH name [(columns)]</c>: the CTE body. </summary>
public ref struct CteNameStage
{
    private SqlWriter __w;

    internal CteNameStage( SqlWriter writer ) => __w = writer;

    /// <summary> <c>AS (query)</c>: <c>.As(q =&gt; q.Select("id").From("users"))</c>. </summary>
    public CteStage As<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Body("AS", query);

    /// <summary> <c>AS MATERIALIZED (query)</c> (PostgreSQL, SQLite). </summary>
    public CteStage AsMaterialized<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.Require(__w.Dialect.SupportsMaterializedCte(), "MATERIALIZED");
        return Body("AS MATERIALIZED", query);
    }

    /// <summary> <c>AS NOT MATERIALIZED (query)</c> (PostgreSQL, SQLite). </summary>
    public CteStage AsNotMaterialized<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.Require(__w.Dialect.SupportsMaterializedCte(), "NOT MATERIALIZED");
        return Body("AS NOT MATERIALIZED", query);
    }

    private CteStage Body<TEnd>( string keyword, SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.Word(keyword);
        Subqueries.Write(ref __w, query, true);
        return new CteStage(__w);
    }
}



/// <summary> After a CTE: another CTE, or the statement that uses them. </summary>
public ref struct CteStage
{
    private SqlWriter __w;

    internal CteStage( SqlWriter writer ) => __w = writer;

    private readonly SqlRoot Root => SqlRoot.Nested(__w);

    public CteNameStage With( string          name, params ReadOnlySpan<string> columns ) => Ctes.Start(ref __w, name, columns, false);
    public CteNameStage WithRecursive( string name, params ReadOnlySpan<string> columns ) => Ctes.Start(ref __w, name, columns, true);

    public SelectStage Select( params ReadOnlySpan<string>         columns ) => Root.Select(columns);
    public SelectStage SelectDistinct( params ReadOnlySpan<string> columns ) => Root.SelectDistinct(columns);
    public SelectStage Select<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T> => Root.Select<T>(propertyNames);

    public InsertStage Insert() => Root.Insert();
    public InsertColumnsStage Insert<T>()
        where T : ISqlTable<T> => Root.Insert<T>();
    public UpdateStage Update( string table, string? alias = null ) => Root.Update(table, alias);
    public UpdateStage Update<T>( string? alias = null )
        where T : ISqlTable<T> => Root.Update<T>(alias);
    public DeleteStage DeleteFrom( string table, string? alias = null ) => Root.DeleteFrom(table, alias);
    public DeleteStage Delete<T>( string? alias = null )
        where T : ISqlTable<T> => Root.Delete<T>(alias);
    public MergeStage MergeInto( string table, string alias ) => Root.MergeInto(table, alias);
}



internal static class Ctes
{
    public static CteNameStage Start( scoped ref SqlWriter writer, string name, scoped ReadOnlySpan<string> columns, bool recursive )
    {
        if ( writer.Has(ClauseFlags.With) ) { writer.Comma(); }
        else
        {
            writer.Word("WITH");
            writer.MarkWith();
            writer.MarkClause(ClauseFlags.With);
        }

        if ( recursive ) { writer.RequireRecursive(); }

        writer.Identifier(name);
        if ( columns.Length > 0 ) { Clauses.WriteIdentifiers(ref writer, columns); }

        return new CteNameStage(writer);
    }
}



/// <summary> After <c>MERGE INTO target alias</c>: the source. </summary>
public ref struct MergeStage
{
    private SqlWriter __w;

    internal MergeStage( SqlWriter writer ) => __w = writer;

    public MergeUsingStage Using( string table, string alias )
    {
        Clauses.Source(ref __w, "USING", table, SqlWriter.RequireName(alias, nameof(alias)));
        return new MergeUsingStage(__w);
    }
    public MergeUsingStage Using<T>( string alias )
        where T : ISqlTable<T> => Using(T.SqlTableName, alias);

    /// <summary> <c>USING (SELECT ...) alias</c>. </summary>
    public MergeUsingStage Using<TEnd>( SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        Clauses.DerivedTable(ref __w, "USING", query, alias);
        return new MergeUsingStage(__w);
    }
}



/// <summary> After <c>USING source</c>: the match condition. </summary>
public ref struct MergeUsingStage
{
    private SqlWriter __w;

    internal MergeUsingStage( SqlWriter writer ) => __w = writer;

    /// <summary> <c>ON (predicate)</c> (parenthesized, as Oracle requires). </summary>
    public MergeOnStage On( SqlPredicateFn predicate )
    {
        __w.Word("ON");
        __w.OpenParen();
        Predicates.Write(ref __w, predicate);
        __w.CloseParen();
        __w.MarkClause(ClauseFlags.On);
        return new MergeOnStage(__w);
    }
}



/// <summary> After <c>ON (...)</c>: at least one <c>WHEN</c> clause. </summary>
public ref struct MergeOnStage
{
    private SqlWriter __w;

    internal MergeOnStage( SqlWriter writer ) => __w = writer;

    /// <summary> <c>WHEN MATCHED [AND condition] THEN UPDATE SET ...</c> (Oracle: <c>... UPDATE SET ... WHERE condition</c>). </summary>
    public MergeWhenStage WhenMatchedThenUpdate( SqlSetFn set, SqlPredicateFn? and = null ) => Merges.MatchedUpdate(ref __w, set, and);

    /// <summary> <c>WHEN MATCHED [AND condition] THEN DELETE</c> (not Oracle). </summary>
    public MergeWhenStage WhenMatchedThenDelete( SqlPredicateFn? and = null ) => Merges.MatchedDelete(ref __w, and);

    /// <summary> <c>WHEN NOT MATCHED [AND condition] THEN INSERT (columns) VALUES (values)</c> (Oracle: <c>... VALUES (...) WHERE condition</c>). </summary>
    public MergeWhenStage WhenNotMatchedThenInsert( scoped ReadOnlySpan<string> columns, scoped ReadOnlySpan<SqlExprFn> values, SqlPredicateFn? and = null ) => Merges.NotMatchedInsert(ref __w, columns, values, and);
}



/// <summary> After a <c>WHEN</c> clause: more <c>WHEN</c> clauses, SQL Server's <c>OUTPUT</c>, terminate (SQL Server's MERGE always gets its <c>;</c>). </summary>
public ref struct MergeWhenStage
{
    private SqlWriter __w;

    internal MergeWhenStage( SqlWriter writer ) => __w = writer;

    public MergeWhenStage WhenMatchedThenUpdate( SqlSetFn set, SqlPredicateFn? and = null ) => Merges.MatchedUpdate(ref __w, set, and);
    public MergeWhenStage WhenMatchedThenDelete( SqlPredicateFn? and = null ) => Merges.MatchedDelete(ref __w, and);
    public MergeWhenStage WhenNotMatchedThenInsert( scoped ReadOnlySpan<string> columns, scoped ReadOnlySpan<SqlExprFn> values, SqlPredicateFn? and = null ) => Merges.NotMatchedInsert(ref __w, columns, values, and);

    /// <summary> SQL Server's <c>WHEN NOT MATCHED BY SOURCE [AND condition] THEN DELETE</c>. </summary>
    public MergeWhenStage WhenNotMatchedBySourceThenDelete( SqlPredicateFn? and = null )
    {
        __w.Require(__w.Dialect is SqlDialectKind.SqlServer, "WHEN NOT MATCHED BY SOURCE");
        __w.Word("WHEN NOT MATCHED BY SOURCE");
        Merges.And(ref __w, and);
        __w.Word("THEN DELETE");
        return new MergeWhenStage(__w);
    }

    /// <summary> SQL Server's <c>OUTPUT INSERTED.c, ...</c>. </summary>
    public CompletedStage Output( params ReadOnlySpan<string> columns ) => Mutations.Output(ref __w, columns, "INSERTED");

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
}



internal static class Merges
{
    public static void And( scoped ref SqlWriter writer, SqlPredicateFn? and )
    {
        if ( and is null ) { return; }

        writer.Word("AND");
        writer.OpenParen();
        Predicates.Write(ref writer, and);
        writer.CloseParen();
    }

    private static void OracleWhere( scoped ref SqlWriter writer, SqlPredicateFn? and )
    {
        if ( and is null ) { return; }

        writer.Word("WHERE");
        Predicates.Write(ref writer, and);
    }

    public static MergeWhenStage MatchedUpdate( scoped ref SqlWriter writer, SqlSetFn set, SqlPredicateFn? and )
    {
        bool oracle = writer.Dialect is SqlDialectKind.Oracle;
        writer.Word("WHEN MATCHED");
        if ( !oracle ) { And(ref writer, and); }

        writer.Word("THEN UPDATE SET");
        SqlSetList.Write(ref writer, set);
        if ( oracle ) { OracleWhere(ref writer, and); }

        return new MergeWhenStage(writer);
    }

    public static MergeWhenStage MatchedDelete( scoped ref SqlWriter writer, SqlPredicateFn? and )
    {
        writer.Require(writer.Dialect is not SqlDialectKind.Oracle, "WHEN MATCHED THEN DELETE");
        writer.Word("WHEN MATCHED");
        And(ref writer, and);
        writer.Word("THEN DELETE");
        return new MergeWhenStage(writer);
    }

    public static MergeWhenStage NotMatchedInsert( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns, scoped ReadOnlySpan<SqlExprFn> values, SqlPredicateFn? and )
    {
        writer.RequireItems(values.Length, "INSERT VALUES");
        if ( columns.Length > 0 && columns.Length != values.Length ) { throw new ArgumentException($"The insert names {columns.Length} column(s) but has {values.Length} value(s).", nameof(values)); }

        bool oracle = writer.Dialect is SqlDialectKind.Oracle;
        writer.Word("WHEN NOT MATCHED");
        if ( !oracle ) { And(ref writer, and); }

        writer.Word("THEN INSERT");
        if ( columns.Length > 0 ) { Clauses.WriteIdentifiers(ref writer, columns); }

        writer.Word("VALUES");
        writer.OpenParen();
        SqlExpr.WriteList(ref writer, values, "VALUES");
        writer.CloseParen();
        if ( oracle ) { OracleWhere(ref writer, and); }

        return new MergeWhenStage(writer);
    }
}
