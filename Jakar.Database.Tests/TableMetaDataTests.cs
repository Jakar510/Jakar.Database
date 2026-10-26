// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using System.Data;
using Jakar.Extensions;



namespace Jakar.Database.Tests;


[TestFixture]
public sealed class TableMetaDataTests : Assert
{
    [Test] public void Instance_and_Default_are_the_same_singleton()
    {
        Multiple(() =>
                 {
                     That((object)TableMetaData<RoleRecord>.Default, Is.SameAs(RoleRecord.MetaData));
                     That(RoleRecord.MetaData,                       Is.SameAs(TableMetaData<RoleRecord>.Instance));
                 });
    }

    [Test] public void ColumnCount_is_positive_and_consistent_with_indexes()
    {
        TableMetaData<RoleRecord> meta = RoleRecord.MetaData;

        Multiple(() =>
                 {
                     That(meta.ColumnCount,   Is.GreaterThan(0));
                     That(meta.Indexes.Count, Is.EqualTo(meta.ColumnCount));
                 });
    }

    [Test] public void ContainsKey_and_TryGetValue_resolve_mapped_columns()
    {
        TableMetaData<RoleRecord> meta  = RoleRecord.MetaData;
        bool                      found = meta.TryGetValue(nameof(RoleRecord.NameOfRole), out ColumnMetaData? column);

        Multiple(() =>
                 {
                     That(meta.ContainsKey(nameof(RoleRecord.NameOfRole)), Is.True);
                     That(meta.ContainsKey("NotARealColumn"),              Is.False);
                     That(found,                                           Is.True);
                     That(column,                                          Is.Not.Null);
                     That(column!.ColumnName,                              Is.EqualTo("name_of_role"));
                 });
    }

    [Test] public void Indexer_by_name_and_index_are_consistent()
    {
        TableMetaData<RoleRecord> meta = RoleRecord.MetaData;

        ColumnMetaData byName = meta[nameof(RoleRecord.NameOfRole)];

        Multiple(() =>
                 {
                     That(byName.ColumnName,           Is.EqualTo("name_of_role"));
                     That(meta.PrimaryKeyPropertyName, Is.EqualTo(nameof(RoleRecord.ID)));

                     for ( int i = 0; i < meta.ColumnCount; i++ )
                     {
                         That(meta.Indexes.ContainsKey(i), Is.True);

                         PropertyColumn pc = meta[i];
                         That(pc.Column.Index, Is.EqualTo(i));
                         That(pc.PropertyName, Is.Not.Empty);
                     }
                 });
    }

    [Test] public void Enumerator_visits_every_column()
    {
        TableMetaData<RoleRecord> meta  = RoleRecord.MetaData;
        int                       count = 0;

        foreach ( PropertyColumn column in meta )
        {
            That(column.Column, Is.Not.Null);
            count++;
        }

        That(count, Is.EqualTo(meta.ColumnCount));
    }

    [Test] public void MaxLength_metrics_are_populated()
    {
        TableMetaData<RoleRecord> meta = RoleRecord.MetaData;

        Multiple(() =>
                 {
                     That(meta.MaxLengthColumnName,      Is.GreaterThan(0));
                     That(meta.MaxLength_DataType,        Is.GreaterThan(0));
                     That(meta.MaxLength_KeyValuePair,    Is.GreaterThan(0));
                     That(meta.MaxLength_Variables,       Is.GreaterThan(0));
                     That(meta.MaxLength_IndexColumnName, Is.GreaterThanOrEqualTo(0));
                 });
    }

    [Test] public void DataTable_mirrors_table_name_and_column_count()
    {
        TableMetaData<RoleRecord> meta = RoleRecord.MetaData;

        using DataTable table = meta.DataTable;

        Multiple(() =>
                 {
                     That(table.TableName,     Is.EqualTo("roles"));
                     That(table.Columns.Count, Is.EqualTo(meta.ColumnCount));
                 });
    }

    [Test] public void CreateTableSql_includes_keys_and_constraints()
    {
        string ddl = RoleRecord.MetaData.CreateTableSql(DatabaseType.PostgreSQL);

        Multiple(() =>
                 {
                     That(ddl, Does.Contain("CREATE TABLE").And.Contain("roles"));
                     That(ddl, Does.Contain("PRIMARY KEY"));
                     That(ddl, Does.Contain("REFERENCES users (id)")); // RoleRecord.UserID references users
                 });
    }

    /// <summary> SQL Server DDL uses T-SQL types and syntax, never PostgreSQL's (<c>Integration/CreateTableAgreementTests</c> runs it on a real SQL Server). </summary>
    [Test] public void CreateTableSql_for_SqlServer_uses_TSql_types()
    {
        string ddl = UserRecord.MetaData.CreateTableSql(DatabaseType.MicrosoftSqlServer);

        Multiple(() =>
                 {
                     That(ddl, Does.Contain("[id] uniqueidentifier DEFAULT NEWID() NOT NULL PRIMARY KEY"));
                     That(ddl, Does.Contain("datetimeoffset").And.Contain("nvarchar("));
                     foreach ( string postgres in new[] { "uuid", "bytea", "timestamptz", " bool", " text", "gen_random_uuid", "GENERATED" } ) { That(ddl, Does.Not.Contain(postgres), postgres); }
                 });
    }

    /// <summary> The migrations' DDL is Jakar.SqlBuilder's, so every dialect gets its own syntax. </summary>
    [TestCase(DatabaseType.PostgreSQL,         "CREATE TABLE user_roles (",   "PRIMARY KEY (key_id, value_id));")]
    [TestCase(DatabaseType.MicrosoftSqlServer, "CREATE TABLE [user_roles] (", "PRIMARY KEY ([key_id], [value_id]));")]
    [TestCase(DatabaseType.SQLite,             "CREATE TABLE \"user_roles\" (", "PRIMARY KEY (\"key_id\", \"value_id\"));")]
    [TestCase(DatabaseType.MySQL,              "CREATE TABLE `user_roles` (", "PRIMARY KEY (`key_id`, `value_id`));")]
    [TestCase(DatabaseType.Oracle,             "CREATE TABLE \"user_roles\" (", "PRIMARY KEY (\"key_id\", \"value_id\"))")]
    [TestCase(DatabaseType.Firebird,           "CREATE TABLE \"user_roles\" (", "PRIMARY KEY (\"key_id\", \"value_id\"));")]
    public void CreateTableSql_in_every_dialect( DatabaseType type, string start, string end )
    {
        string ddl = UserRoleRecord.MetaData.CreateTableSql(type);
        Multiple(() =>
                 {
                     That(ddl, Does.StartWith(start));
                     That(ddl, Does.EndWith(end));
                 });
    }

    [TestCase(DatabaseType.PostgreSQL,         "DROP TABLE IF EXISTS roles CASCADE;")]
    [TestCase(DatabaseType.MicrosoftSqlServer, "DROP TABLE IF EXISTS [roles];")]
    [TestCase(DatabaseType.Oracle,             "DROP TABLE IF EXISTS \"roles\" CASCADE CONSTRAINTS")]
    [TestCase(DatabaseType.Firebird,           "DROP TABLE \"roles\";")]
    public void DropTableSql_in_every_dialect( DatabaseType type, string expected ) => That(RoleRecord.MetaData.DropTableSql(type), Is.EqualTo(expected));

    /// <summary> The last_modified trigger in each dialect's form (the engines run it in Jakar.SqlBuilder.Tests' <c>Last_Modified_Trigger_Sets_The_Column</c>). </summary>
    [TestCase(DatabaseType.PostgreSQL,         "CREATE OR REPLACE TRIGGER roles_set_last_modified BEFORE INSERT OR UPDATE ON roles FOR EACH ROW EXECUTE FUNCTION set_last_modified();")]
    [TestCase(DatabaseType.MicrosoftSqlServer, "UPDATE target SET [last_modified] = SYSDATETIMEOFFSET() FROM [roles] target JOIN inserted ON target.[id] = inserted.[id];")]
    [TestCase(DatabaseType.SQLite,             "AFTER UPDATE ON \"roles\" FOR EACH ROW")]
    [TestCase(DatabaseType.MySQL,              "BEFORE UPDATE ON `roles` FOR EACH ROW SET NEW.`last_modified` = UTC_TIMESTAMP(6);")]
    [TestCase(DatabaseType.Oracle,             "CREATE OR REPLACE TRIGGER \"roles_set_last_modified\" BEFORE INSERT OR UPDATE ON \"roles\" FOR EACH ROW BEGIN :NEW.\"last_modified\" := SYSTIMESTAMP; END;")]
    [TestCase(DatabaseType.Firebird,           "CREATE OR ALTER TRIGGER \"roles_set_last_modified\" FOR \"roles\" ACTIVE BEFORE INSERT OR UPDATE POSITION 0 AS BEGIN NEW.\"last_modified\" = CURRENT_TIMESTAMP; END")]
    public void SetLastModified_trigger_per_dialect( DatabaseType type, string expected )
    {
        MigrationRecord migration = RoleRecord.MetaData.SetLastModifiedFunction(1, type);

        Multiple(() =>
                 {
                     That(migration.UpSQL.SQL,     Does.Contain(expected));
                     That(migration.DownSQL.SQL,   Does.StartWith("DROP TRIGGER"));
                     That(migration.CanRollback, Is.True);
                 });
    }

    [Test] public void SetLastModified_trigger_keys_on_the_composite_key()
        => That(UserLoginProviderRecord.MetaData.SetLastModifiedSql(DatabaseType.MicrosoftSqlServer), Does.Contain("JOIN inserted ON target.[id] = inserted.[id]"));

    [Test] public void SetLastModified_trigger_needs_the_column()
        => That(() => UserRoleRecord.MetaData.SetLastModifiedSql(DatabaseType.PostgreSQL), NUnit.Framework.Throws.InvalidOperationException);

    [Test] public void SetLastModified_function_only_exists_on_PostgreSQL()
        => Multiple(() =>
                    {
                        That(MigrationRecord.SetLastModified(0, DatabaseType.PostgreSQL).UpSQL.SQL, Does.Contain("CREATE OR REPLACE FUNCTION set_last_modified()"));
                        That(MigrationRecord.SetLastModified(0, DatabaseType.Oracle).UpSQL.SQL,     Is.EqualTo("SELECT 1 FROM DUAL"));
                        That(MigrationRecord.SetLastModified(0, DatabaseType.MySQL).UpSQL.SQL,      Is.EqualTo("SELECT 1;"));
                    });

    [Test] public void Every_column_type_has_a_name_in_every_dialect()
    {
        foreach ( ColumnMetaData column in UserRecord.MetaData.Properties.Values )
        {
            foreach ( SqlDialectKind dialect in Enum.GetValues<SqlDialectKind>() )
            {
                if ( dialect is SqlDialectKind.NotSet ) { continue; }

                That(( (ISqlColumn)column ).GetTypeName(dialect), Is.Not.Empty, $"{column.PropertyName} on {dialect}");
            }
        }
    }

    private sealed class Amounts
    {
        public decimal                                   Plain   { get; init; }
        [Precision(10, 2)] public decimal                Price   { get; init; }
        [System.ComponentModel.DataAnnotations.MaxLength(40)] public string Code    { get; init; } = string.Empty;
        public string                                    Note    { get; init; } = string.Empty;
        [Unique] public string                           Email   { get; init; } = string.Empty;
    }

    private static string TypeOf( string property, DatabaseType type )
    {
        System.Reflection.PropertyInfo info = typeof(Amounts).GetProperty(property)!;
        return info.GetDataTypes(out _, out _, out _)[type];
    }

    private static string SqliteTypeOf( string property )
    {
        System.Reflection.PropertyInfo info = typeof(Amounts).GetProperty(property)!;
        info.GetDataTypes(out _, out _, out SqlType portable);
        return portable.ColumnName(SqlDialectKind.Sqlite);
    }

    /// <summary> <see cref="decimal"/> is (38, 9) everywhere unless <see cref="PrecisionAttribute"/> says otherwise; strings get a key-sized length when they're unique. </summary>
    [Test] public void Portable_types_per_dialect()
    {
        Multiple(() =>
                 {
                     That(TypeOf(nameof(Amounts.Plain), DatabaseType.PostgreSQL),         Is.EqualTo("numeric(38, 9)"));
                     That(TypeOf(nameof(Amounts.Plain), DatabaseType.MicrosoftSqlServer), Is.EqualTo("numeric(38, 9)"));
                     That(TypeOf(nameof(Amounts.Plain), DatabaseType.MySQL),              Is.EqualTo("DECIMAL(38, 9)"));
                     That(TypeOf(nameof(Amounts.Plain), DatabaseType.Oracle),             Is.EqualTo("NUMBER(38, 9)"));
                     That(TypeOf(nameof(Amounts.Plain), DatabaseType.Firebird),           Is.EqualTo("DECIMAL(38, 9)"));
                     That(SqliteTypeOf(nameof(Amounts.Plain)),                            Is.EqualTo("NUMERIC"));
                     That(TypeOf(nameof(Amounts.Price), DatabaseType.PostgreSQL),         Is.EqualTo("numeric(10, 2)"));
                     That(TypeOf(nameof(Amounts.Price), DatabaseType.MySQL),              Is.EqualTo("DECIMAL(10, 2)"));
                     That(TypeOf(nameof(Amounts.Code),  DatabaseType.Oracle),             Is.EqualTo("VARCHAR2(40)"));
                     That(TypeOf(nameof(Amounts.Note),  DatabaseType.MySQL),              Is.EqualTo("LONGTEXT"));
                     That(TypeOf(nameof(Amounts.Email), DatabaseType.MySQL),              Is.EqualTo("VARCHAR(450)"));
                     That(TypeOf(nameof(Amounts.Email), DatabaseType.Firebird),           Is.EqualTo("VARCHAR(450)"));
                     That(TypeOf(nameof(Amounts.Email), DatabaseType.MicrosoftSqlServer), Is.EqualTo("nvarchar(450)"));
                 });
    }

    [Test] public void IndexName_and_ColumnNames_render_non_empty_text()
    {
        TableMetaData<RoleRecord> meta = RoleRecord.MetaData;

        Multiple(() =>
                 {
                     That(meta.IndexName(nameof(RoleRecord.UserID)), Is.Not.Empty);
                     That(meta.ColumnNames(1).ToString(),            Is.Not.Empty);
                 });
    }

    [Test] public void ITableMetaData_abstraction_exposes_same_data()
    {
        ITableMetaData abstraction = RoleRecord.MetaData;

        Multiple(() =>
                 {
                     That(abstraction.ColumnCount,            Is.EqualTo(RoleRecord.MetaData.ColumnCount));
                     That(abstraction.TableName.Value,        Is.EqualTo("roles"));
                     That(abstraction.PrimaryKeyPropertyName, Is.EqualTo(nameof(RoleRecord.ID)));
                 });
    }

    [Test] public void Sorter_orders_parameters_by_column_index()
    {
        TableMetaData<RoleRecord> meta   = RoleRecord.MetaData;
        ParameterSorter           sorter = meta.Sorter;

        CommandParameter first  = meta[nameof(RoleRecord.NameOfRole)].ToParameter("a", "first");
        CommandParameter second = meta[nameof(RoleRecord.NormalizedName)].ToParameter("b", "second");

        int firstIndex  = meta[nameof(RoleRecord.NameOfRole)].Index;
        int secondIndex = meta[nameof(RoleRecord.NormalizedName)].Index;

        Multiple(() =>
                 {
                     That(Math.Sign(sorter.Compare(first, second)), Is.EqualTo(Math.Sign(firstIndex.CompareTo(secondIndex))));
                     That(sorter.Compare(first, first),             Is.EqualTo(0));
                 });
    }

    [Test] public void PropertyColumn_converts_implicitly_to_its_column()
    {
        PropertyColumn pc       = RoleRecord.MetaData[0];
        ColumnMetaData asColumn = pc;

        Multiple(() =>
                 {
                     That(asColumn,        Is.SameAs(pc.Column));
                     That(pc.PropertyName, Is.Not.Empty);
                 });
    }
}
