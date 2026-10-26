namespace Jakar.SqlBuilder;


public readonly struct ColumnNames
{
    /// <summary> The rendered SQL fragment. </summary>
    public readonly string Value;
    public ColumnNames( CommandParameters parameters, int indentLevel ) : this(parameters.Table, indentLevel) { }

    /// <summary> <paramref name="table"/>'s column list, one per line, at <paramref name="indentLevel"/>. </summary>
    public ColumnNames( ISqlCommandTable table, int indentLevel )
    {
        ValueStringBuilder builder = new();
        table.ColumnNames(ref builder, ref indentLevel);
        Value = builder.ToString();
    }
    public override string ToString() => Value;
}