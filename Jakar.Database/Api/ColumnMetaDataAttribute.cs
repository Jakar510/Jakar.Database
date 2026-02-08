namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnMetaDataAttribute : Attribute
{
    public static readonly ColumnMetaDataAttribute Default = new(ColumnOptions.None);
    public                 string?                 ForeignKey { get; init; }
    public                 SizeInfo?               Length     { get; init; }
    public                 ColumnOptions           Options    { get; init; }
    public                 ColumnCheckMetaData?    Checks     { get; init; }


    public ColumnMetaDataAttribute( string        foreignKeyName ) : this(ColumnOptions.ForeignKey, null, foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options ) : this(options, null, null) { }
    public ColumnMetaDataAttribute( ColumnOptions options,   int           length,    string? foreignKeyName = null ) : this(options, SizeInfo.Create(length), foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options,   IntRange      range,     string? foreignKeyName = null ) : this(options, SizeInfo.Create(range), foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options,   PrecisionInfo precision, string? foreignKeyName = null ) : this(options, SizeInfo.Create(precision), foreignKeyName) { }
    public ColumnMetaDataAttribute( int           length,    string?       foreignKeyName = null ) : this(ColumnOptions.None, SizeInfo.Create(length), foreignKeyName) { }
    public ColumnMetaDataAttribute( IntRange      range,     string?       foreignKeyName = null ) : this(ColumnOptions.None, SizeInfo.Create(range), foreignKeyName) { }
    public ColumnMetaDataAttribute( PrecisionInfo precision, string?       foreignKeyName = null ) : this(ColumnOptions.None, SizeInfo.Create(precision), foreignKeyName) { }
    private ColumnMetaDataAttribute( ColumnOptions options, SizeInfo? length, string? foreignKeyName )
    {
        ForeignKey = foreignKeyName;
        Length     = length;
        Options    = options;
    }


    public void Deconstruct( out ColumnOptions options, out SizeInfo? length, out ColumnCheckMetaData? checks, out string? foreignKeyName )
    {
        options        = Options;
        foreignKeyName = ForeignKey;
        length         = Length;
        checks         = Checks;
    }
}
