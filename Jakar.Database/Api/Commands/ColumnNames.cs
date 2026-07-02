namespace Jakar.Database;


public readonly struct ColumnNames
{
    internal readonly StringBuilder Value;
    public ColumnNames( CommandParameters parameters, int indentLevel )
    {
        Value = new StringBuilder();
        parameters.Table.ColumnNames(Value, ref indentLevel);
    }
    public override string ToString() => Value.ToString();
}