// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 03/06/2026  22:33

using System.Collections.Immutable;


namespace Jakar.SqlBuilder;


public readonly struct VariableNames
{
    internal readonly CommandParameters            Parameters;
    /// <summary> The rendered SQL fragment. </summary>
    public readonly string                       Value;
    private readonly  ImmutableArray<CommandParameter> __all;

    /// <summary> Every parameter emitted into the VALUES text, including the per-row suffixed parameters from grouped (multi-row) inserts. The interpolated-string handler binds these, so this must cover the grouped parameters and not just <see cref="CommandParameters.Values"/> (the top-level list, which is empty for a multi-row insert). </summary>
    public ReadOnlySpan<CommandParameter> Values => __all.AsSpan();
    public VariableNames( CommandParameters parameters, int indentLevel )
    {
        Parameters = parameters;
        ValueStringBuilder builder = new(parameters.VariableNameLength);

        if ( !parameters.IsGrouped )
        {
            AppendValues(ref builder, parameters.Values, indentLevel);
            __all = [..parameters.Values];
            Value = builder.ToString();
            return;
        }

        // Multi-row insert: emit one parenthesized tuple per row, separated by commas. Parameter names were suffixed per row in CommandParameters.AddGroup so they stay unique.
        ImmutableArray<CommandParameter>.Builder all = ImmutableArray.CreateBuilder<CommandParameter>(parameters.ParameterCount);
        bool                                 first = true;

        if ( parameters.Count > 0 )
        {
            AppendTuple(ref builder, parameters.Values, indentLevel);
            all.AddRange(parameters.Values);
            first = false;
        }

        foreach ( ref readonly ImmutableArray<CommandParameter> array in parameters.Groups )
        {
            if ( !first ) { builder.Append(",\n"); }

            AppendTuple(ref builder, array.AsSpan(), indentLevel);
            all.AddRange(array);
            first = false;
        }

        __all = all.ToImmutable();
        Value = builder.ToString();
    }


    /// <summary> Appends a comma-and-newline separated list of values, one per line, with no wrapping parentheses and no trailing comma. </summary>
    private static void AppendValues( ref ValueStringBuilder builder, ReadOnlySpan<CommandParameter> values, int indentLevel )
    {
        for ( int i = 0; i < values.Length; i++ )
        {
            builder.Append(' ', indentLevel * 4);
            values[i].AppendValue(ref builder);
            if ( i < values.Length - 1 ) { builder.Append(','); }
            builder.Append('\n');
        }

        if ( values.Length > 0 ) { builder.Length--; } // drop the trailing newline
    }


    /// <summary> Appends a single parenthesized VALUES tuple for one row. </summary>
    private static void AppendTuple( ref ValueStringBuilder builder, ReadOnlySpan<CommandParameter> values, int indentLevel )
    {
        builder.Append(' ', indentLevel * 4).Append("(\n");
        AppendValues(ref builder, values, indentLevel + 1);
        builder.Append('\n').Append(' ', indentLevel * 4).Append(')');
    }


    public override string ToString() => Value;
}
