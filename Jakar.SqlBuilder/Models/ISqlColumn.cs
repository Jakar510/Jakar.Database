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


    // ---- DDL (CREATE TABLE from metadata); the defaults describe a plain column ----

    bool IsUnique         => false;
    bool IsAlwaysIdentity => false;

    /// <summary> The column's <c>DEFAULT</c> expression in <paramref name="dialect"/> (trusted SQL), or <see langword="null"/>. </summary>
    string? GetDefaultSql( SqlDialectKind dialect ) => null;

    /// <summary> The column's <c>CHECK</c> condition in <paramref name="dialect"/> (trusted SQL), or <see langword="null"/>. </summary>
    string? GetCheckSql( SqlDialectKind dialect ) => null;

    /// <summary> The table this column references, or <see langword="null"/>. </summary>
    string? ForeignTable => null;

    /// <summary> The referenced column (<c>id</c> when <see langword="null"/>). </summary>
    string? ForeignColumn => null;

    SqlReferentialAction OnDelete => SqlReferentialAction.NoAction;
    SqlReferentialAction OnUpdate => SqlReferentialAction.NoAction;
}
