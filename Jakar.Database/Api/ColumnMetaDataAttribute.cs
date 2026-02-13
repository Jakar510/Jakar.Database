namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnInfoAttribute : Attribute
{
    public static readonly ColumnInfoAttribute Empty = new(ColumnOptions.None);


    public ForeignKeyInfo? ForeignKey { get; init; }
    public SizeInfo?       Length     { get; init; }
    public ColumnOptions   Options    { get; init; }
    public ColumnChecks?   Checks     { get; init; }
    public ColumnDefaults? Defaults   { get; init; }


    public ColumnInfoAttribute( string        foreignKeyName, string? onAction = "ON DELETE CASCADE" ) : this(ColumnOptions.ForeignKey, null, new ForeignKeyInfo(foreignKeyName, OnActionInfo.TryCreate(onAction))) { }
    public ColumnInfoAttribute( ColumnOptions options ) : this(options, null) { }
    public ColumnInfoAttribute( ColumnOptions options, int           length ) : this(options, SizeInfo.Create(length)) { }
    public ColumnInfoAttribute( ColumnOptions options, IntRange      range ) : this(options, SizeInfo.Create(range)) { }
    public ColumnInfoAttribute( ColumnOptions options, PrecisionInfo precision ) : this(options, SizeInfo.Create(precision)) { }
    public ColumnInfoAttribute( int           length ) : this(ColumnOptions.None, SizeInfo.Create(length)) { }
    public ColumnInfoAttribute( IntRange      range ) : this(ColumnOptions.None, SizeInfo.Create(range)) { }
    public ColumnInfoAttribute( PrecisionInfo precision ) : this(ColumnOptions.None, SizeInfo.Create(precision)) { }
    private ColumnInfoAttribute( ColumnOptions options, SizeInfo? length, ForeignKeyInfo? foreignKey = null )
    {
        ForeignKey = foreignKey;
        Length     = length;
        Options    = options;
    }
}
