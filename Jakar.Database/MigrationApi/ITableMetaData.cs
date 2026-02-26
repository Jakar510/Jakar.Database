// Jakar.Database :: Jakar.Database
// 02/20/2026  13:31

using ZLinq.Linq;



namespace Jakar.Database;


[SuppressMessage("ReSharper", "InconsistentNaming")]
public interface ITableMetaData
{
    public const BindingFlags ATTRIBUTES = BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty;

    public abstract static ITableMetaData                                                                                   Default        { get; }
    public                 ValueEnumerable<Select<TableMetaDataEnumerator, PropertyColumn, ColumnMetaData>, ColumnMetaData> Columns        { [Pure] get; }
    public                 int                                                                                              Count          { get; }
    public                 PooledArray<ColumnMetaData>                                                                      ForeignKeys    { [Pure] [MustUseReturnValue] [MustDisposeResource] get; }
    public                 PooledArray<Func<long, MigrationRecord>>                                                         IndexedColumns { [Pure] [MustUseReturnValue] [MustDisposeResource] get; }
    public                 FrozenDictionary<int, string>                                                                    Indexes        { get; }
    public ref readonly ColumnMetaData this[ string propertyName ] { get; }
    public PropertyColumn this[ int                 index ] { get; }
    public int                                      MaxLength_ColumnName        { get; }
    public int                                      MaxLength_DataType          { get; }
    public int                                      MaxLength_IndexColumnName   { get; }
    public int                                      MaxLength_KeyValuePair      { get; }
    public int                                      MaxLength_Variables         { get; }
    public FrozenDictionary<string, ColumnMetaData> Properties                  { get; }
    public PooledArray<ColumnMetaData>              SortedColumns               { [Pure] [MustUseReturnValue] [MustDisposeResource] get; }
    public string                                   TableName                   { [Pure] get; }
    public string                                   SetLastModifiedFunctionName { get; }


    public StringBuilder ColumnNames( int    indentLevel );
    public StringBuilder VariableNames( int  indentLevel );
    public StringBuilder KeyValuePairs( int  indentLevel );
    public string        IndexName( string   propertyName );
    public bool          ContainsKey( string propertyName );
    public bool          TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value );


    public MigrationRecord SetLastModifiedFunction( long migrationID );
    public MigrationRecord CreateTable( long             migrationID );


    public TableMetaDataEnumerator GetEnumerator();
}
