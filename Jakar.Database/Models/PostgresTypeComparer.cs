// Jakar.Database :: Jakar.Database
// 01/31/2026  16:07

namespace Jakar.Database;


public class PostgresTypeComparer : Comparer<DbColumnType>
{
    public static PostgresTypeComparer Instance { get; set; } = new();

    protected PostgresTypeComparer() { }

    public override int Compare( DbColumnType left, DbColumnType right )
    {
        if ( left == right ) { return 0; }

        (SizeKind kind, int size) l = GetSizeInfo(left);
        (SizeKind kind, int size) r = GetSizeInfo(right);

        int kindCompare = l.kind.CompareTo(r.kind); // 1) Fixed < VariableFixed < VariableUnbounded
        if ( kindCompare != 0 ) { return kindCompare; }

        int sizeCompare = l.size.CompareTo(r.size); // 2) Size within the same class
        if ( sizeCompare != 0 ) { return sizeCompare; }

        return left.CompareTo(right); // 3) Stable ordering fallback
    }


    public static (SizeKind kind, int size) GetSizeInfo( DbColumnType type )
    {
        return type switch
               {
                   // ---------------------------
                   // 1-byte
                   // ---------------------------
                   DbColumnType.Boolean => ( SizeKind.Fixed, 1 ),
                   DbColumnType.Char    => ( SizeKind.Fixed, 1 ),
                   DbColumnType.Bit     => ( SizeKind.Fixed, 1 ),
                   DbColumnType.Byte    => ( SizeKind.Fixed, 1 ),
                   DbColumnType.SByte   => ( SizeKind.Fixed, 1 ),

                   // ---------------------------
                   // 2-byte
                   // ---------------------------
                   DbColumnType.Short  => ( SizeKind.Fixed, 2 ),
                   DbColumnType.UShort => ( SizeKind.Fixed, 2 ),

                   // ---------------------------
                   // 4-byte
                   // ---------------------------
                   DbColumnType.Int    => ( SizeKind.Fixed, 4 ),
                   DbColumnType.UInt   => ( SizeKind.Fixed, 4 ),
                   DbColumnType.Single => ( SizeKind.Fixed, 4 ),
                   DbColumnType.Date   => ( SizeKind.Fixed, 4 ),

                   // ---------------------------
                   // 8-byte
                   // ---------------------------
                   DbColumnType.Long     => ( SizeKind.Fixed, 8 ),
                   DbColumnType.ULong    => ( SizeKind.Fixed, 8 ),
                   DbColumnType.Double   => ( SizeKind.Fixed, 8 ),
                   DbColumnType.Time     => ( SizeKind.Fixed, 8 ),
                   DbColumnType.DateTime => ( SizeKind.Fixed, 8 ),
                   DbColumnType.TimeTz   => ( SizeKind.Fixed, 8 ),
                   DbColumnType.Money    => ( SizeKind.Fixed, 8 ),
                   DbColumnType.PgLsn    => ( SizeKind.Fixed, 8 ),

                   // ---------------------------
                   // 16-byte
                   // ---------------------------
                   DbColumnType.Guid           => ( SizeKind.Fixed, 16 ),
                   DbColumnType.Int128         => ( SizeKind.Fixed, 16 ),
                   DbColumnType.UInt128        => ( SizeKind.Fixed, 16 ),
                   DbColumnType.DateTimeOffset => ( SizeKind.Fixed, 16 ),

                   // ---------------------------
                   // Network / system fixed-ish
                   // ---------------------------
                   DbColumnType.Inet     => ( SizeKind.VariableFixed, 16 ),
                   DbColumnType.Cidr     => ( SizeKind.VariableFixed, 16 ),
                   DbColumnType.MacAddr  => ( SizeKind.Fixed, 6 ),
                   DbColumnType.MacAddr8 => ( SizeKind.Fixed, 8 ),
                   DbColumnType.Tid      => ( SizeKind.Fixed, 6 ),

                   // ---------------------------
                   // Numeric / arbitrary precision
                   // ---------------------------
                   DbColumnType.Numeric           => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.Decimal           => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.NumericRange      => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.NumericMultirange => ( SizeKind.VariableFixed, 64 ),

                   // ---------------------------
                   // Arrays & ranges (bounded but variable)
                   // ---------------------------
                   DbColumnType.IntVector    => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.LongVector   => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.FloatVector  => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.DoubleVector => ( SizeKind.VariableFixed, 32 ),

                   DbColumnType.IntegerRange        => ( SizeKind.VariableFixed, 16 ),
                   DbColumnType.BigIntRange         => ( SizeKind.VariableFixed, 16 ),
                   DbColumnType.TimestampRange      => ( SizeKind.VariableFixed, 16 ),
                   DbColumnType.DateTimeOffsetRange => ( SizeKind.VariableFixed, 16 ),
                   DbColumnType.DateRange           => ( SizeKind.VariableFixed, 16 ),

                   DbColumnType.IntMultirange            => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.LongMultirange           => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.TimestampMultirange      => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.DateTimeOffsetMultirange => ( SizeKind.VariableFixed, 32 ),
                   DbColumnType.DateMultirange           => ( SizeKind.VariableFixed, 32 ),

                   // ---------------------------
                   // Bounded text-like
                   // ---------------------------
                   DbColumnType.String => ( SizeKind.VariableFixed, 64 ),
                   DbColumnType.CiText => ( SizeKind.VariableFixed, 64 ),
                   DbColumnType.Enum   => ( SizeKind.VariableFixed, 16 ),

                   // ---------------------------
                   // Fully unbounded
                   // ---------------------------
                   DbColumnType.Binary    => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.Json      => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.Jsonb     => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.JsonPath  => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.Xml       => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.Hstore    => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.TsVector  => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.TsQuery   => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.RegConfig => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.Geometry  => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.Geography => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.LTree     => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.LQuery    => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   DbColumnType.LTxtQuery => ( SizeKind.VariableUnbounded, int.MaxValue ),

                   // ---------------------------
                   // Fallback
                   // ---------------------------
                   _ => ( SizeKind.VariableUnbounded, int.MaxValue )
               };
    }



    public enum SizeKind : byte
    {
        Fixed             = 0,
        VariableFixed     = 1,
        VariableUnbounded = 2
    }
}



public sealed class InvertedBoolComparer : Comparer<bool>
{
    public static readonly InvertedBoolComparer Instance = new();

    private InvertedBoolComparer() { }
    public override int Compare( bool x, bool y ) => y.CompareTo(x); // reverse order
}
