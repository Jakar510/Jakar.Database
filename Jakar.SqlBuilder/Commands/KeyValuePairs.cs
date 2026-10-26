namespace Jakar.SqlBuilder;


public readonly struct KeyValuePairs
{
    internal readonly CommandParameters          Parameters;
    /// <summary> The rendered SQL fragment. </summary>
    public readonly string                     Value;
    public            ReadOnlySpan<CommandParameter> Values => Parameters.Values;
    public KeyValuePairs( CommandParameters parameters, int indentLevel, params ReadOnlySpan<char> separator )
    {
        Parameters = parameters;
        ValueStringBuilder             builder = new(parameters.KeyValuePairLength(indentLevel));
        ReadOnlySpan<CommandParameter> buffer  = parameters.Values;

        // Every pair is emitted at the same indentation so the block stays aligned.
        // Continuation-line alignment relative to the surrounding statement is handled by the interpolated-string handler (hang indent).
        for ( int i = 0; i < buffer.Length; i++ )
        {
            ref readonly CommandParameter parameter = ref buffer[i];
            if ( i > 0 ) { builder.Append(",\n"); }

            builder.Append(' ', indentLevel * 4);
            parameter.AppendAssignment(ref builder);
        }

        Value = builder.ToString();
    }
    public override string ToString() => Value;
}