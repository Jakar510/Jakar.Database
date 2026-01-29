// Jakar.Extensions :: Jakar.Database
// 10/18/2025  23:29

using Microsoft.AspNetCore.Components.WebAssembly.Http;



namespace Jakar.Database;


public readonly ref struct SqlTable<TSelf> : IDisposable
    where TSelf : class, ITableRecord<TSelf>
{
    internal readonly SortedDictionary<string, ColumnMetaData> Columns = new(StringComparer.InvariantCultureIgnoreCase);


    public SqlTable() { }
    public static SqlTable<TSelf> Empty => new();

    public static SqlTable<TSelf> Default => Empty.WithColumn(ColumnMetaData.ID)
                                                  .WithColumn(ColumnMetaData.LastModified)
                                                  .WithColumn(ColumnMetaData.DateCreated);

    public void Dispose() => Columns?.Clear();


    // public SqlTableBuilder<TSelf> WithIndexColumn( string indexColumnName, string columnName ) => WithColumn(ColumnMetaData.Indexed(columnName, indexColumnName));


    public SqlTable<TSelf> With_DateCreated()    => WithColumn(ColumnMetaData.DateCreated);
    public SqlTable<TSelf> With_CreatedBy()      => WithColumn(ColumnMetaData.CreatedBy);
    public SqlTable<TSelf> With_AdditionalData() => WithColumn(ColumnMetaData.AdditionalData);


    public SqlTable<TSelf> WithColumn_Json( string propertyName, ColumnOptions options = ColumnOptions.Nullable, ColumnCheckMetaData? checks = null )
    {
        ColumnMetaData column = new(propertyName, PostgresType.Json, options, null, null, SizeInfo.Default, checks);
        return WithColumn(column);
    }
    public SqlTable<TSelf> WithColumn<TValue>( string propertyName, ColumnOptions options, SizeInfo length = default, ColumnCheckMetaData? checks = null, string? indexColumnName = null )
    {
        if ( typeof(TValue) == typeof(RecordID<TSelf>) || typeof(TValue) == typeof(RecordID<TSelf>?) ) { throw new InvalidOperationException($"Use the other overload of {nameof(WithColumn)} instead for primary key columns {RecordID<TSelf>.Description()}."); }

        if ( typeof(TValue).Name.StartsWith("RecordID", StringComparison.InvariantCultureIgnoreCase) ) { throw new InvalidOperationException($"Use the other overload of {nameof(WithColumn)} instead for primary key columns ({nameof(RecordID<>)})."); }

        PostgresType   dbType = typeof(TValue).GetPostgresType(ref options, ref length);
        ColumnMetaData column = new(propertyName, dbType, options, null, indexColumnName, length, checks);
        return WithColumn(column);
    }
    public SqlTable<TSelf> WithColumn<TValue>( string propertyName, bool isNullable, SizeInfo length = default, ColumnCheckMetaData? checks = null, string? indexColumnName = null )
        where TValue : struct, Enum
    {
        ColumnOptions options = ColumnOptions.PrimaryKey;
        if ( isNullable ) { options |= ColumnOptions.Nullable; }

        ColumnMetaData column = new(propertyName, PostgresType.Guid, options, typeof(TValue).Name, indexColumnName, length, checks);
        return WithColumn(column);
    }
    public SqlTable<TSelf> WithColumn<TRecord>( string propertyName, ColumnCheckMetaData? checks = null )
        where TRecord : ITableRecord<TRecord>
    {
        ColumnMetaData column = new(propertyName, PostgresType.Guid, ColumnOptions.ForeignKey, TRecord.TableName, checks: checks);
        return WithColumn(column);
    }
    public SqlTable<TSelf> WithColumn( ColumnMetaData column )
    {
        try
        {
            Columns.Add(column.ColumnName, column);
            return this;
        }
        catch ( Exception e ) { throw new InvalidOperationException(column.ColumnName, e); }
    }


    public TableMetaData Build()
    {
        int check = Columns.Values.Count(static x => x.IsPrimaryKey);
        if ( check != 1 ) { throw new InvalidOperationException($"Must be exactly one primary key defined for {typeof(TSelf).Name}. Instead there are {check} primary keys."); }

        return Columns.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase);
    }
}