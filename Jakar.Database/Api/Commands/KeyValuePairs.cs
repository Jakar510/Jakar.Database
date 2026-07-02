namespace Jakar.Database;


public readonly struct KeyValuePairs
{
    internal readonly CommandParameters          Parameters;
    internal readonly StringBuilder              Value;
    public            ReadOnlySpan<SqlParameter> Values => Parameters.Values;
    public KeyValuePairs( CommandParameters parameters, int indentLevel, params ReadOnlySpan<char> separator )
    {
        Parameters = parameters;
        Value      = new StringBuilder(parameters.KeyValuePairLength(indentLevel));
        ReadOnlySpan<SqlParameter> buffer = parameters.Values;

        // Every pair is emitted at the same indentation so the block stays aligned.
        // Continuation-line alignment relative to the surrounding statement is handled by the interpolated-string handler (hang indent).
        for ( int i = 0; i < buffer.Length; i++ )
        {
            ref readonly SqlParameter parameter = ref buffer[i];
            if ( i > 0 ) { Value.Append(",\n"); }

            Value.Append(' ', indentLevel * 4);
            parameter.AppendAssignment(Value);
        }
    }
    public override string ToString() => Value.ToString();
}