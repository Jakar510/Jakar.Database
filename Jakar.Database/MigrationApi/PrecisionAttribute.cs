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


    public PrecisionAttribute( (int Scope, int Precison) pair ) : this(pair.Scope, pair.Precison) { }
    public PrecisionAttribute( PrecisionInfo info ) : this(info switch
                                                           {
                                                               PrecisionInfo.Decimal => ( 28, 0 ),
                                                               PrecisionInfo.Double  => ( 308, 15 ),
                                                               PrecisionInfo.Float   => ( 38, 7 ),
                                                               PrecisionInfo.Int128  => ( 128, 0 ),
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
