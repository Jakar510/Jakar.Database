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

        int index = 0;
        int count = parameters.Count;

        foreach ( NpgsqlParameter parameter in parameters.Parameters.DistinctBy(static x => x.SourceColumn) )
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
            foreach ( NpgsqlParameter parameter in parameters.parameters ) { sb.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append(",\n"); }
        }
        else
        {
            if ( parameters.parameters.Count > 0 )
            {
                sb.Append(' ', indentLevel * 4).Append('(');
                indentLevel++;
                foreach ( NpgsqlParameter parameter in parameters.parameters ) { sb.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append(",\n"); }

                sb.Append("),\n");
                indentLevel--;
            }

            ReadOnlySpan<ImmutableArray<NpgsqlParameter>> span = parameters.Extras.AsSpan();

            for ( int i = 0; i < span.Length; i++ )
            {
                ref readonly ImmutableArray<NpgsqlParameter> array = ref span[i];
                sb.Append(' ', indentLevel * 4).Append("(\n");

                indentLevel++;

                foreach ( NpgsqlParameter parameter in array )
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
