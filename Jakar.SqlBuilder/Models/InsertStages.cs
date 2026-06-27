// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> A statement that is complete and can only be built. </summary>
public ref struct CompletedStage
{
    private SqlWriter __w;

    internal CompletedStage( SqlWriter writer ) => __w = writer;

    public SqlResult Build() => __w.Build();
}



/// <summary> After <c>INSERT</c>: target table and column list. </summary>
public ref struct InsertStage
{
    private SqlWriter __w;

    internal InsertStage( SqlWriter writer ) => __w = writer;

    public InsertColumnsStage Into( string table, params ReadOnlySpan<string> columns )
    {
        __w.Word("INTO");
        __w.Identifier(table);
        WriteColumnList(columns);
        __w.MarkClause(ClauseFlags.Into);
        return new InsertColumnsStage(__w);
    }
    public InsertColumnsStage Into<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T>
    {
        __w.Word("INTO");
        __w.Identifier(T.SqlTableName);
        if ( propertyNames.Length > 0 )
        {
            __w.OpenParen();
            bool has = false;
            foreach ( string propertyName in propertyNames )
            {
                ISqlColumn column = TypedColumns.Resolve<T>(propertyName, in __w);
                if ( has ) { __w.Comma(); }

                __w.PreparedIdentifier(SqlDialects.Fold(__w.Dialect, column.GetColumnName(__w.Dialect)));
                has = true;
            }

            __w.CloseParen();
        }

        __w.MarkClause(ClauseFlags.Into);
        return new InsertColumnsStage(__w);
    }

    private void WriteColumnList( scoped ReadOnlySpan<string> columns )
    {
        if ( columns.Length is 0 ) { return; }

        __w.OpenParen();
        bool has = false;
        foreach ( string column in columns )
        {
            if ( has ) { __w.Comma(); }

            __w.Identifier(column);
            has = true;
        }

        __w.CloseParen();
    }
}



/// <summary> After the insert target: supply <c>VALUES</c>. </summary>
public ref struct InsertColumnsStage
{
    private SqlWriter __w;

    internal InsertColumnsStage( SqlWriter writer ) => __w = writer;

    public InsertValuesStage Values( params ReadOnlySpan<SqlValue> values )
    {
        __w.Word("VALUES");
        WriteRow(values);
        __w.MarkClause(ClauseFlags.Values);
        return new InsertValuesStage(__w);
    }
    public InsertValuesStage ValuesParams( params object?[] values )
    {
        __w.Word("VALUES");
        __w.OpenParen();
        bool has = false;
        foreach ( object? value in values )
        {
            if ( has ) { __w.Comma(); }

            __w.Parameter(value);
            has = true;
        }

        __w.CloseParen();
        __w.MarkClause(ClauseFlags.Values);
        return new InsertValuesStage(__w);
    }

    private void WriteRow( scoped ReadOnlySpan<SqlValue> values )
    {
        __w.OpenParen();
        bool has = false;
        foreach ( SqlValue value in values )
        {
            if ( has ) { __w.Comma(); }

            value.Write(ref __w);
            has = true;
        }

        __w.CloseParen();
    }
}



/// <summary> After <c>VALUES</c>: additional rows, <c>RETURNING</c>/<c>OUTPUT</c>, or terminate. </summary>
public ref struct InsertValuesStage
{
    private SqlWriter __w;

    internal InsertValuesStage( SqlWriter writer ) => __w = writer;

    public InsertValuesStage Values( params ReadOnlySpan<SqlValue> values )
    {
        __w.Comma();
        __w.OpenParen();
        bool has = false;
        foreach ( SqlValue value in values )
        {
            if ( has ) { __w.Comma(); }

            value.Write(ref __w);
            has = true;
        }

        __w.CloseParen();
        return new InsertValuesStage(__w);
    }

    public CompletedStage Returning( params ReadOnlySpan<string> columns )
    {
        if ( !SqlDialects.SupportsReturning(__w.Dialect) )
        {
            throw new SqlBuildException(SqlBuildError.UnsupportedFeature, $"RETURNING is not supported by {__w.Dialect}; use Output(...) on SQL Server.", __w.Snapshot(), __w.Dialect);
        }

        __w.Word("RETURNING");
        WriteList(columns);
        __w.MarkClause(ClauseFlags.Returning);
        return new CompletedStage(__w);
    }
    public CompletedStage Output( params ReadOnlySpan<string> columns )
    {
        if ( !SqlDialects.SupportsOutputClause(__w.Dialect) )
        {
            throw new SqlBuildException(SqlBuildError.UnsupportedFeature, "OUTPUT is only supported by SQL Server; use Returning(...) instead.", __w.Snapshot(), __w.Dialect);
        }

        __w.Word("OUTPUT");
        bool has = false;
        foreach ( string column in columns )
        {
            if ( has ) { __w.Comma(); }

            __w.Word("INSERTED");
            __w.Dot();
            __w.Identifier(column);
            has = true;
        }

        __w.MarkClause(ClauseFlags.Output);
        return new CompletedStage(__w);
    }

    private void WriteList( scoped ReadOnlySpan<string> columns )
    {
        bool has = false;
        foreach ( string column in columns )
        {
            if ( has ) { __w.Comma(); }

            __w.ColumnRef(column);
            has = true;
        }
    }

    public SqlResult Build() => __w.Build();
}
