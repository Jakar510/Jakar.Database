// Jakar.Database :: Jakar.Database
// 03/06/2026  22:33


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



public readonly struct KeyValuePairs
{
    internal readonly CommandParameters          Parameters;
    internal readonly StringBuilder              Value;
    public            ReadOnlySpan<SqlParameter> Values => Parameters.Values;
    public KeyValuePairs( CommandParameters parameters, int indentLevel, params ReadOnlySpan<char> separator )
    {
        Parameters = parameters;
        Value      = new StringBuilder(parameters.KeyValuePairLength(indentLevel));
        int                        index  = 0;
        int                        count  = parameters.Count;
        ReadOnlySpan<SqlParameter> buffer = parameters.Values;

        for ( int i = 0; i < buffer.Length; i++ )
        {
            ref readonly SqlParameter parameter = ref buffer[i];
            if ( i > 0 && !separator.IsEmpty ) { indentLevel++; }

            Value.Append(' ', indentLevel * 4).Append(parameter.Column.ColumnName).Append(" = @").Append(parameter.ParameterName);

            if ( index++ >= count - 1 ) { continue; }

            if ( i > 0 && !separator.IsEmpty )
            {
                indentLevel--;
                Value.Append('\n').Append(' ', indentLevel * 4).Append(separator);
            }

            Value.Append(",\n");
        }
    }
    public override string ToString() => Value.ToString();
}



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
            foreach ( ref readonly SqlParameter parameter in parameters.Values ) { Value.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append(",\n"); }
        }
        else
        {
            if ( parameters.Count > 0 )
            {
                Value.Append(' ', indentLevel * 4).Append('(');
                indentLevel++;
                foreach ( ref readonly SqlParameter parameter in parameters.Values ) { Value.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append(",\n"); }

                Value.Append("),\n");
                indentLevel--;
            }

            ReadOnlySpan<ImmutableArray<SqlParameter>> span = parameters.Groups;

            for ( int i = 0; i < span.Length; i++ )
            {
                ref readonly ImmutableArray<SqlParameter> array = ref span[i];
                Value.Append(' ', indentLevel * 4).Append("(\n");

                indentLevel++;

                foreach ( ref readonly SqlParameter parameter in array.AsSpan() )
                {
                    Value.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName);
                    if ( i < span.Length ) { Value.Append(",\n"); }
                }

                indentLevel--;
                Value.Append(' ', indentLevel * 4).Append("),\n");
            }
        }

        Value.Length -= 2;
    }
    public override string ToString() => Value.ToString();
}
