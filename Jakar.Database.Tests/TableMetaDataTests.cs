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
                     That(meta.MaxLength_ColumnName,      Is.GreaterThan(0));
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
                     That(ddl, Does.Contain("FOREIGN KEY")); // RoleRecord.UserID references users
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

        SqlParameter first  = meta[nameof(RoleRecord.NameOfRole)].ToParameter("a", "first");
        SqlParameter second = meta[nameof(RoleRecord.NormalizedName)].ToParameter("b", "second");

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
