namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnMetaDataAttribute : Attribute
{
    internal static readonly ColumnMetaDataAttribute Empty = new();
    public                   string?                 ForeignKey { get; set; }
    public                   SizeInfo?               Length     { get; set; }
    public                   ColumnOptions?          Options    { get; set; }
    public                   ColumnCheckMetaData?    Checks     { get; set; }


    public void Deconstruct( out ColumnOptions options,out string? foreignKeyName, out SizeInfo length,  out ColumnCheckMetaData checks )
    {
        options        = Options ?? ColumnOptions.None;
        length         = Length  ?? SizeInfo.Default;
        foreignKeyName = ForeignKey;
        checks         = Checks ?? ColumnCheckMetaData.Default;
    }
}
