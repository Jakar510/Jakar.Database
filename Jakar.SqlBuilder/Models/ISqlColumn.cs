// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary>
///     The SQL-relevant shape of a single column, driver-agnostic. Implemented by the host's column metadata
///     (e.g. Jakar.Database <c>ColumnMetaData</c>) so the builder can validate and translate <c>nameof(Record.Property)</c>
///     references without depending on any database driver type.
/// </summary>
public interface ISqlColumn
{
    string PropertyName { get; }
    string ColumnName   { get; }
    Type   ClrType      { get; }
    bool   IsNullable   { get; }
    bool   IsPrimaryKey { get; }
    bool   IsIdentity   { get; }

    string GetTypeName( SqlDialectKind dialect );
    string GetColumnName( SqlDialectKind dialect ) => ColumnName;
}
