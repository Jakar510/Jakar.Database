// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Owns the <see cref="SqlWriter"/> and starts a statement. The dialect is fixed for the whole chain. </summary>
public ref struct SqlRoot
{
    private SqlWriter __w;

    internal SqlRoot( SqlDialectKind dialect, SqlBuilderOptions options ) => __w = new SqlWriter(dialect, options);

    /// <summary> Continuation constructor: start a new statement on an existing writer (e.g. after <c>UNION</c>). </summary>
    internal SqlRoot( SqlWriter writer ) => __w = writer;


    public SelectStage Select( params ReadOnlySpan<string> columns )         => StartSelect(false, columns);
    public SelectStage SelectDistinct( params ReadOnlySpan<string> columns ) => StartSelect(true,  columns);

    private SelectStage StartSelect( bool distinct, scoped ReadOnlySpan<string> columns )
    {
        __w.Word("SELECT");
        __w.MarkClause(ClauseFlags.Select);
        if ( distinct ) { __w.Word("DISTINCT"); }

        bool has = false;
        foreach ( string column in columns )
        {
            if ( has ) { __w.Comma(); }

            if ( column is "*" ) { __w.Star(); }
            else { __w.ColumnRef(column); }

            has = true;
        }

        return new SelectStage(__w, has);
    }

    public SelectStage Select<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T>
    {
        __w.Word("SELECT");
        __w.MarkClause(ClauseFlags.Select);
        bool has = false;
        foreach ( string propertyName in propertyNames )
        {
            ISqlColumn column = TypedColumns.Resolve<T>(propertyName, in __w);
            if ( has ) { __w.Comma(); }

            __w.QualifiedColumn(null, column.GetColumnName(__w.Dialect), true);
            has = true;
        }

        return new SelectStage(__w, has);
    }

    public InsertStage Insert()
    {
        __w.Word("INSERT");
        __w.MarkClause(ClauseFlags.Insert);
        return new InsertStage(__w);
    }

    public UpdateStage Update( string table )
    {
        __w.Word("UPDATE");
        __w.Identifier(table);
        __w.MarkClause(ClauseFlags.Update);
        return new UpdateStage(__w);
    }
    public UpdateStage Update<T>()
        where T : ISqlTable<T>
    {
        __w.Word("UPDATE");
        __w.Identifier(T.SqlTableName);
        __w.MarkClause(ClauseFlags.Update);
        return new UpdateStage(__w);
    }

    public DeleteStage DeleteFrom( string table )
    {
        __w.Word("DELETE");
        __w.Word("FROM");
        __w.Identifier(table);
        __w.MarkClause(ClauseFlags.Delete | ClauseFlags.From);
        return new DeleteStage(__w);
    }
    public DeleteStage Delete<T>()
        where T : ISqlTable<T>
    {
        __w.Word("DELETE");
        __w.Word("FROM");
        __w.Identifier(T.SqlTableName);
        __w.MarkClause(ClauseFlags.Delete | ClauseFlags.From);
        return new DeleteStage(__w);
    }
}
