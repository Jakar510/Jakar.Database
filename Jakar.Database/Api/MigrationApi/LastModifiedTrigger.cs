// Jakar.Database :: Jakar.Database
// 10/25/2026

namespace Jakar.Database;


/// <summary>
///     The trigger that keeps a table's <c>last_modified</c> column current on every insert and update, in each dialect's form:
///     <list type="bullet">
///         <item> PostgreSQL: a <c>BEFORE INSERT OR UPDATE</c> row trigger calling the shared <see cref="MigrationRecord.SetLastModifiedName"/> function (<see cref="MigrationRecord.SetLastModified"/>). </item>
///         <item> MySQL, Oracle, Firebird: <c>BEFORE</c> row triggers that set <c>NEW.last_modified</c> (MySQL needs one trigger per event). </item>
///         <item> SQL Server and SQLite, which can't change the row before it's written: <c>AFTER</c> triggers that update the written rows by key. Both engines leave recursive triggers off by default, so the update doesn't fire them again. </item>
///     </list>
///     The application's own value is always overwritten, as on PostgreSQL.
///     <para>
///         MySQL with binary logging on (its default) lets a user without <c>SUPER</c> create triggers only when the server runs with <c>log_bin_trust_function_creators=1</c>; the user also
///         needs the <c>TRIGGER</c> privilege.
///     </para>
/// </summary>
public static class LastModifiedTrigger
{
    /// <param name="trigger"> The trigger's name (MySQL and SQLite add <c>_insert</c> / <c>_update</c>). </param>
    /// <param name="table"> The table. </param>
    /// <param name="column"> The <c>last_modified</c> column. </param>
    /// <param name="keys"> The table's key columns; SQL Server finds the written rows by them. </param>
    public static string Create( DatabaseType type, string trigger, string table, string column, IReadOnlyList<string> keys )
    {
        SqlDialectKind     dialect = type.ToSqlDialectKind();
        ValueStringBuilder sql     = new(512);

        switch ( type )
        {
            case DatabaseType.PostgreSQL:
                sql.Append("CREATE OR REPLACE TRIGGER ");
                Quote(ref sql, dialect, trigger);
                sql.Append(" BEFORE INSERT OR UPDATE ON ");
                Quote(ref sql, dialect, table);
                sql.Append(" FOR EACH ROW EXECUTE FUNCTION ").Append(MigrationRecord.SetLastModifiedName).Append("();");
                break;

            case DatabaseType.MicrosoftSqlServer:
                sql.Append("CREATE OR ALTER TRIGGER ");
                Quote(ref sql, dialect, trigger);
                sql.Append(" ON ");
                Quote(ref sql, dialect, table);
                sql.Append(" AFTER INSERT, UPDATE AS\nBEGIN\n    SET NOCOUNT ON;\n    UPDATE target SET ");
                Quote(ref sql, dialect, column);
                sql.Append(" = SYSDATETIMEOFFSET() FROM ");
                Quote(ref sql, dialect, table);
                sql.Append(" target JOIN inserted ON ");
                KeyJoin(ref sql, dialect, keys);
                sql.Append(";\nEND;");
                break;

            case DatabaseType.SQLite:
                SqliteTrigger(ref sql, dialect, trigger, "_insert", "INSERT", table, column);
                sql.Append('\n');
                SqliteTrigger(ref sql, dialect, trigger, "_update", "UPDATE", table, column);
                break;

            case DatabaseType.MySQL:
                MySqlTrigger(ref sql, dialect, trigger, "_insert", "INSERT", table, column);
                sql.Append('\n');
                MySqlTrigger(ref sql, dialect, trigger, "_update", "UPDATE", table, column);
                break;

            // A PL/SQL block: its closing ';' belongs to the block.
            case DatabaseType.Oracle:
                sql.Append("CREATE OR REPLACE TRIGGER ");
                Quote(ref sql, dialect, trigger);
                sql.Append(" BEFORE INSERT OR UPDATE ON ");
                Quote(ref sql, dialect, table);
                sql.Append(" FOR EACH ROW BEGIN :NEW.");
                Quote(ref sql, dialect, column);
                sql.Append(" := SYSTIMESTAMP; END;");
                break;

            case DatabaseType.Firebird:
                sql.Append("CREATE OR ALTER TRIGGER ");
                Quote(ref sql, dialect, trigger);
                sql.Append(" FOR ");
                Quote(ref sql, dialect, table);
                sql.Append(" ACTIVE BEFORE INSERT OR UPDATE POSITION 0 AS BEGIN NEW.");
                Quote(ref sql, dialect, column);
                sql.Append(" = CURRENT_TIMESTAMP; END");
                break;

            default:
                sql.Dispose();
                throw new OutOfRangeException(type);
        }

        return sql.ToString();
    }

    /// <summary> Drops what <see cref="Create"/> made. </summary>
    public static string Drop( DatabaseType type, string trigger, string table )
    {
        SqlDialectKind     dialect = type.ToSqlDialectKind();
        ValueStringBuilder sql     = new(128);

        switch ( type )
        {
            case DatabaseType.PostgreSQL:
                sql.Append("DROP TRIGGER IF EXISTS ");
                Quote(ref sql, dialect, trigger);
                sql.Append(" ON ");
                Quote(ref sql, dialect, table);
                sql.Append(';');
                break;

            case DatabaseType.MicrosoftSqlServer:
                sql.Append("DROP TRIGGER IF EXISTS ");
                Quote(ref sql, dialect, trigger);
                sql.Append(';');
                break;

            case DatabaseType.SQLite or DatabaseType.MySQL:
                sql.Append("DROP TRIGGER IF EXISTS ");
                Quote(ref sql, dialect, trigger, "_insert");
                sql.Append("; DROP TRIGGER IF EXISTS ");
                Quote(ref sql, dialect, trigger, "_update");
                sql.Append(';');
                break;

            case DatabaseType.Oracle:
                sql.Append("DROP TRIGGER ");
                Quote(ref sql, dialect, trigger);
                break;

            case DatabaseType.Firebird:
                sql.Append("DROP TRIGGER ");
                Quote(ref sql, dialect, trigger);
                sql.Append(';');
                break;

            default:
                sql.Dispose();
                throw new OutOfRangeException(type);
        }

        return sql.ToString();
    }


    // The text form Microsoft.Data.Sqlite reads back as a DateTimeOffset.
    private const string SQLITE_NOW = "strftime('%Y-%m-%d %H:%M:%f', 'now') || '+00:00'";

    /// <summary> One SQLite <c>AFTER</c> trigger (SQLite has no multi-event triggers). </summary>
    private static void SqliteTrigger( ref ValueStringBuilder sql, SqlDialectKind dialect, string trigger, string suffix, string statement, string table, string column )
    {
        sql.Append("CREATE TRIGGER IF NOT EXISTS ");
        Quote(ref sql, dialect, trigger, suffix);
        sql.Append(" AFTER ").Append(statement).Append(" ON ");
        Quote(ref sql, dialect, table);
        sql.Append(" FOR EACH ROW\nBEGIN\n    UPDATE ");
        Quote(ref sql, dialect, table);
        sql.Append(" SET ");
        Quote(ref sql, dialect, column);
        sql.Append(" = ").Append(SQLITE_NOW).Append(" WHERE rowid = NEW.rowid;\nEND;");
    }

    /// <summary> One MySQL <c>BEFORE</c> trigger (MySQL needs one trigger per event). </summary>
    private static void MySqlTrigger( ref ValueStringBuilder sql, SqlDialectKind dialect, string trigger, string suffix, string statement, string table, string column )
    {
        sql.Append("CREATE TRIGGER IF NOT EXISTS ");
        Quote(ref sql, dialect, trigger, suffix);
        sql.Append(" BEFORE ").Append(statement).Append(" ON ");
        Quote(ref sql, dialect, table);
        sql.Append(" FOR EACH ROW SET NEW.");
        Quote(ref sql, dialect, column);
        sql.Append(" = UTC_TIMESTAMP(6);");
    }

    /// <summary> SQL Server's <c>target.k = inserted.k AND ...</c> over the key columns. </summary>
    private static void KeyJoin( ref ValueStringBuilder sql, SqlDialectKind dialect, IReadOnlyList<string> keys )
    {
        if ( keys.Count is 0 ) { throw new InvalidOperationException("SQL Server's last_modified trigger needs the table's key columns."); }

        for ( int i = 0; i < keys.Count; i++ )
        {
            if ( i > 0 ) { sql.Append(" AND "); }

            sql.Append("target.");
            Quote(ref sql, dialect, keys[i]);
            sql.Append(" = inserted.");
            Quote(ref sql, dialect, keys[i]);
        }
    }

    /// <summary>
    ///     Writes <paramref name="name"/> folded and quoted the way Jakar.SqlBuilder writes it, so the trigger names the same table and column the DDL created. Nothing is allocated:
    ///     the folded name comes from the builder's cache, and a closing quote inside it is doubled as it's copied.
    /// </summary>
    /// <param name="suffix"> Written after the folded name, inside the quotes; it must already be in folded form (e.g. <c>_insert</c>). </param>
    private static void Quote( ref ValueStringBuilder sql, SqlDialectKind dialect, string name, scoped ReadOnlySpan<char> suffix = default )
    {
        string folded = dialect.Fold(name);
        ( char open, char close ) = dialect.QuoteChars();

        if ( open is '\0' )
        {
            sql.Append(folded).Append(suffix);
            return;
        }

        sql.Append(open);
        AppendEscaped(ref sql, folded, close);
        AppendEscaped(ref sql, suffix, close);
        sql.Append(close);
    }

    /// <summary> Copies <paramref name="text"/>, doubling every <paramref name="close"/> quote. </summary>
    private static void AppendEscaped( ref ValueStringBuilder sql, scoped ReadOnlySpan<char> text, char close )
    {
        int index;
        while ( ( index = text.IndexOf(close) ) >= 0 )
        {
            sql.Append(text[..( index + 1 )]).Append(close);
            text = text[( index + 1 )..];
        }

        sql.Append(text);
    }
}
