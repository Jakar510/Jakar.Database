// Jakar.Database :: Jakar.Database
// 03/12/2026  17:48

namespace Jakar.Database;


public readonly struct SqlParameter( object? value, string parameterName, ColumnMetaData column, ParameterDirection direction, DataRowVersion sourceVersion ) : IEqualComparable<SqlParameter>
{
    public readonly object?            Value         = value ?? DBNull.Value;
    public readonly string             ParameterName = parameterName.SqlName();
    public readonly ColumnMetaData     Column        = column;
    public readonly ParameterDirection Direction     = direction;
    public readonly DataRowVersion     SourceVersion = sourceVersion;


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
