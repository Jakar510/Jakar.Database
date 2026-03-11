// Jakar.Database :: Jakar.Database
// 03/10/2026  14:13


namespace Jakar.Database;


[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DatabaseType
{
    /// <summary>
    /// The default Value: either has no connection or is not yet configured
    /// </summary>
    NotSet,

    /*
    /// <summary>
    /// Represents the SQLite database type used for identifying and configuring connections to SQLite databases. <see cref="Microsoft.Data.Sqlite.SqliteConnection"/>
    /// </summary>
    /// <remarks>
    /// Use this value when specifying a database type for operations that require interaction with an SQLite database.
    /// This type is commonly used in scenarios where lightweight, file-based storage is needed.
    /// Ensure that the database file exists and is accessible before attempting to connect.
    /// </remarks>
    SQLite,
    */

    /// <summary>
    /// Specifies the PostgreSQL database type.<see cref="Npgsql.NpgsqlConnection"/>
    /// </summary>
    /// <remarks>
    /// Use this value to indicate that a database connection or operation targets a PostgreSQL database.
    /// This type may affect connection string formatting, supported features, and compatibility with database commands.</remarks>
    PostgreSQL,

    /// <summary>
    /// Represents a Microsoft SQL Server database connection. <see cref="Microsoft.Data.SqlClient.SqlConnection"/>
    /// </summary>
    /// <remarks>
    /// This class provides methods and properties to interact with a Microsoft SQL Server database, including executing commands and managing transactions.</remarks>
    MicrosoftSql,

    /// <summary>
    /// Represents the Oracle database type. <see cref="Oracle.ManagedDataAccess.Client.OracleConnection"/>
    /// </summary>
    /// <remarks>
    /// Use this value to specify Oracle as the target database when configuring database connections or operations.</remarks>
    Oracle,

    /// <summary>
    /// Represents the MySQL database type used for identifying and configuring MySQL connections within the application. <see cref="MySqlConnector.MySqlConnection"/>
    /// </summary>
    /// <remarks>
    /// Use this database type when working with MySQL-specific features or when establishing connections to a MySQL server.
    /// This value may be used to select appropriate connection strings, drivers, or behaviors tailored to MySQL databases.</remarks>
    MySQL,

    /// <summary>
    /// Represents the Firebird database type used for identifying Firebird database connections. <see cref="FirebirdSql.Data.FirebirdClient.FbConnection"/>
    /// </summary>
    /// <remarks>
    /// Use this value to specify Firebird as the target database when configuring database operations or connections.
    /// This type is typically used in scenarios where multiple database types are supported and selection is required.</remarks>
    Firebird,
}
