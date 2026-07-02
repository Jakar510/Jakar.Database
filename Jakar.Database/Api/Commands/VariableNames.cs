// Jakar.Database :: Jakar.Database
// 03/06/2026  22:33


namespace Jakar.Database;


public readonly struct VariableNames
{
    internal readonly CommandParameters          Parameters;
    internal readonly StringBuilder              Value;
    public            ReadOnlySpan<SqlParameter> Values => Parameters.Values;
    public VariableNames( CommandParameters parameters, int indentLevel )
    {
        Parameters = parameters;
        Value      = new StringBuilder(parameters.VariableNameLength);

        if ( !parameters.IsGrouped )
        {
            AppendValues(Value, parameters.Values, indentLevel);
            return;
        }

        // Multi-row insert: emit one parenthesized tuple per row, separated by commas. Parameter names were suffixed per row in CommandParameters.AddGroup so they stay unique.
        bool first = true;

        if ( parameters.Count > 0 )
        {
            AppendTuple(Value, parameters.Values, indentLevel);
            first = false;
        }

        foreach ( ref readonly ImmutableArray<SqlParameter> array in parameters.Groups )
        {
            if ( !first ) { Value.Append(",\n"); }

            AppendTuple(Value, array.AsSpan(), indentLevel);
            first = false;
        }
    }


    /// <summary> Appends a comma-and-newline separated list of values, one per line, with no wrapping parentheses and no trailing comma. </summary>
    private static void AppendValues( StringBuilder builder, ReadOnlySpan<SqlParameter> values, int indentLevel )
    {
        for ( int i = 0; i < values.Length; i++ )
        {
            builder.Append(' ', indentLevel * 4);
            values[i].AppendValue(builder);
            if ( i < values.Length - 1 ) { builder.Append(','); }
            builder.Append('\n');
        }

        if ( values.Length > 0 ) { builder.Length--; } // drop the trailing newline
    }


    /// <summary> Appends a single parenthesized VALUES tuple for one row. </summary>
    private static void AppendTuple( StringBuilder builder, ReadOnlySpan<SqlParameter> values, int indentLevel )
    {
        builder.Append(' ', indentLevel * 4).Append("(\n");
        AppendValues(builder, values, indentLevel + 1);
        builder.Append('\n').Append(' ', indentLevel * 4).Append(')');
    }


    public override string ToString() => Value.ToString();
}
