// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> After <c>UPDATE table</c>: the first <c>SET</c> assignment. </summary>
public ref struct UpdateStage
{
    private SqlWriter __w;

    internal UpdateStage( SqlWriter writer ) => __w = writer;

    public UpdateSetStage Set( string column, SqlValue value ) => WriteSet(column, value);
    public UpdateSetStage Set( string column, long     value ) => WriteSet(column, SqlValue.Of(value));
    public UpdateSetStage SetParam( string column, object? value ) => WriteSet(column, SqlValue.Param(value));

    public UpdateSetStage Set<T>( string propertyName, SqlValue value ) where T : ISqlTable<T> => WriteSetTyped<T>(propertyName, value);
    public UpdateSetStage SetParam<T>( string propertyName, object? value ) where T : ISqlTable<T> => WriteSetTyped<T>(propertyName, SqlValue.Param(value));

    private UpdateSetStage WriteSet( string column, SqlValue value )
    {
        __w.Word("SET");
        __w.Identifier(column);
        __w.Word("=");
        value.Write(ref __w);
        __w.MarkClause(ClauseFlags.Set);
        return new UpdateSetStage(__w);
    }
    private UpdateSetStage WriteSetTyped<T>( string propertyName, SqlValue value )
        where T : ISqlTable<T>
    {
        ISqlColumn column = TypedColumns.Resolve<T>(propertyName, in __w);
        __w.Word("SET");
        __w.PreparedIdentifier(SqlDialects.Fold(__w.Dialect, column.GetColumnName(__w.Dialect)));
        __w.Word("=");
        value.Write(ref __w);
        __w.MarkClause(ClauseFlags.Set);
        return new UpdateSetStage(__w);
    }
}



/// <summary> After the first assignment: more assignments, <c>WHERE</c>, <c>RETURNING</c>/<c>OUTPUT</c>, terminate. </summary>
public ref struct UpdateSetStage
{
    private SqlWriter __w;

    internal UpdateSetStage( SqlWriter writer ) => __w = writer;

    public UpdateSetStage Set( string column, SqlValue value )    => More(column, value);
    public UpdateSetStage Set( string column, long     value )    => More(column, SqlValue.Of(value));
    public UpdateSetStage SetParam( string column, object? value ) => More(column, SqlValue.Param(value));

    private UpdateSetStage More( string column, SqlValue value )
    {
        __w.Comma();
        __w.Identifier(column);
        __w.Word("=");
        value.Write(ref __w);
        return new UpdateSetStage(__w);
    }

    public MutationWhereComparison Where( string column )
    {
        __w.Word("WHERE");
        __w.ColumnRef(column);
        __w.MarkClause(ClauseFlags.Where);
        return new MutationWhereComparison(__w);
    }
    public MutationWhereComparison Where<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word("WHERE");
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.MarkClause(ClauseFlags.Where);
        return new MutationWhereComparison(__w);
    }

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);
    public CompletedStage Output( params ReadOnlySpan<string>    columns ) => Mutations.Output(ref __w, columns, "INSERTED");

    public SqlResult Build() => __w.Build();
}



/// <summary> After <c>DELETE FROM table</c>: filter or terminate. </summary>
public ref struct DeleteStage
{
    private SqlWriter __w;

    internal DeleteStage( SqlWriter writer ) => __w = writer;

    public MutationWhereComparison Where( string column )
    {
        __w.Word("WHERE");
        __w.ColumnRef(column);
        __w.MarkClause(ClauseFlags.Where);
        return new MutationWhereComparison(__w);
    }
    public MutationWhereComparison Where<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word("WHERE");
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.MarkClause(ClauseFlags.Where);
        return new MutationWhereComparison(__w);
    }

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);
    public CompletedStage Output( params ReadOnlySpan<string>    columns ) => Mutations.Output(ref __w, columns, "DELETED");

    public SqlResult Build() => __w.Build();
}



/// <summary> Right-hand side of an <c>UPDATE</c>/<c>DELETE</c> <c>WHERE</c> predicate. </summary>
public ref struct MutationWhereComparison
{
    private SqlWriter __w;

    internal MutationWhereComparison( SqlWriter writer ) => __w = writer;

    private MutationWhereStage Done() => new(__w);

    public MutationWhereStage EqualTo( long     value ) { SqlOps.Op(ref __w, SqlOps.EQUAL, value); return Done(); }
    public MutationWhereStage EqualTo( bool     value ) { SqlOps.Op(ref __w, SqlOps.EQUAL, value); return Done(); }
    public MutationWhereStage EqualTo( SqlValue value ) { SqlOps.Op(ref __w, SqlOps.EQUAL, value); return Done(); }
    public MutationWhereStage EqualTo( object?  param ) { SqlOps.OpParam(ref __w, SqlOps.EQUAL, param); return Done(); }

    public MutationWhereStage NotEqualTo( long   value ) { SqlOps.Op(ref __w, SqlOps.NOT_EQUAL, value); return Done(); }
    public MutationWhereStage NotEqualTo( object? param ) { SqlOps.OpParam(ref __w, SqlOps.NOT_EQUAL, param); return Done(); }

    public MutationWhereStage GreaterThan( long value ) { SqlOps.Op(ref __w, SqlOps.GREATER, value); return Done(); }
    public MutationWhereStage LessThan( long    value ) { SqlOps.Op(ref __w, SqlOps.LESS, value); return Done(); }
    public MutationWhereStage GreaterThan( object? param ) { SqlOps.OpParam(ref __w, SqlOps.GREATER, param); return Done(); }
    public MutationWhereStage LessThan( object?    param ) { SqlOps.OpParam(ref __w, SqlOps.LESS, param); return Done(); }

    public MutationWhereStage EqualToColumn( string rawColumn ) { SqlOps.OpColumn(ref __w, SqlOps.EQUAL, rawColumn); return Done(); }
    public MutationWhereStage IsNull()    { SqlOps.Null(ref __w, false); return Done(); }
    public MutationWhereStage IsNotNull() { SqlOps.Null(ref __w, true);  return Done(); }
    public MutationWhereStage Like( string inlinePattern ) { SqlOps.Like(ref __w, SqlValue.Inline(inlinePattern)); return Done(); }
    public MutationWhereStage In( params ReadOnlySpan<long> values ) { SqlOps.In(ref __w, values); return Done(); }
    public MutationWhereStage Between( long low, long high ) { SqlOps.Between(ref __w, low, high); return Done(); }
}



/// <summary> After an <c>UPDATE</c>/<c>DELETE</c> predicate: chain, return rows, or terminate. </summary>
public ref struct MutationWhereStage
{
    private SqlWriter __w;

    internal MutationWhereStage( SqlWriter writer ) => __w = writer;

    public MutationWhereComparison And( string column )
    {
        __w.Word("AND");
        __w.ColumnRef(column);
        return new MutationWhereComparison(__w);
    }
    public MutationWhereComparison Or( string column )
    {
        __w.Word("OR");
        __w.ColumnRef(column);
        return new MutationWhereComparison(__w);
    }
    public MutationWhereComparison And<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word("AND");
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        return new MutationWhereComparison(__w);
    }
    public MutationWhereComparison Or<T>( string propertyName )
        where T : ISqlTable<T>
    {
        __w.Word("OR");
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        return new MutationWhereComparison(__w);
    }

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);
    public CompletedStage Output( params ReadOnlySpan<string>    columns ) => Mutations.Output(ref __w, columns, "DELETED");

    public SqlResult Build() => __w.Build();
}



/// <summary> Shared RETURNING / OUTPUT emission for UPDATE and DELETE. </summary>
internal static class Mutations
{
    public static CompletedStage Returning( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns )
    {
        if ( !SqlDialects.SupportsReturning(writer.Dialect) )
        {
            throw new SqlBuildException(SqlBuildError.UnsupportedFeature, $"RETURNING is not supported by {writer.Dialect}; use Output(...) on SQL Server.", writer.Snapshot(), writer.Dialect);
        }

        writer.Word("RETURNING");
        bool has = false;
        foreach ( string column in columns )
        {
            if ( has ) { writer.Comma(); }

            writer.ColumnRef(column);
            has = true;
        }

        writer.MarkClause(ClauseFlags.Returning);
        return new CompletedStage(writer);
    }
    public static CompletedStage Output( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns, string inflection )
    {
        if ( !SqlDialects.SupportsOutputClause(writer.Dialect) )
        {
            throw new SqlBuildException(SqlBuildError.UnsupportedFeature, "OUTPUT is only supported by SQL Server; use Returning(...) instead.", writer.Snapshot(), writer.Dialect);
        }

        writer.Word("OUTPUT");
        bool has = false;
        foreach ( string column in columns )
        {
            if ( has ) { writer.Comma(); }

            writer.Word(inflection);
            writer.Dot();
            writer.Identifier(column);
            has = true;
        }

        writer.MarkClause(ClauseFlags.Output);
        return new CompletedStage(writer);
    }
}
