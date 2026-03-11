// Jakar.Database :: Jakar.Database
// 03/06/2026  22:33

using ZLinq.Linq;



namespace Jakar.Database;


public readonly struct ColumnNames
{
    internal readonly PostgresParameters Parameters;
    internal readonly StringBuilder      Value;
    public ColumnNames( PostgresParameters parameters, int indentLevel )
    {
        Parameters = parameters;
        StringBuilder sb = Value = new StringBuilder();
        parameters.Table.ColumnNames(sb, ref indentLevel);
    }
    public override string ToString() => Value.ToString();
}



public readonly struct KeyValuePairs
{
    internal readonly PostgresParameters Parameters;
    internal readonly StringBuilder      Value;
    public KeyValuePairs( PostgresParameters parameters, int indentLevel, string separator )
    {
        Parameters = parameters;
        StringBuilder sb = Value = new StringBuilder(parameters.KeyValuePairLength(indentLevel));

        int                          index  = 0;
        int                          count  = parameters.Count;
        using ArrayBuffer<Parameter> buffer = parameters.Parameters;

        foreach ( ref readonly Parameter parameter in buffer.Values )
        {
            sb.Append(' ', indentLevel * 4).Append(parameter.SourceColumn).Append(" = @").Append(parameter.ParameterName);

            if ( index++ >= count - 1 ) { continue; }

            sb.Append(",\n");
            sb.Append(separator);
        }
    }
    public override string ToString() => Value.ToString();
}



public readonly struct VariableNames
{
    internal readonly PostgresParameters Parameters;
    internal readonly StringBuilder      Value;
    public VariableNames( PostgresParameters parameters, int indentLevel )
    {
        Parameters = parameters;
        StringBuilder sb = Value = new StringBuilder(parameters.VariableNameLength);

        if ( !parameters.IsGrouped )
        {
            foreach ( ref readonly Parameter parameter in parameters.Values ) { sb.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append(",\n"); }
        }
        else
        {
            if ( parameters.Count > 0 )
            {
                sb.Append(' ', indentLevel * 4).Append('(');
                indentLevel++;
                foreach ( ref readonly Parameter parameter in parameters.Values ) { sb.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append(",\n"); }

                sb.Append("),\n");
                indentLevel--;
            }

            ReadOnlySpan<ImmutableArray<Parameter>> span = parameters.Extras;

            for ( int i = 0; i < span.Length; i++ )
            {
                ref readonly ImmutableArray<Parameter> array = ref span[i];
                sb.Append(' ', indentLevel * 4).Append("(\n");

                indentLevel++;

                foreach ( ref readonly Parameter parameter in array.AsSpan() )
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
