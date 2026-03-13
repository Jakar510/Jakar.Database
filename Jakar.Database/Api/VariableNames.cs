// Jakar.Database :: Jakar.Database
// 03/06/2026  22:33


namespace Jakar.Database;


public readonly struct ColumnNames
{
    internal readonly StringBuilder Value;
    public ColumnNames( CommandParameters parameters, int indentLevel )
    {
        StringBuilder sb = Value = new StringBuilder();
        parameters.Table.ColumnNames(sb, ref indentLevel);
    }
    public override string ToString() => Value.ToString();
}



public readonly struct KeyValuePairs
{
    internal readonly CommandParameters Parameters;
    internal readonly StringBuilder     Value;
    public KeyValuePairs( CommandParameters parameters, int indentLevel, params ReadOnlySpan<char> separator )
    {
        Parameters = parameters;
        StringBuilder sb = Value = new StringBuilder(parameters.KeyValuePairLength(indentLevel));

        int                             index  = 0;
        int                             count  = parameters.Count;
        using ArrayBuffer<SqlParameter> buffer = parameters.Parameters;

        foreach ( ref readonly SqlParameter parameter in buffer.Values )
        {
            if ( !separator.IsEmpty ) { indentLevel++; }

            sb.Append(' ', indentLevel * 4).Append(parameter.Column.ColumnName).Append(" = @").Append(parameter.ParameterName);

            if ( index++ >= count - 1 ) { continue; }

            if ( !separator.IsEmpty )
            {
                indentLevel--;
                sb.Append('\n').Append(' ', indentLevel * 4).Append(separator);
            }

            sb.Append(",\n");
        }
    }
    public override string ToString() => Value.ToString();
}



public readonly struct VariableNames
{
    internal readonly CommandParameters Parameters;
    internal readonly StringBuilder     Value;
    public VariableNames( CommandParameters parameters, int indentLevel )
    {
        Parameters = parameters;
        StringBuilder sb = Value = new StringBuilder(parameters.VariableNameLength);

        if ( !parameters.IsGrouped )
        {
            foreach ( ref readonly SqlParameter parameter in parameters.Values ) { sb.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append(",\n"); }
        }
        else
        {
            if ( parameters.Count > 0 )
            {
                sb.Append(' ', indentLevel * 4).Append('(');
                indentLevel++;
                foreach ( ref readonly SqlParameter parameter in parameters.Values ) { sb.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append(",\n"); }

                sb.Append("),\n");
                indentLevel--;
            }

            ReadOnlySpan<ImmutableArray<SqlParameter>> span = parameters.Extras;

            for ( int i = 0; i < span.Length; i++ )
            {
                ref readonly ImmutableArray<SqlParameter> array = ref span[i];
                sb.Append(' ', indentLevel * 4).Append("(\n");

                indentLevel++;

                foreach ( ref readonly SqlParameter parameter in array.AsSpan() )
                {
                    sb.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName);
                    if ( i < span.Length ) { sb.Append(",\n"); }
                }

                indentLevel--;
                sb.Append(' ', indentLevel * 4).Append("),\n");
            }
        }

        sb.Length -= 2;
    }
    public override string ToString() => Value.ToString();
}
