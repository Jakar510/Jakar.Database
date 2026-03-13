// Jakar.Database :: Jakar.Database
// 03/12/2026  17:48

namespace Jakar.Database;


public readonly record struct SqlParameter( object? Value, string ParameterName, ColumnMetaData Column, ParameterDirection Direction, DataRowVersion SourceVersion ) : IComparable<SqlParameter>, IComparable
{
    public readonly object?            Value         = Value ?? DBNull.Value;
    public readonly string             ParameterName = ParameterName.SqlName();
    public readonly ColumnMetaData     Column        = Column;
    public readonly ParameterDirection Direction     = Direction;
    public readonly DataRowVersion     SourceVersion = SourceVersion; 


    public NpgsqlParameter ToPostgresParameter() => new(ParameterName, Column.PostgresDbType, 0, Column.ColumnName)
                                                    {
                                                        Value         = Value,
                                                        IsNullable    = Column.IsNullable,
                                                        SourceVersion = SourceVersion,
                                                        Direction     = Direction,
                                                    };
    public Microsoft.Data.SqlClient.SqlParameter ToSqlParameter() => new(ParameterName, Column.SqlDbType, 0, Column.ColumnName)
                                                                     {
                                                                         Value         = Value,
                                                                         IsNullable    = Column.IsNullable,
                                                                         SourceVersion = SourceVersion,
                                                                         Direction     = Direction,
                                                                     };


    public int CompareTo( SqlParameter other )
    {
        int indexComparison = Column.Index.CompareTo(other.Column.Index);
        if ( indexComparison != 0 ) { return indexComparison; }

        int sourceColumnComparison = string.Compare(Column.ColumnName, other.Column.ColumnName, StringComparison.Ordinal);
        if ( sourceColumnComparison != 0 ) { return sourceColumnComparison; }

        return string.Compare(ParameterName, other.ParameterName, StringComparison.Ordinal);
    }
    public int CompareTo( object? obj )
    {
        if ( obj is null ) { return 1; }

        return obj is SqlParameter other
                   ? CompareTo(other)
                   : throw new ArgumentException($"Object must be of type {nameof(SqlParameter)}");
    }
    public static bool operator <( SqlParameter  left, SqlParameter right ) => left.CompareTo(right) < 0;
    public static bool operator >( SqlParameter  left, SqlParameter right ) => left.CompareTo(right) > 0;
    public static bool operator <=( SqlParameter left, SqlParameter right ) => left.CompareTo(right) <= 0;
    public static bool operator >=( SqlParameter left, SqlParameter right ) => left.CompareTo(right) >= 0;
}
