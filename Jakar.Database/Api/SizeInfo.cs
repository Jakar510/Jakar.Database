namespace Jakar.Database;


public readonly record struct IntRange( int Min, int Max ) : IComparable<IntRange>
{
    public int CompareTo( IntRange other )
    {
        int minComparison = Min.CompareTo(other.Min);
        if ( minComparison != 0 ) { return minComparison; }

        return Max.CompareTo(other.Max);
    }
}



[DefaultValue(nameof(Empty))]
public readonly struct SizeInfo : IComparable<SizeInfo>, IEquatable<SizeInfo>
{
    public static readonly SizeInfo      Empty = new(-1);
    private readonly       int           __length0;
    private readonly       IntRange      __range1;
    private readonly       PrecisionInfo __precision2;
    private readonly       int           __index = -1;


    public bool IsValid         => __index >= 0;
    public bool IsInt           => __index == 0;
    public bool IsIntRange      => __index == 1;
    public bool IsPrecisionInfo => __index == 2;

    public int AsInt => __index != 0
                            ? throw new InvalidOperationException($"Cannot return as int as result is T{__index}")
                            : __length0;

    public IntRange AsIntRange => __index != 1
                                      ? throw new InvalidOperationException($"Cannot return as IntRange as result is T{__index}")
                                      : __range1;

    public PrecisionInfo AsPrecisionInfo => __index != 2
                                                ? throw new InvalidOperationException($"Cannot return as PrecisionInfo as result is T{__index}")
                                                : __precision2;


    private SizeInfo( int index, int length = 0, IntRange range = default, PrecisionInfo precision = default )
    {
        __index      = index;
        __length0    = length;
        __range1     = range;
        __precision2 = precision;
    }


    public static implicit operator SizeInfo( int           t ) => Create(t);
    public static implicit operator SizeInfo( IntRange      t ) => Create(t);
    public static implicit operator SizeInfo( PrecisionInfo t ) => Create(t);


    public static SizeInfo Create( int           t ) => new SizeInfo(0, t);
    public static SizeInfo Create( IntRange      t ) => new(1, range: t);
    public static SizeInfo Create( PrecisionInfo t ) => new(2, precision: t);


    public void Switch( Action<int> f0, Action<IntRange> f1, Action<PrecisionInfo> f2 )
    {
        switch ( __index )
        {
            case 0:
                f0(__length0);
                break;

            case 1:
                f1(__range1);
                break;

            default:
            {
                if ( __index != 2 ) { f2(__precision2); }

                break;
            }
        }
    }


    public ColumnCheckMetaData Check( in string propertyName ) => Match(in propertyName, ColumnCheckMetaData.Create, ColumnCheckMetaData.Create, ColumnCheckMetaData.Create, ColumnCheckMetaData.Default);


    public TResult? Match<TResult>( Func<int, TResult> f0, Func<IntRange, TResult> f1, Func<PrecisionInfo, TResult> f2, [NotNullIfNotNull(nameof(defaultValue))] TResult? defaultValue = default ) => __index switch
                                                                                                                                                                                                      {
                                                                                                                                                                                                          0 => f0(__length0),
                                                                                                                                                                                                          1 => f1(__range1),
                                                                                                                                                                                                          2 => f2(__precision2),
                                                                                                                                                                                                          _ => defaultValue
                                                                                                                                                                                                      };
    public TResult? Match<TArg, TResult>( in TArg arg, Func<TArg, int, TResult> f0, Func<TArg, IntRange, TResult> f1, Func<TArg, PrecisionInfo, TResult> f2, [NotNullIfNotNull(nameof(defaultValue))] TResult? defaultValue = default ) => __index switch
                                                                                                                                                                                                                                           {
                                                                                                                                                                                                                                               0 => f0(arg, __length0),
                                                                                                                                                                                                                                               1 => f1(arg, __range1),
                                                                                                                                                                                                                                               2 => f2(arg, __precision2),
                                                                                                                                                                                                                                               _ => defaultValue
                                                                                                                                                                                                                                           };


    public override string ToString() => __index switch
                                         {
                                             0 => __length0.ToString(),
                                             1 => __range1.ToString(),
                                             2 => __precision2.ToString(),
                                             _ => throw new InvalidOperationException("Unexpected index, which indicates a problem in the SizeInfo codegen.")
                                         };


    public bool Equals( SizeInfo other ) => __index == other.__index &&
                                            __index switch
                                            {
                                                0 => EqualityComparer<int>.Default.Equals(__length0, other.__length0),
                                                1 => EqualityComparer<IntRange>.Default.Equals(__range1, other.__range1),
                                                2 => EqualityComparer<PrecisionInfo>.Default.Equals(__precision2, other.__precision2),
                                                _ => false
                                            };
    public int CompareTo( SizeInfo other )
    {
        int indexComparison = __index.CompareTo(other.__index);
        if ( indexComparison != 0 ) { return indexComparison; }
        
        int length0Comparison = __length0.CompareTo(other.__length0);
        if ( length0Comparison != 0 ) { return length0Comparison; }

        int rangeComparison = __range1.CompareTo(other.__range1);
        if ( rangeComparison != 0 ) { return rangeComparison; }

        return __precision2.CompareTo(other.__precision2);
    }
    public override int GetHashCode() => HashCode.Combine(__index, __length0, __range1, __precision2);
}
