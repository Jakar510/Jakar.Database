// Jakar.Database :: Jakar.Database
// 03/06/2026  22:33

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
        int           length = parameters.Table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + parameters.Count * ( indentLevel * 4 + 3 );
        StringBuilder sb     = Value = new StringBuilder(length);

        int index = 0;
        int count = parameters.Count;

        foreach ( NpgsqlParameter parameter in parameters.SourceProperties )
        {
            sb.Append(' ', indentLevel * 4).Append($" {parameter.SourceColumn} = @{parameter.ParameterName} ");

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
        int           length = parameters.Table.MaxLength_ColumnName * parameters.Table.Count + parameters.Parameters.Sum(static x => x.ParameterName.Length + 10);
        StringBuilder sb     = Value = new StringBuilder(length);

        if ( !parameters.IsGrouped )
        {
            parameters.Table.VariableNames(sb, ref indentLevel);
            return;
        }

        int index = 0;
        int count = parameters.Count;

        for ( int i = 0; i < parameters.Extras.Count; i++ )
        {
            sb.Append(' ', indentLevel * 4).Append("(");
            indentLevel++;

            foreach ( NpgsqlParameter parameter in parameters.Extras[i].Values )
            {
                sb.Append(' ', indentLevel * 4).Append('@').Append(parameter.ParameterName).Append('_').Append(i);

                if ( index++ < count - 1 ) { sb.Append(",\n"); }
            }

            sb.Append(")");
            if ( i < parameters.Extras.Count ) { sb.Append(",\n"); }

            indentLevel--;
        }
    }
    public override string ToString() => Value.ToString();
}
