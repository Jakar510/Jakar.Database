// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 03/12/2026  17:48

using System.Data;
using System.Globalization;
using Jakar.Extensions;
using Jakar.Json;



namespace Jakar.SqlBuilder;


/// <summary> A value bound to one column of a <see cref="CommandParameters"/>: its parameter name, the column, and ADO.NET direction / source version. Hosts convert it to their driver's parameter type. </summary>
public readonly struct CommandParameter : IEqualComparable<CommandParameter>
{
    public readonly object?            Value;
    public readonly string             ParameterName;
    public readonly ISqlCommandColumn  Column;
    public readonly ParameterDirection Direction;
    public readonly DataRowVersion     SourceVersion;


    /// <param name="parameterName"> Used as given; <see cref="ISqlCommandTable.GetParameterName"/> produces the table's naming. </param>
    public CommandParameter( object? value, string parameterName, ISqlCommandColumn column, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        // Values the providers can't bind are converted here: Jakar.Json's DOM as its JSON text, ISqlParameterValue (e.g. record ids) as its underlying value.
        Value = value switch
                {
                    JNode node              => node.ToJson(),
                    ISqlParameterValue item => item.ParameterValue,
                    _                       => value
                };

        ParameterName = parameterName;
        Column        = column;
        Direction     = direction;
        SourceVersion = sourceVersion;
    }


    /// <summary> Returns a copy of this parameter whose name carries a numeric suffix, used to keep per-row parameter names unique when several records are inserted in one command (e.g. <c>normalized_name1</c> , <c>normalized_name2</c>). </summary>
    public CommandParameter WithSuffix( int suffix ) => new(Value, $"{ParameterName}{suffix}", Column, Direction, SourceVersion);

    /// <summary>
    ///     A value is a literal (safe to inline directly into the SQL text) when it cannot carry a SQL-injection payload: numbers, booleans, dates/times, GUIDs and enums (record ids arrive as their <see cref="Guid"/>).
    ///     Strings, JSON and other reference values are treated as injectable and must be bound as parameters instead.
    /// </summary>
    public bool IsLiteral => Value switch
                             {
                                 null or DBNull                                                                                                      => true,
                                 bool or char                                                                                                        => true,
                                 byte or sbyte or short or ushort or int or uint or long or ulong or Int128 or UInt128 or float or double or decimal => true,
                                 Guid or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan                                              => true,
                                 Enum                                                                                                                => true,
                                 _                                                                                                                   => false
                             };


    /// <summary> Appends the value for a VALUES list: the inlined literal for non-injectable values, otherwise a bound <c>@parameter</c>. </summary>
    public void AppendValue( ref ValueStringBuilder builder )
    {
        if ( IsLiteral ) { AppendLiteral(ref builder, Value); }
        else { builder.Append('@').Append(ParameterName); }
    }


    /// <summary> Appends a <c>column = value</c> assignment for SET / WHERE clauses. </summary>
    public void AppendAssignment( ref ValueStringBuilder builder )
    {
        builder.Append(Column.ColumnName).Append(" = ");
        AppendValue(ref builder);
    }


    /// <summary> Writes a non-injectable value directly into the SQL text: numbers and booleans verbatim, dates/times/GUIDs/etc. single-quoted, enums as their numeric value, and null as <c>NULL</c>. </summary>
    public static void AppendLiteral( ref ValueStringBuilder builder, object? value )
    {
        switch ( value )
        {
            case null or DBNull:
                builder.Append("NULL");
                return;

            case bool b:
                builder.Append(b ? bool.TrueString : bool.FalseString);
                return;

            case Enum e:
                builder.AppendSpanFormattable(Convert.ToInt64(e), default, CultureInfo.InvariantCulture);
                return;

            case DateTime dt:
                builder.Append('\'').AppendSpanFormattable(dt, "o", CultureInfo.InvariantCulture).Append('\'');
                return;

            case DateTimeOffset dto:
                builder.Append('\'').AppendSpanFormattable(dto, "o", CultureInfo.InvariantCulture).Append('\'');
                return;

            case DateOnly d:
                builder.Append('\'').AppendSpanFormattable(d, "o", CultureInfo.InvariantCulture).Append('\'');
                return;

            case TimeOnly t:
                builder.Append('\'').AppendSpanFormattable(t, "o", CultureInfo.InvariantCulture).Append('\'');
                return;

            case Guid or char or TimeSpan or ISqlParameterValue:
                builder.Append('\'');
                AppendObject(ref builder, value);
                builder.Append('\'');
                return;

            default:
                AppendObject(ref builder, value);
                return;
        }
    }

    /// <summary> <paramref name="value"/> formatted in place when it can be (culture-invariant), otherwise its <see cref="object.ToString"/>. </summary>
    private static void AppendObject( ref ValueStringBuilder builder, object value )
    {
        if ( value is ISpanFormattable formattable ) { builder.AppendSpanFormattable(formattable, default, CultureInfo.InvariantCulture); }
        else { builder.Append(value.ToString()); }
    }


    public int CompareTo( CommandParameter other ) => CompareTo(in other);
    public int CompareTo( ref readonly CommandParameter other )
    {
        int sourceColumnComparison = string.Compare(Column.ColumnName, other.Column.ColumnName, StringComparison.Ordinal);
        if ( sourceColumnComparison != 0 ) { return sourceColumnComparison; }

        return string.Compare(ParameterName, other.ParameterName, StringComparison.Ordinal);
    }
    public int CompareTo( object? other )
    {
        if ( other is null ) { return 1; }

        return other is CommandParameter x
                   ? CompareTo(in x)
                   : throw new ExpectedValueTypeException(other, typeof(CommandParameter));
    }
    public          bool Equals( CommandParameter              other ) => Equals(in other);
    public          bool Equals( ref readonly CommandParameter other ) => Column.Equals(other.Column) && string.Equals(ParameterName, other.ParameterName, StringComparison.InvariantCulture);
    public override bool Equals( object?                       obj )   => obj is CommandParameter other && Equals(in other);
    public override int  GetHashCode()                                 => HashCode.Combine(Column, ParameterName, Value);


    public static bool operator ==( CommandParameter left, CommandParameter right ) => left.Equals(in right);
    public static bool operator !=( CommandParameter left, CommandParameter right ) => !left.Equals(in right);
    public static bool operator <( CommandParameter  left, CommandParameter right ) => left.CompareTo(in right) < 0;
    public static bool operator >( CommandParameter  left, CommandParameter right ) => left.CompareTo(in right) > 0;
    public static bool operator <=( CommandParameter left, CommandParameter right ) => left.CompareTo(in right) <= 0;
    public static bool operator >=( CommandParameter left, CommandParameter right ) => left.CompareTo(in right) >= 0;
}
