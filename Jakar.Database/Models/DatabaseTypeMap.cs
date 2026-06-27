// Jakar.Database :: Jakar.Database
// 06/27/2026

namespace Jakar.Database;


/// <summary> Maps between Jakar.Database <see cref="DatabaseType"/> and the lean Jakar.SqlBuilder <see cref="Jakar.SqlBuilder.SqlDialectKind"/>. </summary>
public static class DatabaseTypeMap
{
    public static Jakar.SqlBuilder.SqlDialectKind ToSqlDialectKind( this DatabaseType type )
    {
        return type switch
               {
                   DatabaseType.MicrosoftSqlServer => Jakar.SqlBuilder.SqlDialectKind.SqlServer,
                   DatabaseType.PostgreSQL         => Jakar.SqlBuilder.SqlDialectKind.PostgreSql,
                   DatabaseType.MySQL              => Jakar.SqlBuilder.SqlDialectKind.MySql,
                   DatabaseType.Oracle             => Jakar.SqlBuilder.SqlDialectKind.Oracle,
                   DatabaseType.Firebird           => Jakar.SqlBuilder.SqlDialectKind.Firebird,
                   _                               => Jakar.SqlBuilder.SqlDialectKind.NotSet
               };
    }

    public static DatabaseType ToDatabaseType( Jakar.SqlBuilder.SqlDialectKind dialect )
    {
        return dialect switch
               {
                   // NOTE: DatabaseType.SQLite is commented out in DatabaseType.cs; re-enable it (and its wiring) to map SQLite here.
                   Jakar.SqlBuilder.SqlDialectKind.Sqlite     => DatabaseType.NotSet,
                   Jakar.SqlBuilder.SqlDialectKind.SqlServer  => DatabaseType.MicrosoftSqlServer,
                   Jakar.SqlBuilder.SqlDialectKind.PostgreSql => DatabaseType.PostgreSQL,
                   Jakar.SqlBuilder.SqlDialectKind.MySql      => DatabaseType.MySQL,
                   Jakar.SqlBuilder.SqlDialectKind.Oracle     => DatabaseType.Oracle,
                   Jakar.SqlBuilder.SqlDialectKind.Firebird   => DatabaseType.Firebird,
                   _                                          => DatabaseType.NotSet
               };
    }
}
