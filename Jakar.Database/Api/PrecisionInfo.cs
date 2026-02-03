// Jakar.Database :: Jakar.Database
// 02/02/2026  12:50

namespace Jakar.Database;


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
