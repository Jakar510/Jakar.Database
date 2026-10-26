// Jakar.Database :: Jakar.Database
// 06/27/2026

namespace Jakar.Database;


/// <summary> Maps between Jakar.Database <see cref="DatabaseType"/> and the lean Jakar.SqlBuilder <see cref="Jakar.SqlBuilder.SqlDialectKind"/>. </summary>
public static class DatabaseTypeMap
{
    public static SqlDialectKind ToSqlDialectKind( this DatabaseType type )
    {
        return type switch
               {
                   DatabaseType.MicrosoftSqlServer => SqlDialectKind.SqlServer,
                   DatabaseType.PostgreSQL         => SqlDialectKind.PostgreSql,
                   DatabaseType.MySQL              => SqlDialectKind.MySql,
                   DatabaseType.Oracle             => SqlDialectKind.Oracle,
                   DatabaseType.Firebird           => SqlDialectKind.Firebird,
                   DatabaseType.SQLite             => SqlDialectKind.Sqlite,
                   _                               => SqlDialectKind.NotSet
               };
    }

    public static DatabaseType ToDatabaseType( SqlDialectKind dialect )
    {
        return dialect switch
               {
                   SqlDialectKind.Sqlite     => DatabaseType.SQLite,
                   SqlDialectKind.SqlServer  => DatabaseType.MicrosoftSqlServer,
                   SqlDialectKind.PostgreSql => DatabaseType.PostgreSQL,
                   SqlDialectKind.MySql      => DatabaseType.MySQL,
                   SqlDialectKind.Oracle     => DatabaseType.Oracle,
                   SqlDialectKind.Firebird   => DatabaseType.Firebird,
                   _                                          => DatabaseType.NotSet
               };
    }
}
