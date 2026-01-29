namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnMetaDataAttribute : Attribute
{
    internal static readonly ColumnMetaDataAttribute Empty = new();
    public                   string?                 ColumnName      { get; set; }
    public                   PostgresType?           DbType          { get; set; }
    public                   string?                 ForeignKey      { get; set; }
    public                   string?                 IndexColumnName { get; set; }
    public                   string?                 KeyValuePair    { get; set; }
    public                   SizeInfo?               Length          { get; set; }
    public                   string?                 Name            { get; set; }
    public                   ColumnOptions?          Options         { get; set; }
    public                   string?                 VariableName    { get; set; }
    public                   ColumnCheckMetaData?    Checks          { get; set; }


    public void Deconstruct( out string? columnName, out ColumnOptions options, out SizeInfo length, out PostgresType? dbType, out string? foreignKeyName, out string? indexColumnName, out string? variableName, out string? keyValuePair, out string? name, out ColumnCheckMetaData checks )
    {
        columnName      = ColumnName;
        options         = Options ?? ColumnOptions.None;
        length          = Length  ?? SizeInfo.Default;
        dbType          = DbType;
        foreignKeyName  = ForeignKey;
        indexColumnName = IndexColumnName;
        variableName    = VariableName;
        keyValuePair    = KeyValuePair;
        name            = Name;
        checks          = Checks ?? ColumnCheckMetaData.Default;
    }
}