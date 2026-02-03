namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnMetaDataAttribute( ColumnOptions options, SizeInfo? length, string? foreignKeyName = null ) : Attribute
{
    public static readonly ColumnMetaDataAttribute Default = new(ColumnOptions.None);
    public                 string?                 ForeignKey { get; init; } = foreignKeyName;
    public                 SizeInfo?               Length     { get; init; } = length;
    public                 ColumnOptions           Options    { get; init; } = options;
    public                 ColumnCheckMetaData?    Checks     { get; init; }


    public ColumnMetaDataAttribute( string        foreignKeyName ) : this(ColumnOptions.ForeignKey, null, foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options ) : this(options, null) { }
    public ColumnMetaDataAttribute( ColumnOptions options,   int           length,    string? foreignKeyName = null ) : this(options, SizeInfo.Create(length), foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options,   IntRange      range,     string? foreignKeyName = null ) : this(options, SizeInfo.Create(range), foreignKeyName) { }
    public ColumnMetaDataAttribute( ColumnOptions options,   PrecisionInfo precision, string? foreignKeyName = null ) : this(options, SizeInfo.Create(precision), foreignKeyName) { }
    public ColumnMetaDataAttribute( int           length,    string?       foreignKeyName = null ) : this(ColumnOptions.None, SizeInfo.Create(length), foreignKeyName) { }
    public ColumnMetaDataAttribute( IntRange      range,     string?       foreignKeyName = null ) : this(ColumnOptions.None, SizeInfo.Create(range), foreignKeyName) { }
    public ColumnMetaDataAttribute( PrecisionInfo precision, string?       foreignKeyName = null ) : this(ColumnOptions.None, SizeInfo.Create(precision), foreignKeyName) { }


    public void Deconstruct( out ColumnOptions options, out SizeInfo length, out ColumnCheckMetaData? checks, out string? foreignKeyName )
    {
        options        = Options;
        foreignKeyName = ForeignKey;
        length         = Length ?? SizeInfo.Empty;
        checks         = Checks;
    }
}
