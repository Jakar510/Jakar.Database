// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Resolves <c>nameof(Record.Property)</c> references to mapped columns and validates them against the record's metadata. </summary>
internal static class TypedColumns
{
    public static ISqlColumn Resolve<T>( string propertyName, scoped in SqlWriter writer )
        where T : ISqlTable<T>
    {
        if ( T.TrySqlColumn(propertyName, out ISqlColumn? column) && column is not null ) { return column; }

        throw new SqlBuildException(SqlBuildError.UnknownColumn, $"'{propertyName}' is not a mapped column of '{typeof(T).Name}'.", writer.Snapshot(), writer.Dialect);
    }
}
