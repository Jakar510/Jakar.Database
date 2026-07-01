// Jakar.Database :: Jakar.Database
// 02/02/2026  12:50

namespace Jakar.Database;


public enum PrecisionInfo
{
    /// <summary> (28, 28) </summary>
    Decimal,
    /// <summary> (308, 15) </summary>
    Double,
    /// <summary> (38, 7) </summary>
    Float,
    /// <summary> (128, 0) </summary>
    Int128
}



public readonly record struct PrecisionPair( int Scope, int Precision ) : IComparable<PrecisionPair>
{
    public static readonly PrecisionPair Empty     = new(-1, -1);
    public readonly        int           Precision = Precision;
    public readonly        int           Scope     = Scope;
    public                 bool          IsValid => Scope >= 0 && Precision >= 0 && Scope <= Precision;
    public int CompareTo( PrecisionPair other )
    {
        if ( Empty.Equals(other) ) { return 1; }

        int minComparison = Scope.CompareTo(other.Scope);
        if ( minComparison != 0 ) { return minComparison; }

        return Precision.CompareTo(other.Precision);
    }
}



/// <summary>
///     <para> <see cref="Scope"/>:  Order of magnitude of representable range (the exponent range). </para>
///     <para> <see cref="Precision"/>: Reliable decimal digits of accuracy. </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PrecisionAttribute : DatabaseAttribute
{
    public readonly int  Precision;
    public readonly int  Scope;
    public          bool IsValid => Scope >= 0 && Precision >= 0;


    public PrecisionAttribute( PrecisionPair pair ) : this(pair.Scope, pair.Precision) { }
    public PrecisionAttribute( PrecisionInfo info ) : this(info switch
                                                           {
                                                               PrecisionInfo.Decimal => new PrecisionPair(28,  0),
                                                               PrecisionInfo.Double  => new PrecisionPair(308, 15),
                                                               PrecisionInfo.Float   => new PrecisionPair(38,  7),
                                                               PrecisionInfo.Int128  => new PrecisionPair(128, 0),
                                                               _                     => throw new OutOfRangeException(info)
                                                           }) { }


    /// <summary>
    ///     <para> <see cref="Scope"/>:  Order of magnitude of representable range (the exponent range). </para>
    ///     <para> <see cref="Precision"/>: Reliable decimal digits of accuracy. </para>
    /// </summary>
    /// <param name="scope"> Order of magnitude of representable range (the exponent range). </param>
    /// <param name="precision"> Reliable decimal digits of accuracy. </param>
    public PrecisionAttribute( int scope, int precision )
    {
        if ( precision > DECIMAL_MAX_PRECISION ) { throw new OutOfRangeException(precision); }

        if ( scope > DECIMAL_MAX_SCALE ) { throw new OutOfRangeException(scope); }

        Precision = precision;
        Scope     = scope;
    }
    public override StringBuilder ToStringBuilder()
    {
        StringBuilder sb = new();
        sb.Append($"{Scope}, {Precision}");
        return sb;
    }
}
