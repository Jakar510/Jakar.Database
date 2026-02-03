namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnMetaDataAttribute( ColumnOptions options, SizeInfo length, string? foreignKeyName = null ) : Attribute
{
    public static readonly ColumnMetaDataAttribute Default = new(ColumnOptions.None);
    public                 string?                 ForeignKey { get; init; } = foreignKeyName;
    public                 SizeInfo                Length     { get; init; } = length;
    public                 ColumnOptions           Options    { get; init; } = options;
    public                 ColumnCheckMetaData?    Checks     { get; init; }


    public ColumnMetaDataAttribute( ColumnOptions options, string?       foreignKeyName                    = null ) : this(options, SizeInfo.Empty, foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options, int           length,    string? foreignKeyName = null ) : this(options, SizeInfo.Create(length), foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options, IntRange      range,     string? foreignKeyName = null ) : this(options, SizeInfo.Create(range), foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options, PrecisionInfo precision, string? foreignKeyName = null ) : this(options, SizeInfo.Create(precision), foreignKeyName) { }


    public void Deconstruct( out ColumnOptions options, out SizeInfo length, out ColumnCheckMetaData checks, out string? foreignKeyName )
    {
        options        = Options;
        length         = Length;
        foreignKeyName = ForeignKey;
        checks         = Checks ?? ColumnCheckMetaData.Default;
    }
}
