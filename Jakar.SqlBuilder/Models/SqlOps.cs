// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Shared predicate/operator emission, used by the comparison stages. </summary>
internal static class SqlOps
{
    public const string EQUAL         = "=";
    public const string NOT_EQUAL     = "<>";
    public const string GREATER       = ">";
    public const string GREATER_EQUAL = ">=";
    public const string LESS          = "<";
    public const string LESS_EQUAL    = "<=";

    /// <summary>
    ///     A typed column reference: qualified now when its table is already part of the current query, otherwise (a projection, written before <c>FROM</c>) qualified when the query
    ///     ends, with whatever alias the table was given.
    /// </summary>
    public static ISqlColumn TypedColumn<T>( scoped ref SqlWriter writer, string propertyName )
        where T : ISqlTable<T>
    {
        ISqlColumn column = TypedColumns.Resolve<T>(propertyName, in writer);
        string     name   = column.GetColumnName(writer.Dialect);

        if ( writer.IsRegistered(T.SqlTableName) ) { writer.QualifiedColumn(writer.QualifierFor(T.SqlTableName), name, true); }
        else { writer.DeferredColumn(T.SqlTableName, name); }

        return column;
    }

    public static void Op( scoped ref SqlWriter writer, string op, long value )
    {
        writer.Word(op);
        writer.InlineNumber(value);
    }
    public static void Op( scoped ref SqlWriter writer, string op, bool value )
    {
        writer.Word(op);
        writer.InlineBoolean(value);
    }
    public static void Op( scoped ref SqlWriter writer, string op, SqlValue value )
    {
        writer.Word(op);
        value.Write(ref writer);
    }
    public static void OpParam( scoped ref SqlWriter writer, string op, object? value )
    {
        writer.Word(op);
        writer.Parameter(value);
    }
    public static void OpColumn( scoped ref SqlWriter writer, string op, string rawColumn )
    {
        writer.Word(op);
        writer.ColumnRef(rawColumn);
    }
    public static void OpTypedColumn<T>( scoped ref SqlWriter writer, string op, string propertyName )
        where T : ISqlTable<T>
    {
        writer.Word(op);
        TypedColumn<T>(ref writer, propertyName);
    }

    public static void Null( scoped ref SqlWriter writer, bool negated ) => writer.Word(negated ? "IS NOT NULL" : "IS NULL");

    /// <exception cref="SqlBuildException"> No values: <c>IN ()</c> is invalid in every dialect (<see cref="SqlBuildError.EmptyPredicate"/>). </exception>
    private static void RequireValues( scoped in SqlWriter writer, int count )
    {
        if ( count <= 0 ) { throw writer.Error(SqlBuildError.EmptyPredicate, "IN needs at least one value."); }
    }

    public static void In( scoped ref SqlWriter writer, scoped ReadOnlySpan<long> values )
    {
        RequireValues(in writer, values.Length);
        writer.Word("IN ");
        writer.OpenCall();
        bool has = false;
        foreach ( long value in values )
        {
            if ( has ) { writer.Comma(); }

            writer.InlineNumber(value);
            has = true;
        }

        writer.CloseParen();
    }
    public static void InParams( scoped ref SqlWriter writer, scoped ReadOnlySpan<object?> values )
    {
        RequireValues(in writer, values.Length);
        writer.Word("IN ");
        writer.OpenCall();
        bool has = false;
        foreach ( object? value in values )
        {
            if ( has ) { writer.Comma(); }

            writer.Parameter(value);
            has = true;
        }

        writer.CloseParen();
    }
    public static void InValues( scoped ref SqlWriter writer, string keyword, scoped ReadOnlySpan<SqlValue> values )
    {
        RequireValues(in writer, values.Length);
        writer.Word(keyword);
        writer.OpenParen();
        for ( int i = 0; i < values.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            values[i].Write(ref writer);
        }

        writer.CloseParen();
    }

    public static void Between( scoped ref SqlWriter writer, long low, long high )
    {
        writer.Word("BETWEEN");
        writer.InlineNumber(low);
        writer.Word("AND");
        writer.InlineNumber(high);
    }
    public static void Between( scoped ref SqlWriter writer, string keyword, SqlValue low, SqlValue high )
    {
        writer.Word(keyword);
        low.Write(ref writer);
        writer.Word("AND");
        high.Write(ref writer);
    }
}
