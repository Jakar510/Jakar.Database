// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Shared predicate/operator emission, used by the WHERE, JOIN ON and HAVING comparison stages. </summary>
internal static class SqlOps
{
    public const string EQUAL         = "=";
    public const string NOT_EQUAL     = "<>";
    public const string GREATER       = ">";
    public const string GREATER_EQUAL = ">=";
    public const string LESS          = "<";
    public const string LESS_EQUAL    = "<=";

    public static void TypedColumn<T>( scoped ref SqlWriter writer, string propertyName )
        where T : ISqlTable<T>
    {
        ISqlColumn column = TypedColumns.Resolve<T>(propertyName, in writer);
        writer.QualifiedColumn(null, column.GetColumnName(writer.Dialect), true);
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

    public static void Like( scoped ref SqlWriter writer, SqlValue pattern )
    {
        writer.Word("LIKE");
        pattern.Write(ref writer);
    }

    public static void In( scoped ref SqlWriter writer, scoped ReadOnlySpan<long> values )
    {
        writer.Word("IN");
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
        writer.Word("IN");
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

    public static void Between( scoped ref SqlWriter writer, long low, long high )
    {
        writer.Word("BETWEEN");
        writer.InlineNumber(low);
        writer.Word("AND");
        writer.InlineNumber(high);
    }
}
