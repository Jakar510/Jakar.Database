// Jakar.Extensions :: Jakar.Database
// 10/13/2025  22:56

using System.Xml;



namespace Jakar.Database;


public static class DbColumnTypes
{
    public static bool TryGetUnderlyingType( Type type, [NotNullWhen(true)] out Type? result )
    {
        if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) )
        {
            foreach ( Type argument in type.GenericTypeArguments.AsSpan() )
            {
                result = argument;
                return true;
            }
        }

        result = null;
        return false;
    }



    extension( PropertyInfo self )
    {
        public int TryGetLength() => self.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ?? self.GetCustomAttribute<LengthAttribute>()?.MaximumLength ?? self.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? self.GetCustomAttribute<MinLengthAttribute>()?.Length ?? 0;


        public FrozenDictionary<DatabaseType, string> GetDataTypes( out DbColumnType dbType, out bool isNullable )
        {
            dbType = self.PropertyType.GetPostgresType(in self, out isNullable, out int length);

            Dictionary<DatabaseType, string> dataTypes = new()
                                                         {
                                                             [DatabaseType.PostgreSQL]         = self.GetDataType(DatabaseType.PostgreSQL,         in dbType, in length),
                                                             [DatabaseType.MicrosoftSqlServer] = self.GetDataType(DatabaseType.MicrosoftSqlServer, in dbType, in length)
                                                         };

            return dataTypes.ToFrozenDictionary();
        }
        public string GetDataType( in DatabaseType type, ref readonly DbColumnType dbType, ref readonly int length )
        {
            return type switch
                   {
                       DatabaseType.PostgreSQL         => self.GetPostgresDataType(in dbType, in length),
                       DatabaseType.MicrosoftSqlServer => self.GetMicrosoftSqlServerDataType(in dbType, in length),
                       DatabaseType.Oracle             => EMPTY,
                       DatabaseType.MySQL              => EMPTY,
                       DatabaseType.Firebird           => EMPTY,
                       DatabaseType.NotSet             => throw new InvalidOperationException("Database type is not set"),
                       _                               => throw new OutOfRangeException(type)
                   };
        }


        public string GetMicrosoftSqlServerDataType( ref readonly DbColumnType dbType, ref readonly int length )
        {
            return dbType switch
                   {
                       DbColumnType.Binary => self.GetCustomAttribute<MaxLengthAttribute>() is { } attribute
                                                  ? @$"varbit({attribute.Length})"
                                                  : "bytea",
                       DbColumnType.String => self.GetCustomAttribute<MaxLengthAttribute>() is { } attribute
                                                  ? $"varchar({attribute.Length})"
                                                  : "text",
                       DbColumnType.Byte => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                                ? $"bit({attribute.Length})"
                                                : "bit(8)",
                       DbColumnType.SByte => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                                 ? $"bit({attribute.Length})"
                                                 : "bit(8)",
                       DbColumnType.Bit => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                               ? $"bit({attribute.Length})"
                                               : "bit(8)",
                       DbColumnType.VarBit => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                                  ? $"bit varying({attribute.Length})"
                                                  : "bit varying(8)",
                       DbColumnType.Numeric => self.GetCustomAttribute<PrecisionAttribute>() is { } attribute
                                                   ? $"numeric({attribute.Scope}, {attribute.Precision})"
                                                   : "numeric",
                       DbColumnType.Enum                     => $"varchar({length})", // enum
                       DbColumnType.Serial                   => "serial",
                       DbColumnType.BigSerial                => @"bigserial",
                       DbColumnType.SmallSerial              => @"smallserial",
                       DbColumnType.Short                    => "smallint",
                       DbColumnType.UShort                   => "integer",
                       DbColumnType.Int                      => "integer",
                       DbColumnType.UInt                     => "bigint",
                       DbColumnType.Long                     => "bigint",
                       DbColumnType.ULong                    => "numeric(20,0)",
                       DbColumnType.Single                   => "float4",
                       DbColumnType.Double                   => "double precision",
                       DbColumnType.Decimal                  => "numeric(28, 28)",
                       DbColumnType.Boolean                  => "bool",
                       DbColumnType.Date                     => "date",
                       DbColumnType.Time                     => "time",
                       DbColumnType.DateTime                 => "timestamp",
                       DbColumnType.DateTimeOffset           => @"timestamptz",
                       DbColumnType.Money                    => "money",
                       DbColumnType.Guid                     => "uuid",
                       DbColumnType.Json                     => "json",
                       DbColumnType.Xml                      => "xml",
                       DbColumnType.Polygon                  => "polygon",
                       DbColumnType.LineSegment              => @"lseg",
                       DbColumnType.Point                    => "point",
                       DbColumnType.Int128                   => "int16",  // numeric(39,0)
                       DbColumnType.UInt128                  => "uint16", // numeric(39,0)
                       DbColumnType.Box                      => "box",
                       DbColumnType.Circle                   => "circle",
                       DbColumnType.Line                     => "line",
                       DbColumnType.Path                     => "path",
                       DbColumnType.Char                     => "char",
                       DbColumnType.CiText                   => @"citext",
                       DbColumnType.TimeSpan                 => "interval",
                       DbColumnType.TimeTz                   => "time with time zone",
                       DbColumnType.Inet                     => "inet",
                       DbColumnType.Cidr                     => "cidr",
                       DbColumnType.MacAddr                  => @"macaddr",
                       DbColumnType.MacAddr8                 => "macaddr8",
                       DbColumnType.TsVector                 => @"tsvector",
                       DbColumnType.TsQuery                  => @"tsquery",
                       DbColumnType.RegConfig                => @"regconfig",
                       DbColumnType.Jsonb                    => "jsonb",
                       DbColumnType.JsonPath                 => "jsonpath",
                       DbColumnType.Hstore                   => "hstore",
                       DbColumnType.RefCursor                => @"refcursor",
                       DbColumnType.OidVector                => @"oidvector",
                       DbColumnType.Oid                      => "oid",
                       DbColumnType.Xid                      => "xid",
                       DbColumnType.Xid8                     => "xid8",
                       DbColumnType.Cid                      => "cit",
                       DbColumnType.RegType                  => @"regtype",
                       DbColumnType.Tid                      => "tid",
                       DbColumnType.PgLsn                    => @"pglsn",
                       DbColumnType.Geometry                 => "geometry",
                       DbColumnType.Geography                => "geodetic",
                       DbColumnType.LTree                    => @"ltree",
                       DbColumnType.LQuery                   => @"lquery",
                       DbColumnType.LTxtQuery                => @"ltxtquery",
                       DbColumnType.IntVector                => "integer[]",
                       DbColumnType.LongVector               => "bigint[]",
                       DbColumnType.FloatVector              => "float[]",
                       DbColumnType.DoubleVector             => "double[]",
                       DbColumnType.IntegerRange             => "int4range",
                       DbColumnType.BigIntRange              => "int8range",
                       DbColumnType.NumericRange             => @"numrange",
                       DbColumnType.TimestampRange           => @"tsrange",
                       DbColumnType.DateTimeOffsetRange      => @"tstzrange",
                       DbColumnType.DateRange                => @"daterange",
                       DbColumnType.IntMultirange            => "int4multirange",
                       DbColumnType.LongMultirange           => "int8multirange",
                       DbColumnType.NumericMultirange        => @"nummultirange",
                       DbColumnType.TimestampMultirange      => @"tsmultirange",
                       DbColumnType.DateTimeOffsetMultirange => @"tstzmultirange",
                       DbColumnType.DateMultirange           => @"datemultirange",
                       DbColumnType.NotSet                   => EMPTY,
                       _                                     => throw new OutOfRangeException(self)
                   };
        }
        public string GetPostgresDataType( ref readonly DbColumnType dbType, ref readonly int length )
        {
            return dbType switch
                   {
                       DbColumnType.Binary => self.GetCustomAttribute<MaxLengthAttribute>() is { } attribute
                                                  ? @$"varbit({attribute.Length})"
                                                  : "bytea",
                       DbColumnType.String => self.GetCustomAttribute<MaxLengthAttribute>() is { } attribute
                                                  ? $"varchar({attribute.Length})"
                                                  : "text",
                       DbColumnType.Byte => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                                ? $"bit({attribute.Length})"
                                                : "bit(8)",
                       DbColumnType.SByte => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                                 ? $"bit({attribute.Length})"
                                                 : "bit(8)",
                       DbColumnType.Bit => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                               ? $"bit({attribute.Length})"
                                               : "bit(8)",
                       DbColumnType.VarBit => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                                  ? $"bit varying({attribute.Length})"
                                                  : "bit varying(8)",
                       DbColumnType.Numeric => self.GetCustomAttribute<PrecisionAttribute>() is { } attribute
                                                   ? $"numeric({attribute.Scope}, {attribute.Precision})"
                                                   : "numeric",
                       DbColumnType.Enum                     => $"varchar({length})", // enum
                       DbColumnType.Serial                   => "serial",
                       DbColumnType.BigSerial                => @"bigserial",
                       DbColumnType.SmallSerial              => @"smallserial",
                       DbColumnType.Short                    => "smallint",
                       DbColumnType.UShort                   => "integer",
                       DbColumnType.Int                      => "integer",
                       DbColumnType.UInt                     => "bigint",
                       DbColumnType.Long                     => "bigint",
                       DbColumnType.ULong                    => "numeric(20,0)",
                       DbColumnType.Single                   => "float4",
                       DbColumnType.Double                   => "double precision",
                       DbColumnType.Decimal                  => "numeric(28, 28)",
                       DbColumnType.Boolean                  => "bool",
                       DbColumnType.Date                     => "date",
                       DbColumnType.Time                     => "time",
                       DbColumnType.DateTime                 => "timestamp",
                       DbColumnType.DateTimeOffset           => @"timestamptz",
                       DbColumnType.Money                    => "money",
                       DbColumnType.Guid                     => "uuid",
                       DbColumnType.Json                     => "json",
                       DbColumnType.Xml                      => "xml",
                       DbColumnType.Polygon                  => "polygon",
                       DbColumnType.LineSegment              => @"lseg",
                       DbColumnType.Point                    => "point",
                       DbColumnType.Int128                   => "int16",  // numeric(39,0)
                       DbColumnType.UInt128                  => "uint16", // numeric(39,0)
                       DbColumnType.Box                      => "box",
                       DbColumnType.Circle                   => "circle",
                       DbColumnType.Line                     => "line",
                       DbColumnType.Path                     => "path",
                       DbColumnType.Char                     => "char",
                       DbColumnType.CiText                   => @"citext",
                       DbColumnType.TimeSpan                 => "interval",
                       DbColumnType.TimeTz                   => "time with time zone",
                       DbColumnType.Inet                     => "inet",
                       DbColumnType.Cidr                     => "cidr",
                       DbColumnType.MacAddr                  => @"macaddr",
                       DbColumnType.MacAddr8                 => "macaddr8",
                       DbColumnType.TsVector                 => @"tsvector",
                       DbColumnType.TsQuery                  => @"tsquery",
                       DbColumnType.RegConfig                => @"regconfig",
                       DbColumnType.Jsonb                    => "jsonb",
                       DbColumnType.JsonPath                 => "jsonpath",
                       DbColumnType.Hstore                   => "hstore",
                       DbColumnType.RefCursor                => @"refcursor",
                       DbColumnType.OidVector                => @"oidvector",
                       DbColumnType.Oid                      => "oid",
                       DbColumnType.Xid                      => "xid",
                       DbColumnType.Xid8                     => "xid8",
                       DbColumnType.Cid                      => "cit",
                       DbColumnType.RegType                  => @"regtype",
                       DbColumnType.Tid                      => "tid",
                       DbColumnType.PgLsn                    => @"pglsn",
                       DbColumnType.Geometry                 => "geometry",
                       DbColumnType.Geography                => "geodetic",
                       DbColumnType.LTree                    => @"ltree",
                       DbColumnType.LQuery                   => @"lquery",
                       DbColumnType.LTxtQuery                => @"ltxtquery",
                       DbColumnType.IntVector                => "integer[]",
                       DbColumnType.LongVector               => "bigint[]",
                       DbColumnType.FloatVector              => "float[]",
                       DbColumnType.DoubleVector             => "double[]",
                       DbColumnType.IntegerRange             => "int4range",
                       DbColumnType.BigIntRange              => "int8range",
                       DbColumnType.NumericRange             => @"numrange",
                       DbColumnType.TimestampRange           => @"tsrange",
                       DbColumnType.DateTimeOffsetRange      => @"tstzrange",
                       DbColumnType.DateRange                => @"daterange",
                       DbColumnType.IntMultirange            => "int4multirange",
                       DbColumnType.LongMultirange           => "int8multirange",
                       DbColumnType.NumericMultirange        => @"nummultirange",
                       DbColumnType.TimestampMultirange      => @"tsmultirange",
                       DbColumnType.DateTimeOffsetMultirange => @"tstzmultirange",
                       DbColumnType.DateMultirange           => @"datemultirange",
                       DbColumnType.NotSet                   => EMPTY,
                       _                                     => throw new OutOfRangeException(self)
                   };
        }
    }



    extension( Type self )
    {
        private bool IsTypeOrUnderlyingType( in Type targetType )
        {
            if ( self == targetType ) { return true; }

            switch ( self.IsGenericType )
            {
                case true when self.GetGenericTypeDefinition() == targetType:
                    return true;

                case true when self.GetGenericTypeDefinition() == typeof(Nullable<>):
                {
                    foreach ( Type argument in self.GenericTypeArguments.AsSpan() ) { return argument.IsTypeOrUnderlyingType(in targetType); }

                    break;
                }
            }

            return false;
        }


        public DbColumnType GetPostgresType( in PropertyInfo property, out bool isNullable, out int length )
        {
            length     = 0;
            isNullable = self.IsNullableType() || self.IsBuiltInNullableType() || !property.IsNonNullableReferenceType();

            if ( ( TryGetUnderlyingType(self, out Type? underlyingType) && underlyingType.IsEnum ) || self.IsEnum )
            {
                length = Enum.GetNames(underlyingType ?? property.PropertyType).AsValueEnumerable().Max(static x => x.Length);

                isNullable = underlyingType is not null;
                return DbColumnType.Enum;
            }

            if ( self.IsTypeOrUnderlyingType(typeof(RecordID<>)) ) { return DbColumnType.Guid; }

            if ( self.IsTypeOrUnderlyingType(typeof(RecordID<,>)) ) { return DbColumnType.NotSet; }

            if ( self.IsTypeOrUnderlyingType(typeof(AutoRecordID<>)) ) { return DbColumnType.BigSerial; }

            if ( self == typeof(byte[]) || self == typeof(Memory<byte>) || self == typeof(ReadOnlyMemory<byte>) || self == typeof(ImmutableArray<byte>) ) { return DbColumnType.Binary; }

            if ( self == typeof(Memory<byte>?) || self == typeof(ReadOnlyMemory<byte>?) || self == typeof(ImmutableArray<byte>?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Binary;
            }

            if ( typeof(JToken).IsAssignableFrom(self) || typeof(JsonNode).IsAssignableFrom(self) || self == typeof(JsonDocument) || self == typeof(JsonElement) ) { return DbColumnType.Json; }

            if ( typeof(XmlNode).IsAssignableFrom(self) ) { return DbColumnType.Xml; }

            if ( self == typeof(Guid) ) { return DbColumnType.Guid; }

            if ( self == typeof(Guid?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Guid;
            }

            if ( self == typeof(string) || self == typeof(UserRights) ) { return DbColumnType.String; }

            if ( self == typeof(Int128) ) { return DbColumnType.Int128; }

            if ( self == typeof(Int128?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Int128;
            }

            if ( self == typeof(UInt128) ) { return DbColumnType.UInt128; }

            if ( self == typeof(UInt128?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.UInt128;
            }

            if ( self == typeof(byte) ) { return DbColumnType.Bit; }

            if ( self == typeof(byte?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Bit;
            }

            if ( self == typeof(short) || self == typeof(ushort) ) { return DbColumnType.Short; }

            if ( self == typeof(short?) || self == typeof(ushort?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Short;
            }

            if ( self == typeof(int) || self == typeof(uint) ) { return DbColumnType.Int; }

            if ( self == typeof(int?) || self == typeof(uint?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Int;
            }

            if ( self == typeof(long) || self == typeof(ulong) ) { return DbColumnType.Long; }

            if ( self == typeof(long?) || self == typeof(ulong?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Long;
            }

            if ( self == typeof(float) ) { return DbColumnType.Single; }

            if ( self == typeof(float?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Single;
            }

            if ( self == typeof(double) ) { return DbColumnType.Double; }

            if ( self == typeof(double?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Double;
            }

            if ( self == typeof(decimal) ) { return DbColumnType.Decimal; }

            if ( self == typeof(decimal?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Decimal;
            }

            if ( self == typeof(bool) ) { return DbColumnType.Boolean; }

            if ( self == typeof(bool?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Boolean;
            }

            if ( self == typeof(DateOnly) ) { return DbColumnType.Date; }

            if ( self == typeof(DateOnly?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Date;
            }

            if ( self == typeof(TimeOnly) || self == typeof(TimeSpan) ) { return DbColumnType.Time; }

            if ( self == typeof(TimeOnly?) || self == typeof(TimeSpan?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.Time;
            }

            if ( self == typeof(DateTime) ) { return DbColumnType.DateTime; }

            if ( self == typeof(DateTime?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.DateTime;
            }

            if ( self == typeof(DateTimeOffset) ) { return DbColumnType.DateTimeOffset; }

            if ( self == typeof(DateTimeOffset?) )
            {
                Debug.Assert(isNullable);
                return DbColumnType.DateTimeOffset;
            }

            throw new ArgumentException($"Unsupported type: {self.Name}");
        }
    }



    extension( DbColumnType self )
    {
        [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")] public NpgsqlDbType ToNpgsqlDbType()
        {
            return self switch
                   {
                       DbColumnType.Enum                     => NpgsqlDbType.Bigint,
                       DbColumnType.Serial                   => NpgsqlDbType.Integer,
                       DbColumnType.BigSerial                => NpgsqlDbType.Bigint,
                       DbColumnType.SmallSerial              => NpgsqlDbType.Smallint,
                       DbColumnType.Boolean                  => NpgsqlDbType.Boolean,
                       DbColumnType.Short                    => NpgsqlDbType.Smallint,
                       DbColumnType.UShort                   => NpgsqlDbType.Smallint,
                       DbColumnType.Int                      => NpgsqlDbType.Integer,
                       DbColumnType.UInt                     => NpgsqlDbType.Integer,
                       DbColumnType.Long                     => NpgsqlDbType.Bigint,
                       DbColumnType.ULong                    => NpgsqlDbType.Bigint,
                       DbColumnType.Int128                   => NpgsqlDbType.Numeric,
                       DbColumnType.UInt128                  => NpgsqlDbType.Numeric,
                       DbColumnType.Single                   => NpgsqlDbType.Double,
                       DbColumnType.Double                   => NpgsqlDbType.Double,
                       DbColumnType.Numeric                  => NpgsqlDbType.Numeric,
                       DbColumnType.Decimal                  => NpgsqlDbType.Numeric,
                       DbColumnType.Money                    => NpgsqlDbType.Money,
                       DbColumnType.Box                      => NpgsqlDbType.Box,
                       DbColumnType.Circle                   => NpgsqlDbType.Circle,
                       DbColumnType.Line                     => NpgsqlDbType.Line,
                       DbColumnType.LineSegment              => NpgsqlDbType.LSeg,
                       DbColumnType.Path                     => NpgsqlDbType.Path,
                       DbColumnType.Point                    => NpgsqlDbType.Point,
                       DbColumnType.Polygon                  => NpgsqlDbType.Polygon,
                       DbColumnType.Char                     => NpgsqlDbType.Char,
                       DbColumnType.String                   => NpgsqlDbType.Text,
                       DbColumnType.CiText                   => NpgsqlDbType.Citext,
                       DbColumnType.Binary                   => NpgsqlDbType.Bytea,
                       DbColumnType.Date                     => NpgsqlDbType.Date,
                       DbColumnType.Time                     => NpgsqlDbType.Time,
                       DbColumnType.DateTime                 => NpgsqlDbType.DateRange,
                       DbColumnType.DateTimeOffset           => NpgsqlDbType.TimestampTz,
                       DbColumnType.TimeSpan                 => NpgsqlDbType.Integer,
                       DbColumnType.TimeTz                   => NpgsqlDbType.TimeTz,
                       DbColumnType.Inet                     => NpgsqlDbType.Inet,
                       DbColumnType.Cidr                     => NpgsqlDbType.Cidr,
                       DbColumnType.MacAddr                  => NpgsqlDbType.MacAddr,
                       DbColumnType.MacAddr8                 => NpgsqlDbType.MacAddr8,
                       DbColumnType.Bit                      => NpgsqlDbType.Bit,
                       DbColumnType.Byte                     => NpgsqlDbType.Bit,
                       DbColumnType.SByte                    => NpgsqlDbType.Bit,
                       DbColumnType.VarBit                   => NpgsqlDbType.Bit,
                       DbColumnType.TsVector                 => NpgsqlDbType.TsVector,
                       DbColumnType.TsQuery                  => NpgsqlDbType.TsQuery,
                       DbColumnType.RegConfig                => NpgsqlDbType.Regconfig,
                       DbColumnType.Guid                     => NpgsqlDbType.Uuid,
                       DbColumnType.Xml                      => NpgsqlDbType.Xml,
                       DbColumnType.Json                     => NpgsqlDbType.Json,
                       DbColumnType.Jsonb                    => NpgsqlDbType.Jsonb,
                       DbColumnType.JsonPath                 => NpgsqlDbType.JsonPath,
                       DbColumnType.Hstore                   => NpgsqlDbType.Hstore,
                       DbColumnType.RefCursor                => NpgsqlDbType.Refcursor,
                       DbColumnType.OidVector                => NpgsqlDbType.Oidvector,
                       DbColumnType.Oid                      => NpgsqlDbType.Oid,
                       DbColumnType.Xid                      => NpgsqlDbType.Xid,
                       DbColumnType.Xid8                     => NpgsqlDbType.Xid8,
                       DbColumnType.Cid                      => NpgsqlDbType.Cid,
                       DbColumnType.RegType                  => NpgsqlDbType.Regtype,
                       DbColumnType.Tid                      => NpgsqlDbType.Tid,
                       DbColumnType.PgLsn                    => NpgsqlDbType.PgLsn,
                       DbColumnType.Geometry                 => NpgsqlDbType.Geometry,
                       DbColumnType.Geography                => NpgsqlDbType.Geography,
                       DbColumnType.LTree                    => NpgsqlDbType.LTree,
                       DbColumnType.LQuery                   => NpgsqlDbType.LQuery,
                       DbColumnType.LTxtQuery                => NpgsqlDbType.LTxtQuery,
                       DbColumnType.IntVector                => NpgsqlDbType.Int2Vector,
                       DbColumnType.LongVector               => NpgsqlDbType.BigIntRange,
                       DbColumnType.FloatVector              => NpgsqlDbType.Double | NpgsqlDbType.Array,
                       DbColumnType.DoubleVector             => NpgsqlDbType.Double | NpgsqlDbType.Array,
                       DbColumnType.IntegerRange             => NpgsqlDbType.IntegerRange,
                       DbColumnType.BigIntRange              => NpgsqlDbType.BigIntRange,
                       DbColumnType.NumericRange             => NpgsqlDbType.NumericRange,
                       DbColumnType.TimestampRange           => NpgsqlDbType.TimestampRange,
                       DbColumnType.DateTimeOffsetRange      => NpgsqlDbType.TimestampTzRange,
                       DbColumnType.DateRange                => NpgsqlDbType.DateRange,
                       DbColumnType.IntMultirange            => NpgsqlDbType.IntegerMultirange,
                       DbColumnType.LongMultirange           => NpgsqlDbType.BigIntMultirange,
                       DbColumnType.NumericMultirange        => NpgsqlDbType.NumericMultirange,
                       DbColumnType.TimestampMultirange      => NpgsqlDbType.TimestampMultirange,
                       DbColumnType.DateTimeOffsetMultirange => NpgsqlDbType.TimestampTzMultirange,
                       DbColumnType.DateMultirange           => NpgsqlDbType.DateMultirange,
                       DbColumnType.NotSet                   => 0,
                       _                                     => throw new OutOfRangeException(self)
                   };
        }


        [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")] public SqlDbType ToSqlDbType()
        {
            return self switch
                   {
                       DbColumnType.Enum           => SqlDbType.BigInt,
                       DbColumnType.Serial         => SqlDbType.Int,
                       DbColumnType.BigSerial      => SqlDbType.BigInt,
                       DbColumnType.SmallSerial    => SqlDbType.SmallInt,
                       DbColumnType.Boolean        => SqlDbType.Bit,
                       DbColumnType.Short          => SqlDbType.SmallInt,
                       DbColumnType.UShort         => SqlDbType.SmallInt,
                       DbColumnType.Int            => SqlDbType.Int,
                       DbColumnType.UInt           => SqlDbType.Int,
                       DbColumnType.Long           => SqlDbType.BigInt,
                       DbColumnType.ULong          => SqlDbType.BigInt,
                       DbColumnType.Single         => SqlDbType.Float,
                       DbColumnType.Double         => SqlDbType.Float,
                       DbColumnType.Decimal        => SqlDbType.Decimal,
                       DbColumnType.Money          => SqlDbType.Money,
                       DbColumnType.Char           => SqlDbType.Char,
                       DbColumnType.String         => SqlDbType.Text,
                       DbColumnType.Binary         => SqlDbType.Binary,
                       DbColumnType.Date           => SqlDbType.Date,
                       DbColumnType.Time           => SqlDbType.Time,
                       DbColumnType.DateTime       => SqlDbType.DateTime,
                       DbColumnType.DateTimeOffset => SqlDbType.DateTimeOffset,
                       DbColumnType.TimeSpan       => SqlDbType.Time,
                       DbColumnType.TimeTz         => SqlDbType.Timestamp,
                       DbColumnType.Bit            => SqlDbType.Bit,
                       DbColumnType.Byte           => SqlDbType.Bit,
                       DbColumnType.SByte          => SqlDbType.Bit,
                       DbColumnType.VarBit         => SqlDbType.Bit,
                       DbColumnType.Guid           => SqlDbType.UniqueIdentifier,
                       DbColumnType.Xml            => SqlDbType.Xml,
                       DbColumnType.Json           => SqlDbType.Json,
                       DbColumnType.NotSet         => 0,
                       _                           => throw new OutOfRangeException(self)
                   };
        }
    }
}
