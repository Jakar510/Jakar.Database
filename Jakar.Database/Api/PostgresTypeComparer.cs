// Jakar.Database :: Jakar.Database
// 01/31/2026  16:07

namespace Jakar.Database;


public sealed class PostgresTypeComparer : Comparer<PostgresType>
{
    public static readonly PostgresTypeComparer Instance = new();


    public override int Compare( PostgresType left, PostgresType right )
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


    public static (SizeKind kind, int size) GetSizeInfo( PostgresType type )
    {
        return type switch
               {
                   // ---------------------------
                   // 1-byte
                   // ---------------------------
                   PostgresType.Boolean => ( SizeKind.Fixed, 1 ),
                   PostgresType.Char    => ( SizeKind.Fixed, 1 ),
                   PostgresType.Bit     => ( SizeKind.Fixed, 1 ),
                   PostgresType.Byte    => ( SizeKind.Fixed, 1 ),
                   PostgresType.SByte   => ( SizeKind.Fixed, 1 ),

                   // ---------------------------
                   // 2-byte
                   // ---------------------------
                   PostgresType.Short  => ( SizeKind.Fixed, 2 ),
                   PostgresType.UShort => ( SizeKind.Fixed, 2 ),

                   // ---------------------------
                   // 4-byte
                   // ---------------------------
                   PostgresType.Int    => ( SizeKind.Fixed, 4 ),
                   PostgresType.UInt   => ( SizeKind.Fixed, 4 ),
                   PostgresType.Single => ( SizeKind.Fixed, 4 ),
                   PostgresType.Date   => ( SizeKind.Fixed, 4 ),

                   // ---------------------------
                   // 8-byte
                   // ---------------------------
                   PostgresType.Long     => ( SizeKind.Fixed, 8 ),
                   PostgresType.ULong    => ( SizeKind.Fixed, 8 ),
                   PostgresType.Double   => ( SizeKind.Fixed, 8 ),
                   PostgresType.Time     => ( SizeKind.Fixed, 8 ),
                   PostgresType.DateTime => ( SizeKind.Fixed, 8 ),
                   PostgresType.TimeTz   => ( SizeKind.Fixed, 8 ),
                   PostgresType.Money    => ( SizeKind.Fixed, 8 ),
                   PostgresType.PgLsn    => ( SizeKind.Fixed, 8 ),

                   // ---------------------------
                   // 16-byte
                   // ---------------------------
                   PostgresType.Guid           => ( SizeKind.Fixed, 16 ),
                   PostgresType.Int128         => ( SizeKind.Fixed, 16 ),
                   PostgresType.UInt128        => ( SizeKind.Fixed, 16 ),
                   PostgresType.DateTimeOffset => ( SizeKind.Fixed, 16 ),

                   // ---------------------------
                   // Network / system fixed-ish
                   // ---------------------------
                   PostgresType.Inet     => ( SizeKind.VariableFixed, 16 ),
                   PostgresType.Cidr     => ( SizeKind.VariableFixed, 16 ),
                   PostgresType.MacAddr  => ( SizeKind.Fixed, 6 ),
                   PostgresType.MacAddr8 => ( SizeKind.Fixed, 8 ),
                   PostgresType.Tid      => ( SizeKind.Fixed, 6 ),

                   // ---------------------------
                   // Numeric / arbitrary precision
                   // ---------------------------
                   PostgresType.Numeric           => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.Decimal           => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.NumericRange      => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.NumericMultirange => ( SizeKind.VariableFixed, 64 ),

                   // ---------------------------
                   // Arrays & ranges (bounded but variable)
                   // ---------------------------
                   PostgresType.IntVector    => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.LongVector   => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.FloatVector  => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.DoubleVector => ( SizeKind.VariableFixed, 32 ),

                   PostgresType.IntegerRange        => ( SizeKind.VariableFixed, 16 ),
                   PostgresType.BigIntRange         => ( SizeKind.VariableFixed, 16 ),
                   PostgresType.TimestampRange      => ( SizeKind.VariableFixed, 16 ),
                   PostgresType.DateTimeOffsetRange => ( SizeKind.VariableFixed, 16 ),
                   PostgresType.DateRange           => ( SizeKind.VariableFixed, 16 ),

                   PostgresType.IntMultirange            => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.LongMultirange           => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.TimestampMultirange      => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.DateTimeOffsetMultirange => ( SizeKind.VariableFixed, 32 ),
                   PostgresType.DateMultirange           => ( SizeKind.VariableFixed, 32 ),

                   // ---------------------------
                   // Bounded text-like
                   // ---------------------------
                   PostgresType.String => ( SizeKind.VariableFixed, 64 ),
                   PostgresType.CiText => ( SizeKind.VariableFixed, 64 ),
                   PostgresType.Enum   => ( SizeKind.VariableFixed, 16 ),

                   // ---------------------------
                   // Fully unbounded
                   // ---------------------------
                   PostgresType.Binary    => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.Json      => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.Jsonb     => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.JsonPath  => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.Xml       => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.Hstore    => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.TsVector  => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.TsQuery   => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.RegConfig => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.Geometry  => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.Geography => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.LTree     => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.LQuery    => ( SizeKind.VariableUnbounded, int.MaxValue ),
                   PostgresType.LTxtQuery => ( SizeKind.VariableUnbounded, int.MaxValue ),

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
