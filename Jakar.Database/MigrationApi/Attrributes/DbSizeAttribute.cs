namespace Jakar.Database;


public readonly record struct IntRange( int Min, int Max ) : IComparable<IntRange>
{
    public static readonly IntRange Empty = new(-1, -1);
    public readonly        int      Min   = Min;
    public readonly        int      Max   = Max;
    public                 bool     IsValid => Min >= 0 && Max >= 0 && Min <= Max;
    public int CompareTo( IntRange other )
    {
        if ( Empty.Equals(other) ) { return 1; }

        int minComparison = Min.CompareTo(other.Min);
        if ( minComparison != 0 ) { return minComparison; }

        return Max.CompareTo(other.Max);
    }
}



[AttributeUsage(AttributeTargets.Property)]
public sealed class DbSizeAttribute( int? min, int? max ) : Attribute
{
    public readonly int? Min = min;
    public readonly int? Max = max;

    public IntRange AsIntRange => Min.HasValue && Max.HasValue
                                      ? new IntRange(Min.Value, Max.Value)
                                      : IntRange.Empty;

    public PrecisionPair AsPrecisionInfo => Min.HasValue && Max.HasValue
                                                ? new PrecisionPair(Min.Value, Max.Value)
                                                : PrecisionPair.Empty;
}
