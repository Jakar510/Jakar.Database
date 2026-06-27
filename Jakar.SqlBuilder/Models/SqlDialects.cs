// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Dialect-divergent rendering, dispatched on <see cref="SqlDialectKind"/>. </summary>
public static class SqlDialects
{
    /// <summary> PostgreSQL identifier folder. Defaults to invariant snake_case + lower. Hosts may replace it (e.g. Jakar.Database <c>PostgresParams.SqlName</c>). </summary>
    public static Func<string, string> PostgresIdentifierFolder { get; set; } = DefaultSnakeCaseLower;

    public static bool SupportsReturning( SqlDialectKind dialect )        => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite;
    public static bool SupportsOutputClause( SqlDialectKind dialect )     => dialect is SqlDialectKind.SqlServer;
    public static bool SupportsFullOuterJoin( SqlDialectKind dialect )    => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.SqlServer or SqlDialectKind.Sqlite;
    public static bool SupportsBooleanLiteral( SqlDialectKind dialect )   => dialect is SqlDialectKind.PostgreSql;
    public static bool RequiresOrderByForOffset( SqlDialectKind dialect ) => dialect is SqlDialectKind.SqlServer;
    public static bool FoldsIdentifiersToLower( SqlDialectKind dialect )  => dialect is SqlDialectKind.PostgreSql;

    public static string ParameterName( SqlDialectKind dialect, int ordinal )
    {
        return dialect switch
               {
                   SqlDialectKind.PostgreSql => $"${ordinal + 1}",
                   SqlDialectKind.SqlServer  => $"@p{ordinal}",
                   SqlDialectKind.Sqlite     => $"@p{ordinal}",
                   _                         => $"@p{ordinal}"
               };
    }

    public static (char Open, char Close) QuoteChars( SqlDialectKind dialect )
    {
        return dialect switch
               {
                   SqlDialectKind.SqlServer  => ( '[', ']' ),
                   SqlDialectKind.Sqlite     => ( '"', '"' ),
                   SqlDialectKind.PostgreSql => ( '\0', '\0' ),
                   _                         => ( '"', '"' )
               };
    }

    public static string Fold( SqlDialectKind dialect, string name )
    {
        return dialect is SqlDialectKind.PostgreSql
                   ? PostgresIdentifierFolder(name)
                   : name;
    }

    internal static string DefaultSnakeCaseLower( string name )
    {
        if ( string.IsNullOrEmpty(name) ) { return name; }

        StringBuilder builder = new(name.Length + 8);
        for ( int i = 0; i < name.Length; i++ )
        {
            char c = name[i];
            if ( char.IsUpper(c) )
            {
                if ( i > 0 && name[i - 1] is not '_' ) { builder.Append('_'); }

                builder.Append(char.ToLowerInvariant(c));
            }
            else { builder.Append(c); }
        }

        return builder.ToString();
    }
}
