using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using MySqlConnector;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class ParameterConversionTests : Assert
{
    private static CommandParameter Parameter( string propertyName, object? value ) => UserRecord.MetaData[propertyName].ToParameter(value, propertyName);


    [Test] public void Type_Maps_Cover_Common_Columns()
    {
        Multiple(() =>
                 {
                     That(DbColumnType.Long.ToDbType(),      Is.EqualTo(DbType.Int64));
                     That(DbColumnType.Guid.ToDbType(),      Is.EqualTo(DbType.Guid));
                     That(DbColumnType.Json.ToDbType(),      Is.EqualTo(DbType.String));
                     That(DbColumnType.DateRange.ToDbType(), Is.EqualTo(DbType.Object));

                     That(DbColumnType.Int.ToSqliteType(),            Is.EqualTo(SqliteType.Integer));
                     That(DbColumnType.Double.ToSqliteType(),         Is.EqualTo(SqliteType.Real));
                     That(DbColumnType.Binary.ToSqliteType(),         Is.EqualTo(SqliteType.Blob));
                     That(DbColumnType.Guid.ToSqliteType(),           Is.EqualTo(SqliteType.Text));
                     That(DbColumnType.DateTimeOffset.ToSqliteType(), Is.EqualTo(SqliteType.Text));

                     That(DbColumnType.Boolean.ToMySqlDbType(),               Is.EqualTo(MySqlDbType.Bool));
                     That(DbColumnType.Decimal.ToMySqlDbType(),               Is.EqualTo(MySqlDbType.NewDecimal));
                     That(DbColumnType.Json.ToMySqlDbType(),                  Is.EqualTo(MySqlDbType.JSON));
                     That(DbColumnType.Guid.ToMySqlDbType(),                  Is.EqualTo(MySqlDbType.Guid));
                     That(static () => DbColumnType.TsVector.ToMySqlDbType(), NUnit.Framework.Throws.InstanceOf<Exception>());
                 });
    }

    [Test] public void Conversions_Carry_Name_Type_And_Value()
    {
        CommandParameter name = Parameter(nameof(UserRecord.UserName), "tester");

        SqliteParameter sqlite = name.ToSqliteParameter();
        MySqlParameter  mySql  = name.ToMySqlParameter();

        using SqliteCommand command = new();
        DbParameter         generic = name.ToDbParameter(command);

        Multiple(() =>
                 {
                     That(sqlite.ParameterName, Is.EqualTo(name.ParameterName));
                     That(sqlite.SqliteType,    Is.EqualTo(SqliteType.Text));
                     That(sqlite.Value,         Is.EqualTo("tester"));

                     That(mySql.ParameterName, Is.EqualTo(name.ParameterName));
                     That(mySql.MySqlDbType,   Is.EqualTo(MySqlDbType.VarChar));
                     That(mySql.Value,         Is.EqualTo("tester"));

                     That(generic,               Is.InstanceOf<SqliteParameter>());
                     That(generic.ParameterName, Is.EqualTo(name.ParameterName));
                     That(generic.DbType,        Is.EqualTo(DbType.String));
                     That(generic.SourceColumn,  Is.EqualTo(UserRecord.MetaData[nameof(UserRecord.UserName)].ColumnName));
                 });
    }

    [Test] public void Null_Values_Are_Sent_As_DBNull()
    {
        CommandParameter empty = Parameter(nameof(UserRecord.Email), null);

        using SqliteCommand command = new();

        Multiple(() =>
                 {
                     That(empty.ToPostgresParameter().Value, Is.EqualTo(DBNull.Value));
                     That(empty.ToSqlParameter().Value,      Is.EqualTo(DBNull.Value));
                     That(empty.ToSqliteParameter().Value,   Is.EqualTo(DBNull.Value));
                     That(empty.ToMySqlParameter().Value,    Is.EqualTo(DBNull.Value));
                     That(empty.ToDbParameter(command).Value, Is.EqualTo(DBNull.Value));
                 });
    }

    [Test] public void ToCommand_Binds_Only_Referenced_Parameters()
    {
        CommandParameters parameters = CommandParameters.Create<UserRecord>();
        parameters.Add(nameof(UserRecord.UserName), "tester");
        parameters.Add(nameof(UserRecord.Email),    "tester@example.com");

        string     name = parameters.Values.ToArray().Single(static x => x.Column.PropertyName == nameof(UserRecord.UserName)).ParameterName;
        SqlCommand sql  = SqlCommand.Create($"SELECT * FROM users WHERE user_name = @{name}", parameters);

        using SqliteConnection connection = new("Data Source=:memory:");
        using DbCommand        sqlite     = sql.ToCommand(connection);
        using DbCommand        generic    = sql.ToCommand((DbConnection)connection);

        Multiple(() =>
                 {
                     That(sqlite,                          Is.InstanceOf<SqliteCommand>());
                     That(sqlite.Parameters.Count,         Is.EqualTo(1));
                     That(sqlite.Parameters[0].Value,      Is.EqualTo("tester"));
                     That(generic.Parameters.Count,        Is.EqualTo(1));
                     That(generic.Parameters[0].DbType,    Is.EqualTo(DbType.String));
                 });
    }
}
