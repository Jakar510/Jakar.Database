namespace Jakar.Database;


[DefaultValue(nameof(Default))]
public readonly record struct SizeInfo( LengthInfo Length, PrecisionInfo Precision )
{
    public static readonly SizeInfo      Default   = new(LengthInfo.Default, PrecisionInfo.Default);
    public readonly        bool          IsValid   = Length.IsValid || Precision.IsValid;
    public readonly        LengthInfo    Length    = Length;
    public readonly        PrecisionInfo Precision = Precision;


    public SizeInfo( int                        value ) : this(value, PrecisionInfo.Default) { }
    public SizeInfo( LengthInfo                 value ) : this(value, PrecisionInfo.Default) { }
    public SizeInfo( (int Precision, int Scope) value ) : this(LengthInfo.Default, value) { }
    public SizeInfo( PrecisionInfo              value ) : this(LengthInfo.Default, value) { }


    public static implicit operator SizeInfo( int                        value ) => new(value);
    public static implicit operator SizeInfo( LengthInfo                 value ) => new(value);
    public static implicit operator SizeInfo( (int Precision, int Scope) value ) => new(value);
    public static implicit operator SizeInfo( PrecisionInfo              value ) => new(value);
}