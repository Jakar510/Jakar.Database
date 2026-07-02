// Jakar.Database :: Jakar.Database
// 03/12/2026  17:48

namespace Jakar.Database;


public readonly struct SqlParameter : IEqualComparable<SqlParameter>
{
    public readonly object?            Value;
    public readonly string             ParameterName;
    public readonly ColumnMetaData     Column;
    public readonly ParameterDirection Direction;
    public readonly DataRowVersion     SourceVersion;


    public SqlParameter( object? value, string parameterName, ColumnMetaData column, ParameterDirection direction, DataRowVersion sourceVersion ) : this(value, parameterName, column, direction, sourceVersion, true) { }
    private SqlParameter( object? value, string parameterName, ColumnMetaData column, ParameterDirection direction, DataRowVersion sourceVersion, bool normalize )
    {
        Value         = value;
        ParameterName = normalize
                            ? parameterName.SqlName()
                            : parameterName;
        Column        = column;
        Direction     = direction;
        SourceVersion = sourceVersion;
    }


    /// <summary> Returns a copy of this parameter whose name carries a numeric suffix, used to keep per-row parameter names unique when several records are inserted in one command (e.g. <c>normalized_name1</c> , <c>normalized_name2</c>). The suffix is appended to the already-normalized name so it is not re-processed by snake-casing. </summary>
    public SqlParameter WithSuffix( int suffix ) => new(Value, $"{ParameterName}{suffix}", Column, Direction, SourceVersion, false);

    /// <summary>
    ///     A value is a literal (safe to inline directly into the SQL text) when it cannot carry a SQL-injection payload: numbers, booleans, dates/times, GUIDs, enums and record ids.
    ///     Strings, JSON and other reference values are treated as injectable and must be bound as parameters instead.
    /// </summary>
    public bool IsLiteral => Value switch
                             {
                                 null or DBNull                                                                                                     => true,
                                 bool or char                                                                                                       => true,
                                 byte or sbyte or short or ushort or int or uint or long or ulong or Int128 or UInt128 or float or double or decimal => true,
                                 Guid or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan                                              => true,
                                 Enum or IRecordID                                                                                                  => true,
                                 _                                                                                                                  => false
                             };


    /// <summary> Appends the value for a VALUES list: the inlined literal for non-injectable values, otherwise a bound <c>@parameter</c>. </summary>
    public void AppendValue( StringBuilder builder )
    {
        if ( IsLiteral ) { AppendLiteral(builder, Value); }
        else { builder.Append('@').Append(ParameterName); }
    }


    /// <summary> Appends a <c>column = value</c> assignment for SET / WHERE clauses. </summary>
    public void AppendAssignment( StringBuilder builder )
    {
        builder.Append(Column.ColumnName).Append(" = ");
        AppendValue(builder);
    }


    /// <summary> Writes a non-injectable value directly into the SQL text: numbers and booleans verbatim, dates/times/GUIDs/etc. single-quoted, enums as their numeric value, and null as <c>NULL</c>. </summary>
    public static void AppendLiteral( StringBuilder builder, object? value )
    {
        switch ( value )
        {
            case null or DBNull:
                builder.Append("NULL");
                return;

            case bool b:
                builder.Append(b);
                return;

            case Enum e:
                builder.Append(Convert.ToInt64(e));
                return;

            case DateTime dt:
                builder.Append('\'').Append(dt.ToString("o", CultureInfo.InvariantCulture)).Append('\'');
                return;

            case DateTimeOffset dto:
                builder.Append('\'').Append(dto.ToString("o", CultureInfo.InvariantCulture)).Append('\'');
                return;

            case DateOnly d:
                builder.Append('\'').Append(d.ToString("o", CultureInfo.InvariantCulture)).Append('\'');
                return;

            case Guid or char or TimeOnly or TimeSpan or IRecordID:
                builder.Append('\'').Append(value).Append('\'');
                return;

            default:
                builder.Append(value);
                return;
        }
    }


    public NpgsqlParameter ToPostgresParameter() => new(ParameterName, Column.PostgresDbType, 0, Column.ColumnName)
                                                    {
                                                        Value         = Value,
                                                        IsNullable    = Column.IsNullable,
                                                        SourceVersion = SourceVersion,
                                                        Direction     = Direction
                                                    };
    public Microsoft.Data.SqlClient.SqlParameter ToSqlParameter() => new(ParameterName, Column.SqlDbType, 0, Column.ColumnName)
                                                                     {
                                                                         Value         = Value,
                                                                         IsNullable    = Column.IsNullable,
                                                                         SourceVersion = SourceVersion,
                                                                         Direction     = Direction
                                                                     };


    public int CompareTo( SqlParameter other ) => CompareTo(in other);
    public int CompareTo( ref readonly SqlParameter other )
    {
        int sourceColumnComparison = string.Compare(Column.ColumnName, other.Column.ColumnName, StringComparison.Ordinal);
        if ( sourceColumnComparison != 0 ) { return sourceColumnComparison; }

        return string.Compare(ParameterName, other.ParameterName, StringComparison.Ordinal);
    }
    public int CompareTo( object? other )
    {
        if ( other is null ) { return 1; }

        return other is SqlParameter x
                   ? CompareTo(in x)
                   : throw new ExpectedValueTypeException(other, typeof(SqlParameter));
    }
    public          bool Equals( SqlParameter              other ) => Equals(in other);
    public          bool Equals( ref readonly SqlParameter other ) => Column.Equals(other.Column) && string.Equals(ParameterName, other.ParameterName, StringComparison.InvariantCulture);
    public override bool Equals( object?                   obj )   => obj is SqlParameter other   && Equals(in other);
    public override int  GetHashCode()                             => HashCode.Combine(Column, ParameterName, Value);


    public static bool operator ==( SqlParameter left, SqlParameter right ) => left.Equals(in right);
    public static bool operator !=( SqlParameter left, SqlParameter right ) => !left.Equals(in right);
    public static bool operator <( SqlParameter  left, SqlParameter right ) => left.CompareTo(in right) < 0;
    public static bool operator >( SqlParameter  left, SqlParameter right ) => left.CompareTo(in right) > 0;
    public static bool operator <=( SqlParameter left, SqlParameter right ) => left.CompareTo(in right) <= 0;
    public static bool operator >=( SqlParameter left, SqlParameter right ) => left.CompareTo(in right) >= 0;
}
