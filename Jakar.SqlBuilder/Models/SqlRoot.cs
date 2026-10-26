// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Owns the <see cref="SqlWriter"/> and starts a statement. The dialect is fixed for the whole chain. </summary>
public ref struct SqlRoot
{
    private SqlWriter __w;

    internal SqlRoot( SqlDialectKind dialect, SqlBuilderOptions options ) => __w = new SqlWriter(dialect, options);

    private SqlRoot( SqlWriter writer ) => __w = writer;

    internal readonly SqlWriter Writer => __w;

    /// <summary> The next operand of a set operation (after <c>UNION</c> / <c>INTERSECT</c> / <c>EXCEPT</c>): its tables and joins are its own. </summary>
    internal static SqlRoot Continue( SqlWriter writer )
    {
        writer.ResetTables();
        return new SqlRoot(writer);
    }

    /// <summary> A nested query (subquery, derived table, CTE body) on a writer that already opened its scope. </summary>
    internal static SqlRoot Nested( SqlWriter writer ) => new(writer);


    // ---- SELECT ----

    public SelectStage Select( params ReadOnlySpan<string> columns )         => StartSelect(false, columns);
    public SelectStage SelectDistinct( params ReadOnlySpan<string> columns ) => StartSelect(true,  columns);

    /// <summary> PostgreSQL's <c>SELECT DISTINCT ON (keys) columns</c>. </summary>
    public SelectStage SelectDistinctOn( string[] on, params ReadOnlySpan<string> columns )
    {
        ArgumentNullException.ThrowIfNull(on);
        __w.Require(__w.Dialect.SupportsDistinctOn(), "DISTINCT ON");
        __w.RequireItems(on.Length, "DISTINCT ON");
        __w.Word("SELECT");
        __w.MarkClause(ClauseFlags.Select);
        __w.Word("DISTINCT ON");
        __w.OpenParen();
        Clauses.WriteColumns(ref __w, on);
        __w.CloseParen();
        int topAt = __w.Length;
        bool has = WriteProjection(columns); // before __w is copied into the stage
        return new SelectStage(__w, has, topAt);
    }

    private SelectStage StartSelect( bool distinct, scoped ReadOnlySpan<string> columns )
    {
        __w.Word("SELECT");
        __w.MarkClause(ClauseFlags.Select);
        if ( distinct ) { __w.Word("DISTINCT"); }

        int topAt = __w.Length;
        bool has = WriteProjection(columns); // before __w is copied into the stage
        return new SelectStage(__w, has, topAt);
    }

    private bool WriteProjection( scoped ReadOnlySpan<string> columns )
    {
        bool has = false;
        foreach ( string column in columns )
        {
            if ( has ) { __w.Comma(); }

            if ( column is "*" ) { __w.Star(); }
            else { __w.ColumnRef(column); }

            has = true;
        }

        return has;
    }

    public SelectStage Select<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T> => StartSelect<T>(false, propertyNames);
    public SelectStage SelectDistinct<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T> => StartSelect<T>(true, propertyNames);

    private SelectStage StartSelect<T>( bool distinct, scoped ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T>
    {
        __w.Word("SELECT");
        __w.MarkClause(ClauseFlags.Select);
        if ( distinct ) { __w.Word("DISTINCT"); }

        int  topAt = __w.Length;
        bool has   = false;
        foreach ( string propertyName in propertyNames )
        {
            if ( has ) { __w.Comma(); }

            SqlOps.TypedColumn<T>(ref __w, propertyName);
            has = true;
        }

        __w.ClearCompared();
        return new SelectStage(__w, has, topAt);
    }


    // ---- INSERT / UPDATE / DELETE / MERGE / TRUNCATE ----

    public InsertStage Insert()
    {
        RequireDmlWith(ClauseFlags.Insert, "INSERT");
        __w.Word("INSERT");
        __w.MarkClause(ClauseFlags.Insert);
        return new InsertStage(__w);
    }

    /// <summary> <c>INSERT INTO table (mapped, non-identity columns)</c>, ready for one value per column. </summary>
    public InsertColumnsStage Insert<T>()
        where T : ISqlTable<T> => Insert().Into<T>();

    public UpdateStage Update( string table, string? alias = null )
    {
        RequireDmlWith(ClauseFlags.Update, "UPDATE");
        return Mutations.StartUpdate(ref __w, table, alias);
    }
    public UpdateStage Update<T>( string? alias = null )
        where T : ISqlTable<T> => Update(T.SqlTableName, alias);

    public DeleteStage DeleteFrom( string table, string? alias = null )
    {
        RequireDmlWith(ClauseFlags.Delete, "DELETE");
        return Mutations.StartDelete(ref __w, table, alias);
    }
    public DeleteStage Delete<T>( string? alias = null )
        where T : ISqlTable<T> => DeleteFrom(T.SqlTableName, alias);

    /// <summary> <c>MERGE INTO target alias USING ... ON (...) WHEN ...</c> (PostgreSQL 15+, SQL Server, Oracle, Firebird). </summary>
    public MergeStage MergeInto( string table, string alias )
    {
        __w.Require(__w.Dialect.SupportsMerge(), "MERGE");
        if ( __w.Has(ClauseFlags.With) ) { __w.Require(__w.Dialect is SqlDialectKind.PostgreSql or SqlDialectKind.SqlServer, "WITH ... MERGE"); }

        Clauses.Source(ref __w, "MERGE INTO", table, alias);
        __w.MarkClause(ClauseFlags.Merge);
        return new MergeStage(__w);
    }
    public MergeStage MergeInto<T>( string alias )
        where T : ISqlTable<T> => MergeInto(T.SqlTableName, alias);

    /// <summary> <c>TRUNCATE TABLE table</c>; SQLite and Firebird (which have none) get <c>DELETE FROM table</c>. </summary>
    public CompletedStage Truncate( string table )
    {
        if ( __w.Has(ClauseFlags.With) ) { throw __w.Error(SqlBuildError.UnsupportedFeature, "TRUNCATE can't follow WITH."); }

        __w.Word(__w.Dialect.SupportsTruncate() ? "TRUNCATE TABLE" : "DELETE FROM");
        __w.Identifier(table);
        return new CompletedStage(__w);
    }
    public CompletedStage Truncate<T>()
        where T : ISqlTable<T> => Truncate(T.SqlTableName);

    private readonly void RequireDmlWith( ClauseFlags statement, string verb )
    {
        if ( __w.Has(ClauseFlags.With) ) { __w.Require(__w.Dialect.SupportsDmlWith(statement), $"WITH ... {verb}"); }
    }


    // ---- WITH ----

    /// <summary> <c>WITH name [(columns)] AS (...)</c>. </summary>
    public CteNameStage With( string name, params ReadOnlySpan<string> columns ) => Ctes.Start(ref __w, name, columns, false);

    /// <summary> <c>WITH RECURSIVE name [(columns)] AS (...)</c>; SQL Server and Oracle write a plain <c>WITH</c>. </summary>
    public CteNameStage WithRecursive( string name, params ReadOnlySpan<string> columns ) => Ctes.Start(ref __w, name, columns, true);


    // ---- DDL ----

    /// <summary> <c>CREATE TABLE [IF NOT EXISTS] name (...)</c>; SQL Server's <c>IF NOT EXISTS</c> is an <c>IF OBJECT_ID(...) IS NULL</c> guard. </summary>
    public CreateTableStage CreateTable( string name, bool ifNotExists = false ) => Ddl.CreateTable(ref __w, name, ifNotExists);

    /// <summary> <c>CREATE TABLE</c> from the record's metadata: every mapped column with its dialect type, <c>NOT NULL</c>, identity, keys, defaults, checks and foreign keys. </summary>
    public CreateTableStage CreateTable<T>( bool ifNotExists = false )
        where T : ISqlTable<T>
    {
        CreateTableStage stage = Ddl.CreateTable(ref __w, T.SqlTableName, ifNotExists);
        return stage.Columns<T>();
    }

    public AlterTableStage AlterTable( string name ) => new(Ddl.Start(__w), SqlWriter.RequireName(name, nameof(name)));
    public AlterTableStage AlterTable<T>()
        where T : ISqlTable<T> => AlterTable(T.SqlTableName);

    public DdlStage DropTable( string name, bool ifExists = false, bool cascade = false ) => Ddl.Drop(ref __w, "TABLE", name, ifExists, cascade);
    public DdlStage DropTable<T>( bool ifExists = false, bool cascade = false )
        where T : ISqlTable<T> => DropTable(T.SqlTableName, ifExists, cascade);
    public DdlStage DropView( string name, bool ifExists = false ) => Ddl.Drop(ref __w, "VIEW", name, ifExists, false);

    /// <summary> <c>DROP INDEX [IF EXISTS] name</c>; SQL Server and MySQL need the <paramref name="table"/> (<c>... ON table</c>). </summary>
    public DdlStage DropIndex( string name, string? table = null, bool ifExists = false ) => Ddl.DropIndex(ref __w, name, table, ifExists);

    public CreateIndexStage CreateIndex( string name, string table, bool unique = false, bool ifNotExists = false ) => Ddl.CreateIndex(ref __w, name, table, unique, ifNotExists);
    public CreateIndexStage CreateIndex<T>( string name, bool unique = false, bool ifNotExists = false )
        where T : ISqlTable<T> => CreateIndex(name, T.SqlTableName, unique, ifNotExists);

    /// <summary> <c>CREATE VIEW name AS SELECT ...</c>. </summary>
    public DdlStage CreateView<TEnd>( string name, SqlSubquery<TEnd> query, params ReadOnlySpan<string> columns )
        where TEnd : ISqlQueryEnd, allows ref struct => Ddl.CreateView(ref __w, name, query, columns, false);

    /// <summary> <c>CREATE OR REPLACE VIEW</c> (SQL Server and Firebird: <c>CREATE OR ALTER VIEW</c>). </summary>
    public DdlStage CreateOrReplaceView<TEnd>( string name, SqlSubquery<TEnd> query, params ReadOnlySpan<string> columns )
        where TEnd : ISqlQueryEnd, allows ref struct => Ddl.CreateView(ref __w, name, query, columns, true);
}
