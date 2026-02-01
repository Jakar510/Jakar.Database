namespace Jakar.Database;


[DefaultValue(nameof(Default))]
public readonly record struct LengthInfo( int Value ) : IComparable<LengthInfo>
{
    public static readonly          LengthInfo Default = new(-1);
    public readonly                 bool       IsValid = Value >= 0;
    public readonly                 int        Value   = Value;
    public static implicit operator LengthInfo( int           value ) => new(value);
    public                          int CompareTo( LengthInfo other ) => Value.CompareTo(other.Value);
}



/// <summary>
///     <para> <see cref="Scope"/>:  Order of magnitude of representable range (the exponent range). </para>
///     <para> <see cref="Precision"/>: Reliable decimal digits of accuracy. </para>
/// </summary>
/// <param name="Scope"> Order of magnitude of representable range (the exponent range). </param>
/// <param name="Precision"> Reliable decimal digits of accuracy. </param>
[DefaultValue(nameof(Default))]
public readonly record struct PrecisionInfo( int Scope, int Precision ) : IComparable<PrecisionInfo>
{
    public static readonly          PrecisionInfo Decimal   = new(28, 28);
    public static readonly          PrecisionInfo Default   = new(-1, -1);
    public static readonly          PrecisionInfo Double    = new(308, 15);
    public static readonly          PrecisionInfo Float     = new(38, 7);
    public static readonly          PrecisionInfo Int128    = new(128, 0);
    public readonly                 bool          IsValid   = Scope >= 0 && Precision >= 0;
    public readonly                 int           Precision = Precision;
    public readonly                 int           Scope     = Scope;
    public static implicit operator PrecisionInfo( (int Precision, int Scope) value ) => Create(value.Precision, value.Scope);

    public override string ToString() => $"{Scope}, {Precision}";
    public static PrecisionInfo Create( int scope, int precision )
    {
        if ( precision > DECIMAL_MAX_PRECISION ) { throw new OutOfRangeException(precision); }

        if ( scope > DECIMAL_MAX_SCALE ) { throw new OutOfRangeException(scope); }

        return new PrecisionInfo(scope, precision);
    }
    public int CompareTo( PrecisionInfo other )
    {
        int scopeComparison = Scope.CompareTo(other.Scope);
        if ( scopeComparison != 0 ) { return scopeComparison; }

        return Precision.CompareTo(other.Precision);
    }
}



[DefaultValue(nameof(Default))]
public readonly record struct SizeInfo( LengthInfo Length, PrecisionInfo Precision ) : IComparable<SizeInfo>
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


    public int CompareTo( SizeInfo other )
    {
        int lengthComparison = Length.CompareTo(other.Length);
        if ( lengthComparison != 0 ) { return lengthComparison; }

        return Precision.CompareTo(other.Precision);
    }
}
