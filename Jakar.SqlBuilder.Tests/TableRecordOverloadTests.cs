// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 06/28/2026

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Jakar.Database;
using Jakar.SqlBuilder;
using SqlBuilder = global::Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary>
///     Exercises the strongly-typed <see cref="SqlBuilder"/> overloads against the real Jakar.Database
///     <c>TableRecord</c> types (which satisfy <see cref="ISqlTable{T}"/> through <c>ITableRecord&lt;TSelf&gt;</c>).
///     <para>
///         Expected identifiers are derived from each record's own column metadata via <see cref="Col{T}"/> /
///         <see cref="Tbl{T}"/>, mirroring the writer's fold-then-quote pipeline. This keeps the assertions exact
///         without hard-coding the host's snake_case algorithm, so the tests verify wiring + dialect rendering rather
///         than re-implementing the naming convention.
///     </para>
///     NOTE: <c>UserDetailsRecord</c> is intentionally not covered: its definition is fully commented out in
///     <c>Jakar.Database/Tables/UserDetailsRecord.cs</c>, so no such type exists to test.
/// </summary>
[TestFixture]
public sealed class TableRecordOverloadTests
{
    private const SqlDialectKind PG = SqlDialectKind.PostgreSql;
    private const SqlDialectKind MS = SqlDialectKind.SqlServer;



#region Helpers

    /// <summary> Mirrors <c>SqlWriter.WriteIdentifier</c>: fold for the dialect, then apply quote chars (doubling any close-quote). </summary>
    private static string Ident( string raw, SqlDialectKind dialect )
    {
        string folded = SqlDialects.Fold(dialect, raw);
        ( char open, char close ) = SqlDialects.QuoteChars(dialect);
        if ( open is '\0' ) { return folded; }

        StringBuilder builder = new(folded.Length + 2);
        builder.Append(open);

        foreach ( char c in folded )
        {
            if ( c == close ) { builder.Append(close); }

            builder.Append(c);
        }

        builder.Append(close);
        return builder.ToString();
    }

    /// <summary> The rendered, dialect-quoted column identifier the builder should emit for <paramref name="propertyName"/>. </summary>
    private static string Col<T>( string propertyName, SqlDialectKind dialect )
        where T : ISqlTable<T> => Ident(Resolve<T>(propertyName).GetColumnName(dialect), dialect);

    /// <summary> The rendered, dialect-quoted table identifier for <typeparamref name="T"/>. </summary>
    private static string Tbl<T>( SqlDialectKind dialect )
        where T : ISqlTable<T> => Ident(T.SqlTableName, dialect);

    private static ISqlColumn Resolve<T>( string propertyName )
        where T : ISqlTable<T>
    {
        bool found = T.TrySqlColumn(propertyName, out ISqlColumn? column);
        Assert.That(found,  Is.True,     $"'{propertyName}' should be a mapped column of '{typeof(T).Name}'.");
        Assert.That(column, Is.Not.Null, $"'{propertyName}' resolved to a null column on '{typeof(T).Name}'.");
        return column!;
    }

    /// <summary> Drives the core read overloads (typed projection, full projection, typed WHERE param) for one record across both dialects. </summary>
    private static void AssertReadOverloads<T>( string propertyName )
        where T : ISqlTable<T>
    {
        Assert.Multiple(() =>
                        {
                            // single typed projection
                            Assert.That(SqlBuilder.PostgreSQL.Select<T>(propertyName).From<T>().Build().Sql, Is.EqualTo($"SELECT {Col<T>(propertyName, PG)} FROM {Tbl<T>(PG)};"));
                            Assert.That(SqlBuilder.SqlServer.Select<T>(propertyName).From<T>().Build().Sql,  Is.EqualTo($"SELECT {Col<T>(propertyName, MS)} FROM {Tbl<T>(MS)};"));

                            // every mapped column is selectable and emitted in metadata order
                            Assert.That(SelectAllSql<T>(PG), Is.EqualTo(ExpectedSelectAll<T>(PG)));
                            Assert.That(SelectAllSql<T>(MS), Is.EqualTo(ExpectedSelectAll<T>(MS)));

                            // typed WHERE with a bound parameter
                            SqlResult pg = SqlBuilder.PostgreSQL.Select("*").From<T>().Where<T>(propertyName).EqualTo((object?)"x").Build();
                            Assert.That(pg.Sql,                 Is.EqualTo($"SELECT * FROM {Tbl<T>(PG)} WHERE {Col<T>(propertyName, PG)} = $1;"));
                            Assert.That(pg.Parameters.Count,    Is.EqualTo(1));
                            Assert.That(pg.Parameters[0].Value, Is.EqualTo("x"));

                            SqlResult ms = SqlBuilder.SqlServer.Select("*").From<T>().Where<T>(propertyName).EqualTo((object?)"x").Build();
                            Assert.That(ms.Sql,              Is.EqualTo($"SELECT * FROM {Tbl<T>(MS)} WHERE {Col<T>(propertyName, MS)} = @p0;"));
                            Assert.That(ms.Parameters.Count, Is.EqualTo(1));
                        });
    }

    private static string SelectAllSql<T>( SqlDialectKind dialect )
        where T : ISqlTable<T>
    {
        IReadOnlyList<ISqlColumn> columns       = T.SqlColumns;
        string[]                  propertyNames = new string[columns.Count];
        for ( int i = 0; i < columns.Count; i++ ) { propertyNames[i] = columns[i].PropertyName; }

        return SqlBuilder.For(dialect).Select<T>(propertyNames).From<T>().Build().Sql;
    }

    private static string ExpectedSelectAll<T>( SqlDialectKind dialect )
        where T : ISqlTable<T>
    {
        IReadOnlyList<ISqlColumn> columns = T.SqlColumns;
        StringBuilder             builder = new("SELECT ");

        for ( int i = 0; i < columns.Count; i++ )
        {
            if ( i > 0 ) { builder.Append(", "); }

            builder.Append(Ident(columns[i].GetColumnName(dialect), dialect));
        }

        builder.Append(" FROM ");
        builder.Append(Tbl<T>(dialect));
        builder.Append(';');
        return builder.ToString();
    }

#endregion



#region Table names + canonical column names

    [Test] public void TableNames_resolve_to_their_declared_constants()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(UserRecord.SqlTableName,         Is.EqualTo("users"));
                            Assert.That(RoleRecord.SqlTableName,         Is.EqualTo("roles"));
                            Assert.That(GroupRecord.SqlTableName,        Is.EqualTo("groups"));
                            Assert.That(UserGroupRecord.SqlTableName,    Is.EqualTo("user_groups"));
                            Assert.That(UserRoleRecord.SqlTableName,     Is.EqualTo("user_roles"));
                            Assert.That(FileRecord.SqlTableName,         Is.EqualTo("files"));
                            Assert.That(AddressRecord.SqlTableName,      Is.EqualTo("addresses"));
                            Assert.That(RecoveryCodeRecord.SqlTableName, Is.EqualTo("recovery_codes"));
                        });
    }

    [Test] public void InheritedColumns_map_to_canonical_snake_case_names()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(Resolve<UserRecord>(nameof(UserRecord.ID)).GetColumnName(PG),             Is.EqualTo("id"));
                            Assert.That(Resolve<UserRecord>(nameof(UserRecord.DateCreated)).GetColumnName(PG),    Is.EqualTo("date_created"));
                            Assert.That(Resolve<UserRecord>(nameof(UserRecord.LastModified)).GetColumnName(PG),   Is.EqualTo("last_modified"));
                            Assert.That(Resolve<UserRecord>(nameof(UserRecord.AdditionalData)).GetColumnName(PG), Is.EqualTo("additional_data"));
                            Assert.That(Resolve<RoleRecord>(nameof(RoleRecord.UserID)).GetColumnName(PG),         Is.EqualTo("user_id"));
                            Assert.That(Resolve<AddressRecord>(nameof(AddressRecord.UserID)).GetColumnName(PG),   Is.EqualTo("user_id"));
                        });
    }

#endregion



#region Per-record read overloads (both dialects)

    [Test] public void UserRecord_read_overloads()         => AssertReadOverloads<UserRecord>(nameof(UserRecord.UserName));
    [Test] public void RoleRecord_read_overloads()         => AssertReadOverloads<RoleRecord>(nameof(RoleRecord.NameOfRole));
    [Test] public void GroupRecord_read_overloads()        => AssertReadOverloads<GroupRecord>(nameof(GroupRecord.NameOfGroup));
    [Test] public void FileRecord_read_overloads()         => AssertReadOverloads<FileRecord>(nameof(FileRecord.FileName));
    [Test] public void AddressRecord_read_overloads()      => AssertReadOverloads<AddressRecord>(nameof(AddressRecord.Line1));
    [Test] public void RecoveryCodeRecord_read_overloads() => AssertReadOverloads<RecoveryCodeRecord>(nameof(RecoveryCodeRecord.Code));
    [Test] public void UserGroupRecord_read_overloads()    => AssertReadOverloads<UserGroupRecord>(nameof(UserGroupRecord.KeyID));
    [Test] public void UserRoleRecord_read_overloads()     => AssertReadOverloads<UserRoleRecord>(nameof(UserRoleRecord.ValueID));

#endregion



#region INSERT / UPDATE / DELETE

    [Test] public void UserRecord_Insert_typed_columns_postgres_with_returning()
    {
        SqlResult result = SqlBuilder.PostgreSQL.Insert().Into<UserRecord>(nameof(UserRecord.UserName), nameof(UserRecord.Email)).ValuesParams("ada", "ada@x.io").Returning("id").Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,                 Is.EqualTo($"INSERT INTO {Tbl<UserRecord>(PG)} ({Col<UserRecord>(nameof(UserRecord.UserName), PG)}, {Col<UserRecord>(nameof(UserRecord.Email), PG)}) VALUES ($1, $2) RETURNING id;"));
                            Assert.That(result.Parameters.Count,    Is.EqualTo(2));
                            Assert.That(result.Parameters[0].Value, Is.EqualTo("ada"));
                            Assert.That(result.Parameters[1].Value, Is.EqualTo("ada@x.io"));
                        });
    }

    [Test] public void UserRecord_Insert_typed_columns_sqlserver_with_output()
    {
        SqlResult result = SqlBuilder.SqlServer.Insert().Into<UserRecord>(nameof(UserRecord.UserName)).ValuesParams("ada").Output("id").Build();

        Assert.That(result.Sql, Is.EqualTo($"INSERT INTO {Tbl<UserRecord>(MS)} ({Col<UserRecord>(nameof(UserRecord.UserName), MS)}) VALUES (@p0) OUTPUT INSERTED.[id];"));
    }

    [Test] public void Mapping_Insert_uses_both_key_and_value_columns()
    {
        SqlResult result = SqlBuilder.PostgreSQL.Insert().Into<UserGroupRecord>(nameof(UserGroupRecord.KeyID), nameof(UserGroupRecord.ValueID)).ValuesParams(Guid.NewGuid(), Guid.NewGuid()).Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,              Is.EqualTo($"INSERT INTO {Tbl<UserGroupRecord>(PG)} ({Col<UserGroupRecord>(nameof(UserGroupRecord.KeyID), PG)}, {Col<UserGroupRecord>(nameof(UserGroupRecord.ValueID), PG)}) VALUES ($1, $2);"));
                            Assert.That(result.Parameters.Count, Is.EqualTo(2));
                        });
    }

    [Test] public void UserRecord_Update_typed_set_and_where_postgres()
    {
        Guid      id     = Guid.NewGuid();
        SqlResult result = SqlBuilder.PostgreSQL.Update<UserRecord>().Set<UserRecord>(nameof(UserRecord.Email), SqlValue.Param("ada@x.io")).Where<UserRecord>(nameof(UserRecord.ID)).EqualTo((object?)id).Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,                 Is.EqualTo($"UPDATE {Tbl<UserRecord>(PG)} SET {Col<UserRecord>(nameof(UserRecord.Email), PG)} = $1 WHERE {Col<UserRecord>(nameof(UserRecord.ID), PG)} = $2;"));
                            Assert.That(result.Parameters.Count,    Is.EqualTo(2));
                            Assert.That(result.Parameters[0].Value, Is.EqualTo("ada@x.io"));
                            Assert.That(result.Parameters[1].Value, Is.EqualTo(id));
                        });
    }

    [Test] public void RoleRecord_Update_multiple_set_assignments()
    {
        // Only the first SET assignment has a typed overload; subsequent assignments use the raw-column overload,
        // so we feed it the already-resolved column name to keep the assertion exact.
        string normalizedColumn = Resolve<RoleRecord>(nameof(RoleRecord.NormalizedName)).GetColumnName(MS);

        SqlResult result = SqlBuilder.SqlServer.Update<RoleRecord>().Set<RoleRecord>(nameof(RoleRecord.NameOfRole), SqlValue.Param("admin")).SetParam(normalizedColumn, "ADMIN").Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,                 Is.EqualTo($"UPDATE {Tbl<RoleRecord>(MS)} SET {Col<RoleRecord>(nameof(RoleRecord.NameOfRole), MS)} = @p0, {Ident(normalizedColumn, MS)} = @p1;"));
                            Assert.That(result.Parameters.Count,    Is.EqualTo(2));
                            Assert.That(result.Parameters[0].Value, Is.EqualTo("admin"));
                            Assert.That(result.Parameters[1].Value, Is.EqualTo("ADMIN"));
                        });
    }

    [Test] public void UserRecord_Delete_typed_where_boolean_literal_per_dialect()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(SqlBuilder.PostgreSQL.Delete<UserRecord>().Where<UserRecord>(nameof(UserRecord.IsActive)).EqualTo(false).Build().Sql, Is.EqualTo($"DELETE FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.IsActive), PG)} = FALSE;"));
                            Assert.That(SqlBuilder.SqlServer.Delete<UserRecord>().Where<UserRecord>(nameof(UserRecord.IsActive)).EqualTo(true).Build().Sql,   Is.EqualTo($"DELETE FROM {Tbl<UserRecord>(MS)} WHERE {Col<UserRecord>(nameof(UserRecord.IsActive), MS)} = 1;"));
                        });
    }

#endregion



#region WHERE operators / chaining

    [Test] public void Where_chained_predicates_number_parameters_in_order()
    {
        SqlResult result = SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.UserName)).EqualTo((object?)"ada").And<UserRecord>(nameof(UserRecord.Email)).EqualTo((object?)"ada@x.io").Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,                 Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.UserName), PG)} = $1 AND {Col<UserRecord>(nameof(UserRecord.Email), PG)} = $2;"));
                            Assert.That(result.Parameters.Count,    Is.EqualTo(2));
                            Assert.That(result.Parameters[0].Value, Is.EqualTo("ada"));
                            Assert.That(result.Parameters[1].Value, Is.EqualTo("ada@x.io"));
                        });
    }

    [Test] public void Where_inline_operators_emit_expected_sql()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.BadLogins)).GreaterThan(3).Build().Sql, Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.BadLogins), PG)} > 3;"));
                            Assert.That(SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.BadLogins)).NotEqualTo(0).Build().Sql,  Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.BadLogins), PG)} <> 0;"));
                            Assert.That(SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.BadLogins)).Between(1, 5).Build().Sql,  Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.BadLogins), PG)} BETWEEN 1 AND 5;"));
                            Assert.That(SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.BadLogins)).In(1, 2, 3).Build().Sql,    Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.BadLogins), PG)} IN (1, 2, 3);"));
                            Assert.That(SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.Email)).IsNull().Build().Sql,           Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.Email),     PG)} IS NULL;"));
                            Assert.That(SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.Email)).IsNotNull().Build().Sql,        Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.Email),     PG)} IS NOT NULL;"));
                            Assert.That(SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.UserName)).Like("a%").Build().Sql,      Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.UserName),  PG)} LIKE 'a%';"));
                        });
    }

    [Test] public void Like_inline_string_escapes_embedded_single_quotes() => Assert.That(SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>(nameof(UserRecord.UserName)).Like("O'Brien%").Build().Sql, Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(PG)} WHERE {Col<UserRecord>(nameof(UserRecord.UserName), PG)} LIKE 'O''Brien%';"));

#endregion



#region GROUP BY / ORDER BY / aggregates / DISTINCT

    [Test] public void SelectDistinct_typed_from() => Assert.That(SqlBuilder.PostgreSQL.SelectDistinct("*").From<UserRecord>().Build().Sql, Is.EqualTo($"SELECT DISTINCT * FROM {Tbl<UserRecord>(PG)};"));

    [Test] public void Typed_aggregate_groupby_orderby_sqlserver()
    {
        string sql = SqlBuilder.SqlServer.Select<UserRecord>(nameof(UserRecord.IsActive)).Count<UserRecord>(nameof(UserRecord.ID), "cnt").From<UserRecord>("u").GroupBy<UserRecord>(nameof(UserRecord.IsActive)).OrderByDesc<UserRecord>(nameof(UserRecord.IsActive)).Build().Sql;

        Assert.That(sql, Is.EqualTo($"SELECT {Col<UserRecord>(nameof(UserRecord.IsActive), MS)}, COUNT({Col<UserRecord>(nameof(UserRecord.ID), MS)}) AS [cnt] FROM {Tbl<UserRecord>(MS)} [u] GROUP BY {Col<UserRecord>(nameof(UserRecord.IsActive), MS)} ORDER BY {Col<UserRecord>(nameof(UserRecord.IsActive), MS)} DESC;"));
    }

    [Test] public void Typed_groupby_having_count_postgres()
    {
        string sql = SqlBuilder.PostgreSQL.Select<RoleRecord>(nameof(RoleRecord.NameOfRole)).From<RoleRecord>().GroupBy<RoleRecord>(nameof(RoleRecord.NameOfRole)).Having().Count<RoleRecord>(nameof(RoleRecord.ID)).GreaterThan(1).Build().Sql;

        Assert.That(sql, Is.EqualTo($"SELECT {Col<RoleRecord>(nameof(RoleRecord.NameOfRole), PG)} FROM {Tbl<RoleRecord>(PG)} GROUP BY {Col<RoleRecord>(nameof(RoleRecord.NameOfRole), PG)} HAVING COUNT({Col<RoleRecord>(nameof(RoleRecord.ID), PG)}) > 1;"));
    }

#endregion



#region JOINs across records

    [Test] public void Mapping_join_to_both_endpoints_via_typed_on_equaltocolumn()
    {
        string sql = SqlBuilder.PostgreSQL.Select("*").From<UserRoleRecord>("ur").InnerJoin<UserRecord>("u").On<UserRoleRecord>(nameof(UserRoleRecord.KeyID)).EqualToColumn<UserRecord>(nameof(UserRecord.ID)).InnerJoin<RoleRecord>("r").On<UserRoleRecord>(nameof(UserRoleRecord.ValueID)).EqualToColumn<RoleRecord>(nameof(RoleRecord.ID)).Build().Sql;

        Assert.That(sql, Is.EqualTo($"SELECT * FROM {Tbl<UserRoleRecord>(PG)} ur INNER JOIN {Tbl<UserRecord>(PG)} u ON {Col<UserRoleRecord>(nameof(UserRoleRecord.KeyID), PG)} = {Col<UserRecord>(nameof(UserRecord.ID), PG)} INNER JOIN {Tbl<RoleRecord>(PG)} r ON {Col<UserRoleRecord>(nameof(UserRoleRecord.ValueID), PG)} = {Col<RoleRecord>(nameof(RoleRecord.ID), PG)};"));
    }

    [Test] public void LeftJoin_owned_record_to_user_sqlserver()
    {
        string sql = SqlBuilder.SqlServer.Select("*").From<UserRecord>("u").LeftJoin<AddressRecord>("a").On<AddressRecord>(nameof(AddressRecord.UserID)).EqualToColumn<UserRecord>(nameof(UserRecord.ID)).Build().Sql;

        Assert.That(sql, Is.EqualTo($"SELECT * FROM {Tbl<UserRecord>(MS)} [u] LEFT JOIN {Tbl<AddressRecord>(MS)} [a] ON {Col<AddressRecord>(nameof(AddressRecord.UserID), MS)} = {Col<UserRecord>(nameof(UserRecord.ID), MS)};"));
    }

#endregion



#region Column metadata surfaced through ISqlColumn

    [Test] public void PrimaryKey_and_nullability_flags_are_exposed()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(Resolve<UserRecord>(nameof(UserRecord.ID)).IsPrimaryKey,       Is.True);
                            Assert.That(Resolve<UserRecord>(nameof(UserRecord.UserName)).IsPrimaryKey, Is.False);

                            // nullable reference columns
                            Assert.That(Resolve<UserRecord>(nameof(UserRecord.Email)).IsNullable,         Is.True);
                            Assert.That(Resolve<FileRecord>(nameof(FileRecord.FullPath)).IsNullable,      Is.True);
                            Assert.That(Resolve<AddressRecord>(nameof(AddressRecord.Address)).IsNullable, Is.True);

                            // non-nullable columns
                            Assert.That(Resolve<UserRecord>(nameof(UserRecord.UserName)).IsNullable,    Is.False);
                            Assert.That(Resolve<AddressRecord>(nameof(AddressRecord.Line1)).IsNullable, Is.False);
                        });
    }

    [Test] public void Every_record_exposes_a_single_primary_key_column()
    {
        AssertSinglePrimaryKey<UserRecord>();
        AssertSinglePrimaryKey<RoleRecord>();
        AssertSinglePrimaryKey<GroupRecord>();
        AssertSinglePrimaryKey<FileRecord>();
        AssertSinglePrimaryKey<AddressRecord>();
        AssertSinglePrimaryKey<RecoveryCodeRecord>();
    }

    private static void AssertSinglePrimaryKey<T>()
        where T : ISqlTable<T>
    {
        int primaryKeys = 0;

        foreach ( ISqlColumn column in T.SqlColumns )
        {
            if ( column.IsPrimaryKey ) { primaryKeys++; }
        }

        Assert.That(primaryKeys, Is.EqualTo(1), $"'{typeof(T).Name}' should expose exactly one primary-key column.");
    }

#endregion



#region Edge cases: unknown columns

    [Test] public void Unknown_column_throws_UnknownColumn_across_typed_entry_points()
    {
        Assert.Multiple(() =>
                        {
                            AssertUnknownColumn(() => SqlBuilder.PostgreSQL.Select<UserRecord>("NotAColumn"));
                            AssertUnknownColumn(() => SqlBuilder.PostgreSQL.Insert().Into<UserRecord>("NotAColumn"));
                            AssertUnknownColumn(() => SqlBuilder.PostgreSQL.Update<UserRecord>().Set<UserRecord>("NotAColumn", SqlValue.Of(1)));
                            AssertUnknownColumn(() => SqlBuilder.PostgreSQL.Delete<UserRecord>().Where<UserRecord>("NotAColumn"));
                            AssertUnknownColumn(() => SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().Where<UserRecord>("NotAColumn"));
                            AssertUnknownColumn(() => SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().GroupBy<UserRecord>("NotAColumn"));
                            AssertUnknownColumn(() => SqlBuilder.PostgreSQL.Select("*").From<UserRecord>().OrderBy<UserRecord>("NotAColumn"));
                            AssertUnknownColumn(() => SqlBuilder.PostgreSQL.Select("*").From<UserRecord>("u").InnerJoin<RoleRecord>("r").On<RoleRecord>("NotAColumn"));
                        });
    }

    [Test] public void Unknown_column_message_names_the_record_and_property()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(() => { SqlBuilder.PostgreSQL.Select<UserRecord>("NotAColumn"); })!;

        Assert.Multiple(() =>
                        {
                            Assert.That(ex.Reason,  Is.EqualTo(SqlBuildError.UnknownColumn));
                            Assert.That(ex.Message, Does.Contain("NotAColumn"));
                            Assert.That(ex.Message, Does.Contain(nameof(UserRecord)));
                        });
    }

    private static void AssertUnknownColumn( Action code )
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(code)!;
        Assert.That(ex.Reason, Is.EqualTo(SqlBuildError.UnknownColumn));
    }

#endregion



#region Edge cases: dialect feature support

    [Test] public void Returning_is_rejected_on_sqlserver()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(() => { SqlBuilder.SqlServer.Insert().Into<UserRecord>(nameof(UserRecord.UserName)).ValuesParams("ada").Returning("id"); })!;
        Assert.That(ex.Reason, Is.EqualTo(SqlBuildError.UnsupportedFeature));
    }

    [Test] public void Output_is_rejected_on_postgres()
    {
        SqlBuildException ex = Assert.Throws<SqlBuildException>(() => { SqlBuilder.PostgreSQL.Insert().Into<UserRecord>(nameof(UserRecord.UserName)).ValuesParams("ada").Output("id"); })!;
        Assert.That(ex.Reason, Is.EqualTo(SqlBuildError.UnsupportedFeature));
    }

    [Test] public void Delete_returning_postgres_round_trips()
    {
        SqlResult result = SqlBuilder.PostgreSQL.Delete<RecoveryCodeRecord>().Where<RecoveryCodeRecord>(nameof(RecoveryCodeRecord.UserID)).EqualTo((object?)Guid.NewGuid()).Returning("id").Build();

        Assert.That(result.Sql, Is.EqualTo($"DELETE FROM {Tbl<RecoveryCodeRecord>(PG)} WHERE {Col<RecoveryCodeRecord>(nameof(RecoveryCodeRecord.UserID), PG)} = $1 RETURNING id;"));
    }

#endregion
}
