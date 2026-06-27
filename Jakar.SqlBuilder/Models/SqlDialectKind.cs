// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> The SQL dialect a query is rendered for. Carried by <see cref="SqlWriter"/> and switched on when emitting dialect-divergent syntax. </summary>
public enum SqlDialectKind
{
    NotSet = 0,
    Sqlite,
    SqlServer,
    PostgreSql,
    MySql,
    Oracle,
    Firebird
}
