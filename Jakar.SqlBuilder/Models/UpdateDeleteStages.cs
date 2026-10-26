// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> After <c>UPDATE table</c>: the first <c>SET</c> assignment. </summary>
public ref struct UpdateStage
{
    private          SqlWriter __w;
    private readonly string?   __source;

    internal UpdateStage( SqlWriter writer, string? source )
    {
        __w      = writer;
        __source = source;
    }

    public UpdateSetStage Set( string column, SqlValue  value )      => Next(column, value);
    public UpdateSetStage Set( string column, long      value )      => Next(column, SqlValue.Of(value));
    public UpdateSetStage Set( string column, SqlExprFn expression )
    {
        Begin();
        Assignments.Write(ref __w, column, expression);
        return new UpdateSetStage(__w, __source);
    }
    public UpdateSetStage SetParam( string column, object? value ) => Next(column, SqlValue.Param(value));

    public UpdateSetStage Set<T>( string propertyName, SqlValue value )
        where T : ISqlTable<T>
    {
        Begin();
        Assignments.WriteTyped<T>(ref __w, propertyName, value);
        return new UpdateSetStage(__w, __source);
    }
    public UpdateSetStage SetParam<T>( string propertyName, object? value )
        where T : ISqlTable<T> => Set<T>(propertyName, SqlValue.Param(value));

    /// <summary> <c>SET (a, b) = (x, y)</c> (PostgreSQL, SQLite); other dialects get <c>SET a = x, b = y</c>. </summary>
    public UpdateSetStage SetRow( string[] columns, params ReadOnlySpan<SqlValue> values )
    {
        Begin();
        Assignments.Row(ref __w, columns, values);
        return new UpdateSetStage(__w, __source);
    }

    /// <summary> <c>SET (a, b) = (SELECT x, y FROM ...)</c> (PostgreSQL, SQLite). </summary>
    public UpdateSetStage SetRow<TEnd>( string[] columns, SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        Begin();
        Assignments.Row(ref __w, columns, query);
        return new UpdateSetStage(__w, __source);
    }

    private UpdateSetStage Next( string column, SqlValue value )
    {
        Begin();
        Assignments.Write(ref __w, column, value);
        return new UpdateSetStage(__w, __source);
    }

    private void Begin()
    {
        __w.Word("SET");
        __w.MarkClause(ClauseFlags.Set);
    }
}



/// <summary> After the first assignment: more assignments, <c>FROM</c>, <c>WHERE</c>, <c>RETURNING</c>/<c>OUTPUT</c>, terminate. </summary>
public ref struct UpdateSetStage
{
    private          SqlWriter __w;
    private readonly string?   __source;

    internal UpdateSetStage( SqlWriter writer, string? source )
    {
        __w      = writer;
        __source = source;
    }

    private UpdateSetStage Self() => new(__w, __source);

    public UpdateSetStage Set( string column, SqlValue value )
    {
        __w.Comma();
        Assignments.Write(ref __w, column, value);
        return Self();
    }
    public UpdateSetStage Set( string column, long value ) => Set(column, SqlValue.Of(value));
    public UpdateSetStage Set( string column, SqlExprFn expression )
    {
        __w.Comma();
        Assignments.Write(ref __w, column, expression);
        return Self();
    }
    public UpdateSetStage SetParam( string column, object? value ) => Set(column, SqlValue.Param(value));
    public UpdateSetStage Set<T>( string propertyName, SqlValue value )
        where T : ISqlTable<T>
    {
        __w.Comma();
        Assignments.WriteTyped<T>(ref __w, propertyName, value);
        return Self();
    }
    public UpdateSetStage SetParam<T>( string propertyName, object? value )
        where T : ISqlTable<T> => Set<T>(propertyName, SqlValue.Param(value));
    public UpdateSetStage SetRow( string[] columns, params ReadOnlySpan<SqlValue> values )
    {
        __w.Comma();
        Assignments.Row(ref __w, columns, values);
        return Self();
    }

    /// <summary>
    ///     Update from other tables: <c>UPDATE t SET ... FROM other o</c> (PostgreSQL, SQLite, SQL Server) or MySQL's <c>UPDATE t, other o SET ...</c>. Relate them in <c>WHERE</c>.
    /// </summary>
    public MutationFromStage From( string table, string? alias = null )
    {
        __w.Require(__w.Dialect.SupportsUpdateFrom(), "UPDATE ... FROM");
        Mutations.EndTarget(ref __w, __source);
        Mutations.AddSource(ref __w, table, alias, __source is null ? "FROM" : ",");
        return new MutationFromStage(__w);
    }
    public MutationFromStage From<T>( string? alias = null )
        where T : ISqlTable<T> => From(T.SqlTableName, alias);

    public SqlComparison<MutationWhereStage> Where( string column )
    {
        Mutations.EndTarget(ref __w, __source);
        return Mutations.Where(ref __w, column);
    }
    public SqlComparison<MutationWhereStage> Where<T>( string propertyName )
        where T : ISqlTable<T>
    {
        Mutations.EndTarget(ref __w, __source);
        return Mutations.Where<T>(ref __w, propertyName);
    }
    public MutationWhereStage Where( SqlPredicateFn predicate )
    {
        Mutations.EndTarget(ref __w, __source);
        return Mutations.Where(ref __w, predicate);
    }

    public CompletedStage Returning( params ReadOnlySpan<string> columns )
    {
        Mutations.EndTarget(ref __w, __source);
        return Mutations.Returning(ref __w, columns);
    }
    public CompletedStage Output( params ReadOnlySpan<string> columns )
    {
        Mutations.EndTarget(ref __w, __source);
        return Mutations.Output(ref __w, columns, "INSERTED");
    }

    public SqlResult Build()
    {
        Mutations.EndTarget(ref __w, __source);
        return __w.Build();
    }
    public string BuildSql()
    {
        Mutations.EndTarget(ref __w, __source);
        return __w.BuildSql();
    }
    public SqlParameterSet WriteTo( IBufferWriter<char> destination )
    {
        Mutations.EndTarget(ref __w, __source);
        return __w.WriteTo(destination);
    }
}



/// <summary> After <c>DELETE FROM table</c>: <c>USING</c>, filter, return rows, or terminate. </summary>
public ref struct DeleteStage
{
    private          SqlWriter __w;
    private readonly bool      __aliased;

    internal DeleteStage( SqlWriter writer, bool aliased )
    {
        __w       = writer;
        __aliased = aliased;
    }

    /// <summary>
    ///     Delete using other tables: <c>DELETE FROM t USING other o</c> (PostgreSQL), <c>DELETE FROM t FROM other o</c> (SQL Server), <c>DELETE t FROM t, other o</c> (MySQL).
    ///     Relate them in <c>WHERE</c>.
    /// </summary>
    public MutationFromStage Using( string table, string? alias = null )
    {
        __w.Require(__w.Dialect.SupportsDeleteUsing(), "DELETE ... USING");
        switch ( __w.Dialect )
        {
            case SqlDialectKind.PostgreSql:
                Mutations.AddSource(ref __w, table, alias, "USING");
                break;

            case SqlDialectKind.SqlServer:
                Mutations.AddSource(ref __w, table, alias, __aliased ? "," : "FROM");
                break;

            default: // MySQL: name the target once, then list every table.
                if ( !__aliased ) { Mutations.NameDeleteTarget(ref __w); }

                Mutations.AddSource(ref __w, table, alias, ",");
                break;
        }

        return new MutationFromStage(__w);
    }
    public MutationFromStage Using<T>( string? alias = null )
        where T : ISqlTable<T> => Using(T.SqlTableName, alias);

    public SqlComparison<MutationWhereStage> Where( string column ) => Mutations.Where(ref __w, column);
    public SqlComparison<MutationWhereStage> Where<T>( string propertyName )
        where T : ISqlTable<T> => Mutations.Where<T>(ref __w, propertyName);
    public MutationWhereStage Where( SqlPredicateFn predicate ) => Mutations.Where(ref __w, predicate);

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);
    public CompletedStage Output( params ReadOnlySpan<string>    columns ) => Mutations.Output(ref __w, columns, "DELETED");

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
}



/// <summary> After <c>UPDATE ... FROM</c> / <c>DELETE ... USING</c>: more sources, then <c>WHERE</c>, return rows, or terminate. </summary>
public ref struct MutationFromStage
{
    private SqlWriter __w;

    internal MutationFromStage( SqlWriter writer ) => __w = writer;

    public MutationFromStage And( string table, string? alias = null )
    {
        Mutations.AddSource(ref __w, table, alias, ",");
        return new MutationFromStage(__w);
    }
    public MutationFromStage And<T>( string? alias = null )
        where T : ISqlTable<T> => And(T.SqlTableName, alias);

    public SqlComparison<MutationWhereStage> Where( string column ) => Mutations.Where(ref __w, column);
    public SqlComparison<MutationWhereStage> Where<T>( string propertyName )
        where T : ISqlTable<T> => Mutations.Where<T>(ref __w, propertyName);
    public MutationWhereStage Where( SqlPredicateFn predicate ) => Mutations.Where(ref __w, predicate);

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);
    public CompletedStage Output( params ReadOnlySpan<string>    columns ) => Mutations.Output(ref __w, columns, Mutations.Inflection(in __w));

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
}



/// <summary> After an <c>UPDATE</c>/<c>DELETE</c> predicate: chain, return rows, or terminate. </summary>
public ref struct MutationWhereStage : ISqlContinuation<MutationWhereStage>
{
    private SqlWriter __w;

    internal MutationWhereStage( SqlWriter writer ) => __w = writer;

    static MutationWhereStage ISqlContinuation<MutationWhereStage>.Continue( SqlWriter writer ) => new(writer);

    public SqlComparison<MutationWhereStage> And( string column ) => Predicates.Column<MutationWhereStage>(ref __w, "AND", column);
    public SqlComparison<MutationWhereStage> Or( string  column ) => Predicates.Column<MutationWhereStage>(ref __w, "OR",  column);
    public SqlComparison<MutationWhereStage> And<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<MutationWhereStage, T>(ref __w, "AND", propertyName);
    public SqlComparison<MutationWhereStage> Or<T>( string propertyName )
        where T : ISqlTable<T> => Predicates.Typed<MutationWhereStage, T>(ref __w, "OR", propertyName);
    public SqlComparison<MutationWhereStage> AndExpr( SqlExprFn expression ) => Predicates.Expr<MutationWhereStage>(ref __w, "AND", expression);
    public SqlComparison<MutationWhereStage> OrExpr( SqlExprFn  expression ) => Predicates.Expr<MutationWhereStage>(ref __w, "OR",  expression);
    public MutationWhereStage And( SqlPredicateFn predicate ) => Predicates.Group<MutationWhereStage>(ref __w, "AND", predicate, false);
    public MutationWhereStage Or( SqlPredicateFn  predicate ) => Predicates.Group<MutationWhereStage>(ref __w, "OR",  predicate, false);
    public MutationWhereStage AndExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<MutationWhereStage, TEnd>(ref __w, "AND", query, false);
    public MutationWhereStage AndNotExists<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct => Predicates.Exists<MutationWhereStage, TEnd>(ref __w, "AND", query, true);

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);
    public CompletedStage Output( params ReadOnlySpan<string>    columns ) => Mutations.Output(ref __w, columns, Mutations.Inflection(in __w));

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
}



/// <summary> Writes the assignments of a <c>SET</c> list (MERGE's <c>UPDATE SET</c>): <c>s =&gt; s.Set("name", e =&gt; e.Column("src.name")).Set("n", SqlValue.Of(1))</c>. </summary>
public delegate SqlSetList SqlSetFn( SqlSetList set );



/// <summary> A comma-separated list of <c>column = value</c> assignments. </summary>
public ref struct SqlSetList
{
    private SqlWriter __w;
    private bool      __has;

    internal SqlSetList( SqlWriter writer, bool has )
    {
        __w   = writer;
        __has = has;
    }

    internal readonly SqlWriter Writer => __w;
    internal readonly bool      Any    => __has;

    private void Next()
    {
        if ( __has ) { __w.Comma(); }
    }

    public SqlSetList Set( string column, SqlValue value )
    {
        Next();
        Assignments.Write(ref __w, column, value);
        return new SqlSetList(__w, true);
    }
    public SqlSetList Set( string column, long value ) => Set(column, SqlValue.Of(value));
    public SqlSetList Set( string column, SqlExprFn expression )
    {
        Next();
        Assignments.Write(ref __w, column, expression);
        return new SqlSetList(__w, true);
    }
    public SqlSetList SetParam( string column, object? value ) => Set(column, SqlValue.Param(value));
    public SqlSetList Set<T>( string propertyName, SqlValue value )
        where T : ISqlTable<T>
    {
        Next();
        Assignments.WriteTyped<T>(ref __w, propertyName, value);
        return new SqlSetList(__w, true);
    }

    internal static void Write( scoped ref SqlWriter writer, SqlSetFn set )
    {
        ArgumentNullException.ThrowIfNull(set);
        SqlSetList result = set(new SqlSetList(writer, false));
        if ( !result.Any ) { throw new ArgumentException("SET needs at least one assignment.", nameof(set)); }

        writer = result.Writer;
    }
}



/// <summary> <c>column = value</c> assignments (UPDATE, ON CONFLICT, MERGE). The target column is never qualified. </summary>
internal static class Assignments
{
    public static void Write( scoped ref SqlWriter writer, string column, SqlValue value )
    {
        writer.Identifier(column);
        writer.Word("=");
        value.Write(ref writer);
    }

    public static void Write( scoped ref SqlWriter writer, string column, SqlExprFn expression )
    {
        writer.Identifier(column);
        writer.Word("=");
        SqlExpr.Write(ref writer, expression);
    }

    public static void WriteTyped<T>( scoped ref SqlWriter writer, string propertyName, SqlValue value )
        where T : ISqlTable<T>
    {
        ISqlColumn column = TypedColumns.Resolve<T>(propertyName, in writer);
        writer.PreparedIdentifier(writer.Dialect.Fold(column.GetColumnName(writer.Dialect)));
        writer.Word("=");
        writer.SetComparedColumn(column);
        writer.CheckCompared(in value);

        value.Write(ref writer);
    }

    /// <summary> <c>column = EXCLUDED.column</c> (MySQL: the <c>excluded</c> row alias). </summary>
    public static void Excluded( scoped ref SqlWriter writer, string column )
    {
        writer.Identifier(column);
        writer.Word("=");
        writer.Word(writer.Dialect is SqlDialectKind.MySql ? "excluded" : "EXCLUDED");
        writer.Dot();
        writer.PreparedIdentifier(writer.Dialect.Fold(SqlWriter.RequireName(column, nameof(column))));
    }

    public static void Row( scoped ref SqlWriter writer, string[] columns, scoped ReadOnlySpan<SqlValue> values )
    {
        ArgumentNullException.ThrowIfNull(columns);
        writer.RequireItems(columns.Length, "SET");
        if ( values.Length != columns.Length ) { throw new ArgumentException($"SET names {columns.Length} column(s) but has {values.Length} value(s).", nameof(values)); }

        if ( columns.Length > 1 && writer.Dialect.SupportsRowValueSet() )
        {
            Clauses.WriteIdentifiers(ref writer, columns);
            writer.Word("=");
            Inserts.WriteRow(ref writer, values);
            return;
        }

        for ( int i = 0; i < columns.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            Write(ref writer, columns[i], values[i]);
        }
    }

    public static void Row<TEnd>( scoped ref SqlWriter writer, string[] columns, SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        ArgumentNullException.ThrowIfNull(columns);
        writer.RequireItems(columns.Length, "SET");
        writer.Require(writer.Dialect.SupportsRowValueSet(), "SET (columns) = (SELECT ...)");
        Clauses.WriteIdentifiers(ref writer, columns);
        writer.Word("=");
        Subqueries.Write(ref writer, query, true);
    }
}



/// <summary> Shared UPDATE / DELETE emission: targets, extra sources, <c>WHERE</c>, <c>RETURNING</c> / <c>OUTPUT</c>. </summary>
internal static class Mutations
{
    /// <summary>
    ///     <c>UPDATE table [alias]</c>. SQL Server can't alias the target there, so it writes <c>UPDATE alias SET ...</c> and the <c>FROM table alias</c> once the
    ///     assignments end (<see cref="EndTarget"/>).
    /// </summary>
    public static UpdateStage StartUpdate( scoped ref SqlWriter writer, string table, string? alias )
    {
        writer.Word("UPDATE");
        writer.MarkClause(ClauseFlags.Update);
        writer.RegisterTable(table, alias);

        if ( alias is { Length: > 0 } && writer.Dialect is SqlDialectKind.SqlServer )
        {
            writer.Identifier(alias);
            return new UpdateStage(writer, table);
        }

        writer.Identifier(table);
        Alias(ref writer, alias);
        writer.MarkTarget();
        return new UpdateStage(writer, null);
    }

    /// <summary> Ends an UPDATE's assignments: marks where SQL Server's <c>OUTPUT</c> goes and writes its deferred <c>FROM table alias</c>. </summary>
    public static void EndTarget( scoped ref SqlWriter writer, string? source )
    {
        if ( writer.IsBuilt ) { return; } // a repeated terminal replays the built result

        writer.MarkOutputPosition();
        if ( source is null || writer.Has(ClauseFlags.From) ) { return; }

        writer.Word("FROM");
        writer.Identifier(source);
        writer.Identifier(writer.QualifierFor(source) ?? source);
        writer.MarkClause(ClauseFlags.From);
    }

    /// <summary>
    ///     <c>DELETE FROM table [alias]</c>. SQL Server writes an aliased target as <c>DELETE alias FROM table alias</c>; SQL Server's <c>OUTPUT</c> goes right after the target.
    /// </summary>
    public static DeleteStage StartDelete( scoped ref SqlWriter writer, string table, string? alias )
    {
        writer.Word("DELETE");
        writer.MarkClause(ClauseFlags.Delete);
        writer.MarkTarget();
        writer.RegisterTable(table, alias);
        bool aliased = alias is { Length: > 0 };

        if ( aliased && writer.Dialect is SqlDialectKind.SqlServer or SqlDialectKind.MySql )
        {
            writer.Identifier(alias!);
            writer.MarkOutputPosition();
            writer.Word("FROM");
            writer.Identifier(table);
            writer.Identifier(alias!);
            writer.MarkClause(ClauseFlags.From);
            return new DeleteStage(writer, true);
        }

        writer.Word("FROM");
        writer.Identifier(table);
        Alias(ref writer, alias);
        writer.MarkOutputPosition();
        writer.MarkClause(ClauseFlags.From);
        return new DeleteStage(writer, false);
    }

    /// <summary> An UPDATE / DELETE target alias; SQLite requires <c>AS</c> there. </summary>
    private static void Alias( scoped ref SqlWriter writer, string? alias )
    {
        if ( alias is not { Length: > 0 } ) { return; }

        if ( writer.Dialect is SqlDialectKind.Sqlite ) { writer.Word("AS"); }

        writer.Identifier(alias);
    }

    /// <summary> MySQL's multi-table delete names its target before <c>FROM</c>: <c>DELETE t FROM t, other</c>. </summary>
    public static void NameDeleteTarget( scoped ref SqlWriter writer )
    {
        // The only table registered so far is the target.
        string target = writer.FirstTable ?? throw writer.Error(SqlBuildError.MissingRequiredClause, "DELETE has no target.");
        writer.InsertAtTarget($" {writer.QuotedIdentifier(target)}");
    }

    /// <summary> Adds a source table: written in place, or (MySQL's <c>UPDATE t, other SET</c>) at the recorded target. </summary>
    public static void AddSource( scoped ref SqlWriter writer, string table, string? alias, string keyword )
    {
        writer.RegisterTable(table, alias);
        writer.MarkClause(ClauseFlags.Join);

        if ( writer.Dialect is SqlDialectKind.MySql && writer.Has(ClauseFlags.Update) )
        {
            string text = alias is { Length: > 0 }
                              ? $", {writer.QuotedIdentifier(table)} {writer.QuotedIdentifier(alias)}"
                              : $", {writer.QuotedIdentifier(table)}";

            // The target position moves past each insertion, so the sources stay in call order.
            writer.InsertAtTarget(text);
            return;
        }

        if ( keyword is "," ) { writer.Comma(); }
        else { writer.Word(keyword); }

        writer.Identifier(table);
        if ( alias is { Length: > 0 } ) { writer.Identifier(alias); }

        writer.MarkClause(ClauseFlags.From);
    }

    public static string Inflection( scoped in SqlWriter writer ) => writer.Has(ClauseFlags.Update)
                                                                         ? "INSERTED"
                                                                         : "DELETED";

    public static SqlComparison<MutationWhereStage> Where( scoped ref SqlWriter writer, string column )
    {
        writer.MarkOutputPosition();
        writer.MarkClause(ClauseFlags.Where);
        return Predicates.Column<MutationWhereStage>(ref writer, "WHERE", column);
    }
    public static SqlComparison<MutationWhereStage> Where<T>( scoped ref SqlWriter writer, string propertyName )
        where T : ISqlTable<T>
    {
        writer.MarkOutputPosition();
        writer.MarkClause(ClauseFlags.Where);
        return Predicates.Typed<MutationWhereStage, T>(ref writer, "WHERE", propertyName);
    }
    public static MutationWhereStage Where( scoped ref SqlWriter writer, SqlPredicateFn predicate )
    {
        writer.MarkOutputPosition();
        writer.MarkClause(ClauseFlags.Where);
        writer.Word("WHERE");
        Predicates.Write(ref writer, predicate);
        return new MutationWhereStage(writer);
    }

    public static CompletedStage Returning( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns )
    {
        if ( !writer.Dialect.SupportsReturning() )
        {
            throw writer.Error(SqlBuildError.UnsupportedFeature,
                               writer.Dialect is SqlDialectKind.SqlServer
                                   ? "RETURNING is not supported by SqlServer; use Output(...) instead."
                                   : $"RETURNING is not supported by {writer.Dialect}.");
        }

        writer.RequireItems(columns.Length, "RETURNING");
        writer.Word("RETURNING");
        if ( writer.Dialect is SqlDialectKind.Sqlite )
        {
            // SQLite's RETURNING names the target's columns, unqualified.
            for ( int i = 0; i < columns.Length; i++ )
            {
                if ( i > 0 ) { writer.Comma(); }

                string column = SqlWriter.RequireName(columns[i], nameof(columns));
                if ( column is "*" ) { writer.Star(); }
                else { writer.Identifier(column[( column.LastIndexOf('.') + 1 )..]); }
            }
        }
        else { Clauses.WriteColumns(ref writer, columns); }
        writer.MarkClause(ClauseFlags.Returning);
        return new CompletedStage(writer);
    }

    /// <summary> SQL Server's <c>OUTPUT INSERTED.c, ...</c> / <c>OUTPUT DELETED.c, ...</c>, moved to where SQL Server requires it (before <c>VALUES</c>, <c>FROM</c> or <c>WHERE</c>). </summary>
    public static CompletedStage Output( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns, string inflection )
    {
        if ( !writer.Dialect.SupportsOutputClause() ) { throw writer.Error(SqlBuildError.UnsupportedFeature, "OUTPUT is only supported by SQL Server; use Returning(...) instead."); }

        writer.RequireItems(columns.Length, "OUTPUT");
        int start = writer.Length;
        writer.Word("OUTPUT");
        for ( int i = 0; i < columns.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            writer.Word(inflection);
            writer.Dot();
            if ( columns[i] is "*" ) { writer.Append('*'); }
            else { writer.PreparedIdentifier(writer.Dialect.Fold(SqlWriter.RequireName(columns[i], nameof(columns)))); }
        }

        writer.MoveToOutputPosition(start);
        writer.MarkClause(ClauseFlags.Output);
        return new CompletedStage(writer);
    }
}
