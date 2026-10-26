// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> A statement that is complete and can only be built. </summary>
public ref struct CompletedStage
{
    private SqlWriter __w;

    internal CompletedStage( SqlWriter writer ) => __w = writer;

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
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
        if ( columns.Length > 0 ) { Clauses.WriteIdentifiers(ref __w, columns); }

        __w.MarkClause(ClauseFlags.Into);
        return new InsertColumnsStage(__w, columns.Length);
    }

    /// <summary> <c>INTO table (columns)</c>; with no <paramref name="propertyNames"/>, every mapped column that isn't an identity. </summary>
    public InsertColumnsStage Into<T>( params ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T>
    {
        __w.Word("INTO");
        __w.Identifier(T.SqlTableName);
        __w.OpenParen();
        int count = 0;

        if ( propertyNames.Length > 0 )
        {
            foreach ( string propertyName in propertyNames ) { WriteColumn(TypedColumns.Resolve<T>(propertyName, in __w), ref count); }
        }
        else
        {
            foreach ( ISqlColumn column in T.SqlColumns )
            {
                if ( !column.IsIdentity ) { WriteColumn(column, ref count); }
            }

            if ( count is 0 ) { throw __w.Error(SqlBuildError.MissingRequiredClause, $"{T.SqlTableName} has no insertable (non-identity) columns."); }
        }

        __w.CloseParen();
        __w.MarkClause(ClauseFlags.Into);
        return new InsertColumnsStage(__w, count);
    }

    private void WriteColumn( ISqlColumn column, ref int count )
    {
        if ( count++ > 0 ) { __w.Comma(); }

        __w.PreparedIdentifier(__w.Dialect.Fold(column.GetColumnName(__w.Dialect)));
    }
}



/// <summary> After the insert target: <c>VALUES</c>, a <c>SELECT</c>, or <c>DEFAULT VALUES</c>. </summary>
public ref struct InsertColumnsStage
{
    private          SqlWriter __w;
    private readonly int       __columns;

    internal InsertColumnsStage( SqlWriter writer, int columns )
    {
        __w       = writer;
        __columns = columns;
    }

    /// <summary> PostgreSQL's <c>OVERRIDING SYSTEM VALUE</c>: insert explicit values into a <c>GENERATED ALWAYS</c> identity column. </summary>
    public InsertColumnsStage OverridingSystemValue()
    {
        __w.Require(__w.Dialect.SupportsOverridingSystemValue(), "OVERRIDING SYSTEM VALUE");
        __w.Word("OVERRIDING SYSTEM VALUE");
        return new InsertColumnsStage(__w, __columns);
    }

    public InsertValuesStage Values( params ReadOnlySpan<SqlValue> values )
    {
        Inserts.CheckCount(in __w, __columns, values.Length);
        __w.MarkOutputPosition();
        __w.Word("VALUES");
        Inserts.WriteRow(ref __w, values);
        __w.MarkClause(ClauseFlags.Values);
        return new InsertValuesStage(__w, __columns);
    }

    /// <summary> One row of bound parameters: <c>VALUES (@p0, @p1, ...)</c>. </summary>
    public InsertValuesStage ValuesParams( params object?[] values )
    {
        ArgumentNullException.ThrowIfNull(values);
        Inserts.CheckCount(in __w, __columns, values.Length);
        __w.MarkOutputPosition();
        __w.Word("VALUES");
        Inserts.WriteParams(ref __w, values);
        __w.MarkClause(ClauseFlags.Values);
        return new InsertValuesStage(__w, __columns);
    }

    /// <summary> <c>INSERT ... SELECT ...</c>. </summary>
    public InsertTailStage Select<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        __w.MarkOutputPosition();
        Subqueries.Write(ref __w, query, false);
        __w.MarkClause(ClauseFlags.Values);
        return new InsertTailStage(__w);
    }

    /// <summary> <c>DEFAULT VALUES</c> (MySQL: <c>() VALUES ()</c>). Needs an empty column list. </summary>
    public InsertTailStage DefaultValues()
    {
        if ( __columns > 0 ) { throw new ArgumentException("DEFAULT VALUES takes no column list.", nameof(DefaultValues)); }

        __w.Require(__w.Dialect.SupportsDefaultValues(), "DEFAULT VALUES");
        __w.MarkOutputPosition();
        __w.Word(__w.Dialect is SqlDialectKind.MySql ? "() VALUES ()" : "DEFAULT VALUES");
        __w.MarkClause(ClauseFlags.Values);
        return new InsertTailStage(__w);
    }
}



/// <summary> After <c>VALUES</c>: more rows, <c>ON CONFLICT</c>, <c>RETURNING</c>/<c>OUTPUT</c>, or terminate. </summary>
public ref struct InsertValuesStage
{
    private          SqlWriter __w;
    private readonly int       __columns;

    internal InsertValuesStage( SqlWriter writer, int columns )
    {
        __w       = writer;
        __columns = columns;
    }

    public InsertValuesStage Values( params ReadOnlySpan<SqlValue> values )
    {
        Inserts.CheckCount(in __w, __columns, values.Length);
        Inserts.RequireMultiRow(in __w);
        __w.Comma();
        Inserts.WriteRow(ref __w, values);
        return new InsertValuesStage(__w, __columns);
    }
    public InsertValuesStage ValuesParams( params object?[] values )
    {
        ArgumentNullException.ThrowIfNull(values);
        Inserts.CheckCount(in __w, __columns, values.Length);
        Inserts.RequireMultiRow(in __w);
        __w.Comma();
        Inserts.WriteParams(ref __w, values);
        return new InsertValuesStage(__w, __columns);
    }

    /// <summary>
    ///     <c>ON CONFLICT [(columns)]</c> (PostgreSQL, SQLite). MySQL has no conflict target (any unique key conflicts) and gets <c>AS excluded ON DUPLICATE KEY UPDATE</c>.
    /// </summary>
    public ConflictStage OnConflict( params ReadOnlySpan<string> columns )
    {
        __w.Require(__w.Dialect.SupportsOnConflict(), "ON CONFLICT");
        string? first = columns.Length > 0 ? SqlWriter.RequireName(columns[0], nameof(columns)) : null;

        // MySQL writes its clause once the action is known (ConflictStage).
        if ( __w.Dialect is SqlDialectKind.MySql ) { return new ConflictStage(__w, first); }

        __w.Word("ON CONFLICT");
        if ( columns.Length > 0 ) { Clauses.WriteIdentifiers(ref __w, columns); }

        return new ConflictStage(__w, first);
    }

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);

    /// <summary> SQL Server's <c>OUTPUT INSERTED.column, ...</c>, written before <c>VALUES</c> where SQL Server requires it. </summary>
    public CompletedStage Output( params ReadOnlySpan<string> columns ) => Mutations.Output(ref __w, columns, "INSERTED");

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
}



/// <summary> After <c>INSERT ... SELECT</c> or <c>DEFAULT VALUES</c>: <c>RETURNING</c>/<c>OUTPUT</c>, or terminate. </summary>
public ref struct InsertTailStage
{
    private SqlWriter __w;

    internal InsertTailStage( SqlWriter writer ) => __w = writer;

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);
    public CompletedStage Output( params ReadOnlySpan<string>    columns ) => Mutations.Output(ref __w, columns, "INSERTED");

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
}



/// <summary> After <c>ON CONFLICT (...)</c>: <c>DO NOTHING</c> or <c>DO UPDATE SET ...</c>. </summary>
public ref struct ConflictStage
{
    private          SqlWriter __w;
    private readonly string?   __first;

    internal ConflictStage( SqlWriter writer, string? first )
    {
        __w     = writer;
        __first = first;
    }

    /// <summary> <c>DO NOTHING</c>; MySQL gets the no-op <c>ON DUPLICATE KEY UPDATE first = first</c> (so it needs a conflict column). </summary>
    public InsertTailStage DoNothing()
    {
        if ( __w.Dialect is SqlDialectKind.MySql )
        {
            if ( __first is null ) { throw new ArgumentException("MySQL writes DO NOTHING as 'column = column'; name a conflict column.", nameof(DoNothing)); }

            __w.Word("ON DUPLICATE KEY UPDATE");
            __w.Identifier(__first);
            __w.Word("=");
            __w.Identifier(__first);
            return new InsertTailStage(__w);
        }

        __w.Word("DO NOTHING");
        return new InsertTailStage(__w);
    }

    /// <summary> <c>DO UPDATE SET column = value</c>. </summary>
    public ConflictUpdateStage DoUpdateSet( string column, SqlValue value )
    {
        Begin();
        Assignments.Write(ref __w, column, value);
        return new ConflictUpdateStage(__w);
    }
    public ConflictUpdateStage DoUpdateSet( string column, SqlExprFn expression )
    {
        Begin();
        Assignments.Write(ref __w, column, expression);
        return new ConflictUpdateStage(__w);
    }

    /// <summary> <c>DO UPDATE SET column = EXCLUDED.column</c>: take the value the insert proposed. </summary>
    public ConflictUpdateStage DoUpdateSet( string column, bool fromExcluded )
    {
        if ( !fromExcluded ) { throw new ArgumentException("Pass a value to set, or fromExcluded: true.", nameof(fromExcluded)); }

        Begin();
        Assignments.Excluded(ref __w, column);
        return new ConflictUpdateStage(__w);
    }

    private void Begin()
    {
        if ( __w.Dialect is SqlDialectKind.MySql )
        {
            __w.Word("AS excluded ON DUPLICATE KEY UPDATE");
            return;
        }

        if ( __first is null && __w.Dialect is SqlDialectKind.PostgreSql ) { throw new ArgumentException("ON CONFLICT DO UPDATE needs a conflict target (columns).", nameof(DoUpdateSet)); }

        __w.Word("DO UPDATE SET");
    }
}



/// <summary> After a <c>DO UPDATE SET</c> assignment: more assignments, <c>WHERE</c>, <c>RETURNING</c>, terminate. </summary>
public ref struct ConflictUpdateStage
{
    private SqlWriter __w;

    internal ConflictUpdateStage( SqlWriter writer ) => __w = writer;

    public ConflictUpdateStage Set( string column, SqlValue value )
    {
        __w.Comma();
        Assignments.Write(ref __w, column, value);
        return new ConflictUpdateStage(__w);
    }
    public ConflictUpdateStage Set( string column, SqlExprFn expression )
    {
        __w.Comma();
        Assignments.Write(ref __w, column, expression);
        return new ConflictUpdateStage(__w);
    }
    public ConflictUpdateStage SetExcluded( string column )
    {
        __w.Comma();
        Assignments.Excluded(ref __w, column);
        return new ConflictUpdateStage(__w);
    }

    /// <summary> <c>DO UPDATE SET ... WHERE predicate</c> (PostgreSQL, SQLite). </summary>
    public InsertTailStage Where( SqlPredicateFn predicate )
    {
        __w.Require(__w.Dialect is not SqlDialectKind.MySql, "ON DUPLICATE KEY UPDATE ... WHERE");
        __w.Word("WHERE");
        Predicates.Write(ref __w, predicate);
        return new InsertTailStage(__w);
    }

    public CompletedStage Returning( params ReadOnlySpan<string> columns ) => Mutations.Returning(ref __w, columns);

    public SqlResult       Build()                                  => __w.Build();
    public string          BuildSql()                               => __w.BuildSql();
    public SqlParameterSet WriteTo( IBufferWriter<char> destination ) => __w.WriteTo(destination);
}



internal static class Inserts
{
    public static void CheckCount( scoped in SqlWriter writer, int columns, int values )
    {
        writer.RequireItems(values, "VALUES");
        if ( columns > 0 && values != columns ) { throw new ArgumentException($"The insert names {columns} column(s) but this row has {values} value(s).", nameof(values)); }
    }

    public static void RequireMultiRow( scoped in SqlWriter writer )
    {
        if ( !writer.Dialect.SupportsMultiRowValues() ) { throw writer.Error(SqlBuildError.UnsupportedFeature, $"{writer.Dialect} has no multi-row VALUES; insert the rows one statement at a time."); }
    }

    public static void WriteRow( scoped ref SqlWriter writer, scoped ReadOnlySpan<SqlValue> values )
    {
        writer.OpenParen();
        for ( int i = 0; i < values.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            values[i].Write(ref writer);
        }

        writer.CloseParen();
    }

    public static void WriteParams( scoped ref SqlWriter writer, scoped ReadOnlySpan<object?> values )
    {
        writer.OpenParen();
        for ( int i = 0; i < values.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            writer.Parameter(values[i]);
        }

        writer.CloseParen();
    }
}
