// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using Jakar.Database;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests.Execution;


/// <summary>
///     Jakar.Database's table migrations for every table, in foreign-key order (each table after the tables it references): <c>CreateTable&lt;T&gt;()</c>, and the <c>last_modified</c>
///     trigger for the tables that have one.
/// </summary>
public static class RecordTables
{
    public static (string Table, SqlResult Create, string? Trigger)[] All( SqlDialectKind dialect ) =>
    [
        Table<FileRecord>(dialect),
        Table<UserRecord>(dialect),
        Table<AddressRecord>(dialect),
        Table<GroupRecord>(dialect),
        Table<RoleRecord>(dialect),
        Table<RecoveryCodeRecord>(dialect),
        Table<UserLoginProviderRecord>(dialect),
        Table<UserAddressRecord>(dialect),
        Table<UserGroupRecord>(dialect),
        Table<UserRoleRecord>(dialect),
        Table<UserRecoveryCodeRecord>(dialect)
    ];

    /// <summary> What the triggers need first: PostgreSQL's shared function (the other dialects' version is a no-op). </summary>
    public static string TriggerFunction( SqlDialectKind dialect ) => MigrationRecord.SetLastModified(0, DatabaseTypeMap.ToDatabaseType(dialect)).UpSQL.SQL;

    private static (string, SqlResult, string?) Table<TSelf>( SqlDialectKind dialect )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        string? trigger = TSelf.MetaData.ContainsKey(nameof(ILastModified.LastModified))
                              ? TSelf.MetaData.SetLastModifiedSql(DatabaseTypeMap.ToDatabaseType(dialect))
                              : null;

        return ( TSelf.SqlTableName, SqlBuilder.For(dialect).CreateTable<TSelf>().Build(), trigger );
    }
}
