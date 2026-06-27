// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary>
///     Fluent entry point for the validated, low-allocation SQL builder. Choose a dialect from the named accessors
///     (<c>SqlBuilder.PostgreSQL.Select(...)</c>) or via <see cref="For(SqlDialectKind, SqlBuilderOptions?)"/> when the
///     dialect is only known at runtime. Both styles return the same <see cref="SqlRoot"/>.
/// </summary>
/// <remarks> Additive: coexists with the legacy <c>SqlCommand</c> and <c>SqlInterpolatedStringHandler</c>. </remarks>
public static class SqlBuilder
{
    public static SqlRoot PostgreSQL => new(SqlDialectKind.PostgreSql, SqlBuilderOptions.Default);
    public static SqlRoot SqlServer  => new(SqlDialectKind.SqlServer,  SqlBuilderOptions.Default);
    public static SqlRoot Sqlite     => new(SqlDialectKind.Sqlite,     SqlBuilderOptions.Default);

    public static SqlRoot For( SqlDialectKind dialect, SqlBuilderOptions? options = null ) => new(dialect, options ?? SqlBuilderOptions.Default);

    public static SqlRoot PostgreSQLWith( SqlBuilderOptions options ) => new(SqlDialectKind.PostgreSql, options);
    public static SqlRoot SqlServerWith( SqlBuilderOptions  options ) => new(SqlDialectKind.SqlServer,  options);
    public static SqlRoot SqliteWith( SqlBuilderOptions     options ) => new(SqlDialectKind.Sqlite,     options);
}
