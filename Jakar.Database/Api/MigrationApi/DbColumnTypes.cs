// Jakar.Extensions :: Jakar.Database
// 10/13/2025  22:56

using System.Xml;
using Microsoft.Data.Sqlite;
using MySqlConnector;



namespace Jakar.Database;


public static class DbColumnTypes
{
    /// <summary> The length of an unconstrained string that is a key, unique or indexed: 450 characters stay within every engine's index-key limit (SQL Server's 900 bytes as UTF-16). </summary>
    public const int KEY_STRING_LENGTH = 450;

    /// <summary> <see cref="decimal"/> columns without a <see cref="PrecisionAttribute"/>: 29 integer digits (every <see cref="decimal"/> integer part) and 9 decimal places. </summary>
    public const int DECIMAL_PRECISION = 38;

    public const int DECIMAL_SCALE = 9;

    private const string DECIMAL_TYPE = "numeric(38, 9)";

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


        /// <param name="portable"> The column's portable type, which names it in the dialects without an explicit map (SQLite, MySQL, Oracle, Firebird). </param>
        public FrozenDictionary<DatabaseType, string> GetDataTypes( out DbColumnType dbType, out bool isNullable, out SqlType portable )
        {
            dbType   = self.PropertyType.GetPostgresType(in self, out isNullable, out int length);
            portable = self.GetPortableType(in dbType, in length);

            Dictionary<DatabaseType, string> dataTypes = new()
                                                         {
                                                             [DatabaseType.PostgreSQL]         = self.GetDataType(DatabaseType.PostgreSQL,         in dbType, in length),
                                                             [DatabaseType.MicrosoftSqlServer] = self.GetDataType(DatabaseType.MicrosoftSqlServer, in dbType, in length),
                                                             [DatabaseType.MySQL]              = self.GetDataType(DatabaseType.MySQL,              in dbType, in length),
                                                             [DatabaseType.Oracle]             = self.GetDataType(DatabaseType.Oracle,             in dbType, in length),
                                                             [DatabaseType.Firebird]           = self.GetDataType(DatabaseType.Firebird,           in dbType, in length),
                                                             [DatabaseType.SQLite]             = self.GetDataType(DatabaseType.SQLite,             in dbType, in length)
                                                         };

            return dataTypes.ToFrozenDictionary();
        }
        public string GetDataType( in DatabaseType type, ref readonly DbColumnType dbType, ref readonly int length )
        {
            return type switch
                   {
                       DatabaseType.PostgreSQL         => self.GetPostgresDataType(in dbType, in length),
                       DatabaseType.MicrosoftSqlServer => self.GetMicrosoftSqlServerDataType(in dbType, in length),
                       DatabaseType.Oracle             => self.GetPortableType(in dbType, in length).ColumnName(SqlDialectKind.Oracle),
                       DatabaseType.MySQL              => self.GetPortableType(in dbType, in length).ColumnName(SqlDialectKind.MySql),
                       DatabaseType.Firebird           => self.GetPortableType(in dbType, in length).ColumnName(SqlDialectKind.Firebird),
                       DatabaseType.SQLite             => self.GetPortableType(in dbType, in length).ColumnName(SqlDialectKind.Sqlite),
                       DatabaseType.NotSet             => throw new InvalidOperationException("Database type is not set"),
                       _                               => throw new OutOfRangeException(type)
                   };
        }


        /// <summary>
        ///     The column's portable <see cref="Jakar.SqlBuilder.SqlType"/>: one map for every dialect without its own (SQLite, MySQL, Oracle, Firebird), rendered by
        ///     <see cref="Jakar.SqlBuilder.SqlType.ColumnName"/>. Types with no portable equivalent (ranges, arrays, network, full-text, ...) are stored as text, JSON as JSON.
        /// </summary>
        public SqlType GetPortableType( ref readonly DbColumnType dbType, ref readonly int length )
        {
            return dbType switch
                   {
                       DbColumnType.String or DbColumnType.CiText => self.GetCustomAttribute<MaxLengthAttribute>() is { Length: > 0 } attribute
                                                                        ? SqlType.VarChar(attribute.Length)
                                                                        : self.IsKeyColumn()
                                                                            ? SqlType.VarChar(KEY_STRING_LENGTH) // text / CLOB / BLOB columns can't be keys or indexed
                                                                            : SqlType.Text,
                       DbColumnType.Binary => self.GetCustomAttribute<MaxLengthAttribute>() is { Length: > 0 } attribute
                                                  ? SqlType.VarBinary(attribute.Length)
                                                  : SqlType.Blob,
                       DbColumnType.Bit => self.GetCustomAttribute<FixedAttribute>() is { } attribute && attribute.Length > 1
                                               ? SqlType.Binary(( attribute.Length + 7 ) / 8)
                                               : SqlType.Boolean,
                       DbColumnType.Enum                                                                                                    => SqlType.VarChar(Math.Max(length, 1)),
                       DbColumnType.Char                                                                                                    => SqlType.Char(1),
                       DbColumnType.Boolean                                                                                                 => SqlType.Boolean,
                       DbColumnType.Byte or DbColumnType.SByte or DbColumnType.Short or DbColumnType.SmallSerial                           => SqlType.SmallInt,
                       DbColumnType.UShort or DbColumnType.Int or DbColumnType.Serial                                                       => SqlType.Int,
                       DbColumnType.UInt or DbColumnType.Long or DbColumnType.BigSerial or DbColumnType.Oid or DbColumnType.Xid or DbColumnType.Cid => SqlType.BigInt,
                       DbColumnType.ULong or DbColumnType.Xid8 or DbColumnType.PgLsn                                                        => SqlType.Decimal(20),
                       DbColumnType.Int128 or DbColumnType.UInt128                                                                          => SqlType.Decimal(38),
                       DbColumnType.Single                                                                                                  => SqlType.Real,
                       DbColumnType.Double                                                                                                  => SqlType.Double,
                       DbColumnType.Money                                                                                                   => SqlType.Decimal(19, 4),
                       DbColumnType.Decimal or DbColumnType.Numeric => self.GetCustomAttribute<PrecisionAttribute>() is { } attribute
                                                                           ? SqlType.Decimal(attribute.Scope, attribute.Precision)
                                                                           : SqlType.Decimal(DECIMAL_PRECISION, DECIMAL_SCALE),
                       DbColumnType.Date                                                => SqlType.Date,
                       DbColumnType.Time or DbColumnType.TimeSpan or DbColumnType.TimeTz => SqlType.Time,
                       DbColumnType.DateTime                                            => SqlType.Timestamp,
                       DbColumnType.DateTimeOffset                                      => SqlType.TimestampTz,
                       DbColumnType.Guid                                                => SqlType.Guid,
                       DbColumnType.Json or DbColumnType.Jsonb or DbColumnType.JsonPath => SqlType.Json,
                       DbColumnType.NotSet                                              => SqlType.Text,
                       _                                                                => SqlType.Text
                   };
        }

        /// <summary> Whether the column is a key, unique, indexed or a foreign key: most engines can't index unbounded text. </summary>
        private bool IsKeyColumn() => ColumnMetaData.IsDbKey(self) || self.HasAttribute<UniqueAttribute>() || self.GetCustomAttribute<IndexedAttribute>() is not null || self.GetCustomAttribute<ForeignKeyAttribute>() is not null;


        /// <summary>
        ///     The T-SQL (SQL Server 2022) type for <paramref name="dbType"/>. PostgreSQL-only types with no T-SQL equivalent (ranges, arrays, full-text, network, ltree, ...) are stored as text
        ///     (<c>nvarchar(max)</c>, JSON for arrays), and PostgreSQL's internal ids as <c>bigint</c>, so a record using them still has a SQL Server schema.
        /// </summary>
        public string GetMicrosoftSqlServerDataType( ref readonly DbColumnType dbType, ref readonly int length )
        {
            return dbType switch
                   {
                       DbColumnType.Binary => self.GetCustomAttribute<MaxLengthAttribute>() is { Length: > 0 and <= 8000 } attribute
                                                  ? $"varbinary({attribute.Length})"
                                                  : "varbinary(max)",
                       DbColumnType.String => self.GetCustomAttribute<MaxLengthAttribute>() is { Length: > 0 and <= 4000 } attribute
                                                  ? $"nvarchar({attribute.Length})"
                                                  : self.IsKeyColumn()
                                                      ? $"nvarchar({KEY_STRING_LENGTH})" // nvarchar(max) can't be a key or indexed
                                                      : "nvarchar(max)",
                       DbColumnType.Bit => self.GetCustomAttribute<FixedAttribute>() is { } attribute && attribute.Length > 1
                                               ? $"binary({( attribute.Length + 7 ) / 8})"
                                               : "bit",
                       DbColumnType.VarBit => self.GetCustomAttribute<FixedAttribute>() is { } attribute
                                                  ? $"varbinary({( attribute.Length + 7 ) / 8})"
                                                  : "varbinary(max)",
                       DbColumnType.Numeric => self.GetCustomAttribute<PrecisionAttribute>() is { } attribute
                                                   ? $"numeric({attribute.Scope}, {attribute.Precision})"
                                                   : "numeric(18, 0)",
                       DbColumnType.Enum                     => $"nvarchar({length})",
                       DbColumnType.Serial                   => "int", // auto-numbered with IDENTITY(1,1)
                       DbColumnType.BigSerial                => "bigint",
                       DbColumnType.SmallSerial              => "smallint",
                       DbColumnType.Byte                     => "tinyint",
                       DbColumnType.SByte                    => "smallint",
                       DbColumnType.Short                    => "smallint",
                       DbColumnType.UShort                   => "int",
                       DbColumnType.Int                      => "int",
                       DbColumnType.UInt                     => "bigint",
                       DbColumnType.Long                     => "bigint",
                       DbColumnType.ULong                    => "numeric(20, 0)",
                       DbColumnType.Int128                   => "numeric(38, 0)", // T-SQL's widest exact integer
                       DbColumnType.UInt128                  => "numeric(38, 0)",
                       DbColumnType.Single                   => "real",
                       DbColumnType.Double                   => "float",
                       DbColumnType.Decimal                  => self.GetCustomAttribute<PrecisionAttribute>() is { } attribute ? $"numeric({attribute.Scope}, {attribute.Precision})" : DECIMAL_TYPE,
                       DbColumnType.Money                    => "money",
                       DbColumnType.Boolean                  => "bit",
                       DbColumnType.Date                     => "date",
                       DbColumnType.Time                     => "time",
                       DbColumnType.TimeSpan                 => "time",
                       DbColumnType.TimeTz                   => "datetimeoffset",
                       DbColumnType.DateTime                 => "datetime2",
                       DbColumnType.DateTimeOffset           => "datetimeoffset",
                       DbColumnType.Guid                     => "uniqueidentifier",
                       DbColumnType.Char                     => "nchar(1)",
                       DbColumnType.CiText                   => self.IsKeyColumn() ? $"nvarchar({KEY_STRING_LENGTH})" : "nvarchar(max)", // the default collation is case-insensitive
                       DbColumnType.Xml                      => "xml",
                       DbColumnType.Json                     => "nvarchar(max)",
                       DbColumnType.Jsonb                    => "nvarchar(max)",
                       DbColumnType.JsonPath                 => "nvarchar(max)",
                       DbColumnType.Hstore                   => "nvarchar(max)",
                       DbColumnType.Geometry                 => "geometry",
                       DbColumnType.Geography                => "geography",
                       DbColumnType.Point                    => "geometry",
                       DbColumnType.Polygon                  => "geometry",
                       DbColumnType.LineSegment              => "geometry",
                       DbColumnType.Line                     => "geometry",
                       DbColumnType.Box                      => "geometry",
                       DbColumnType.Circle                   => "geometry",
                       DbColumnType.Path                     => "geometry",
                       DbColumnType.Inet                     => "varchar(43)",
                       DbColumnType.Cidr                     => "varchar(43)",
                       DbColumnType.MacAddr                  => "varchar(17)",
                       DbColumnType.MacAddr8                 => "varchar(23)",
                       DbColumnType.Oid                      => "bigint",
                       DbColumnType.Xid                      => "bigint",
                       DbColumnType.Xid8                     => "numeric(20, 0)",
                       DbColumnType.Cid                      => "bigint",
                       DbColumnType.PgLsn                    => "numeric(20, 0)",
                       DbColumnType.IntVector                => "nvarchar(max)", // JSON array
                       DbColumnType.LongVector               => "nvarchar(max)",
                       DbColumnType.FloatVector              => "nvarchar(max)",
                       DbColumnType.DoubleVector             => "nvarchar(max)",
                       DbColumnType.TsVector or DbColumnType.TsQuery or DbColumnType.RegConfig or DbColumnType.RefCursor or DbColumnType.OidVector or DbColumnType.RegType or DbColumnType.Tid
                           or DbColumnType.LTree or DbColumnType.LQuery or DbColumnType.LTxtQuery => "nvarchar(max)",
                       DbColumnType.IntegerRange or DbColumnType.BigIntRange or DbColumnType.NumericRange or DbColumnType.TimestampRange or DbColumnType.DateTimeOffsetRange or DbColumnType.DateRange
                           or DbColumnType.IntMultirange or DbColumnType.LongMultirange or DbColumnType.NumericMultirange or DbColumnType.TimestampMultirange or DbColumnType.DateTimeOffsetMultirange
                           or DbColumnType.DateMultirange => "nvarchar(max)",
                       DbColumnType.NotSet => EMPTY,
                       _                   => throw new OutOfRangeException(self)
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
                       DbColumnType.Decimal                  => self.GetCustomAttribute<PrecisionAttribute>() is { } attribute ? $"numeric({attribute.Scope}, {attribute.Precision})" : DECIMAL_TYPE,
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

            if ( typeof(JNode).IsAssignableFrom(self) ) { return DbColumnType.Json; }

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


        /// <summary> The provider-neutral <see cref="System.Data.DbType"/>, for drivers without a dedicated mapping. Types with no ADO.NET equivalent (PostgreSQL ranges, geometric and system types) are <see cref="System.Data.DbType.Object"/>. </summary>
        public DbType ToDbType()
        {
            return self switch
                   {
                       DbColumnType.Enum           => DbType.Int64,
                       DbColumnType.Serial         => DbType.Int32,
                       DbColumnType.BigSerial      => DbType.Int64,
                       DbColumnType.SmallSerial    => DbType.Int16,
                       DbColumnType.Boolean        => DbType.Boolean,
                       DbColumnType.Short          => DbType.Int16,
                       DbColumnType.UShort         => DbType.UInt16,
                       DbColumnType.Int            => DbType.Int32,
                       DbColumnType.UInt           => DbType.UInt32,
                       DbColumnType.Long           => DbType.Int64,
                       DbColumnType.ULong          => DbType.UInt64,
                       DbColumnType.Int128         => DbType.Decimal,
                       DbColumnType.UInt128        => DbType.Decimal,
                       DbColumnType.Single         => DbType.Single,
                       DbColumnType.Double         => DbType.Double,
                       DbColumnType.Numeric        => DbType.Decimal,
                       DbColumnType.Decimal        => DbType.Decimal,
                       DbColumnType.Money          => DbType.Currency,
                       DbColumnType.Char           => DbType.StringFixedLength,
                       DbColumnType.String         => DbType.String,
                       DbColumnType.CiText         => DbType.String,
                       DbColumnType.Binary         => DbType.Binary,
                       DbColumnType.Date           => DbType.Date,
                       DbColumnType.Time           => DbType.Time,
                       DbColumnType.DateTime       => DbType.DateTime2,
                       DbColumnType.DateTimeOffset => DbType.DateTimeOffset,
                       DbColumnType.TimeSpan       => DbType.Time,
                       DbColumnType.TimeTz         => DbType.Time,
                       DbColumnType.Bit            => DbType.Boolean,
                       DbColumnType.Byte           => DbType.Byte,
                       DbColumnType.SByte          => DbType.SByte,
                       DbColumnType.VarBit         => DbType.String,
                       DbColumnType.Guid           => DbType.Guid,
                       DbColumnType.Xml            => DbType.Xml,
                       DbColumnType.Json           => DbType.String,
                       DbColumnType.Jsonb          => DbType.String,
                       DbColumnType.JsonPath       => DbType.String,
                       DbColumnType.Inet           => DbType.String,
                       DbColumnType.Cidr           => DbType.String,
                       DbColumnType.MacAddr        => DbType.String,
                       DbColumnType.MacAddr8       => DbType.String,
                       _                           => DbType.Object
                   };
        }


        /// <summary> SQLite has four storage classes. Types it can't store natively (decimals, 128-bit integers, dates, GUIDs, JSON, ...) are stored as text; binary and spatial data as blobs. </summary>
        public SqliteType ToSqliteType()
        {
            return self switch
                   {
                       DbColumnType.Enum        => SqliteType.Integer,
                       DbColumnType.Serial      => SqliteType.Integer,
                       DbColumnType.BigSerial   => SqliteType.Integer,
                       DbColumnType.SmallSerial => SqliteType.Integer,
                       DbColumnType.Boolean     => SqliteType.Integer,
                       DbColumnType.Short       => SqliteType.Integer,
                       DbColumnType.UShort      => SqliteType.Integer,
                       DbColumnType.Int         => SqliteType.Integer,
                       DbColumnType.UInt        => SqliteType.Integer,
                       DbColumnType.Long        => SqliteType.Integer,
                       DbColumnType.ULong       => SqliteType.Integer,
                       DbColumnType.Bit         => SqliteType.Integer,
                       DbColumnType.Byte        => SqliteType.Integer,
                       DbColumnType.SByte       => SqliteType.Integer,
                       DbColumnType.Single      => SqliteType.Real,
                       DbColumnType.Double      => SqliteType.Real,
                       DbColumnType.Binary      => SqliteType.Blob,
                       DbColumnType.Geometry    => SqliteType.Blob,
                       DbColumnType.Geography   => SqliteType.Blob,
                       _                        => SqliteType.Text
                   };
        }


        /// <summary> MySQL / MariaDB (MySqlConnector). PostgreSQL-only types (ranges, text search, system and network types other than addresses) have no equivalent and throw. </summary>
        public MySqlDbType ToMySqlDbType()
        {
            return self switch
                   {
                       DbColumnType.Enum           => MySqlDbType.Int64,
                       DbColumnType.Serial         => MySqlDbType.Int32,
                       DbColumnType.BigSerial      => MySqlDbType.Int64,
                       DbColumnType.SmallSerial    => MySqlDbType.Int16,
                       DbColumnType.Boolean        => MySqlDbType.Bool,
                       DbColumnType.Short          => MySqlDbType.Int16,
                       DbColumnType.UShort         => MySqlDbType.UInt16,
                       DbColumnType.Int            => MySqlDbType.Int32,
                       DbColumnType.UInt           => MySqlDbType.UInt32,
                       DbColumnType.Long           => MySqlDbType.Int64,
                       DbColumnType.ULong          => MySqlDbType.UInt64,
                       DbColumnType.Int128         => MySqlDbType.NewDecimal,
                       DbColumnType.UInt128        => MySqlDbType.NewDecimal,
                       DbColumnType.Single         => MySqlDbType.Float,
                       DbColumnType.Double         => MySqlDbType.Double,
                       DbColumnType.Numeric        => MySqlDbType.NewDecimal,
                       DbColumnType.Decimal        => MySqlDbType.NewDecimal,
                       DbColumnType.Money          => MySqlDbType.NewDecimal,
                       DbColumnType.Char           => MySqlDbType.String,
                       DbColumnType.String         => MySqlDbType.VarChar,
                       DbColumnType.CiText         => MySqlDbType.VarChar,
                       DbColumnType.Binary         => MySqlDbType.Blob,
                       DbColumnType.Date           => MySqlDbType.Date,
                       DbColumnType.Time           => MySqlDbType.Time,
                       DbColumnType.DateTime       => MySqlDbType.DateTime,
                       DbColumnType.DateTimeOffset => MySqlDbType.DateTime, // MySQL has no offset type; MySqlConnector writes the UTC value
                       DbColumnType.TimeSpan       => MySqlDbType.Time,
                       DbColumnType.TimeTz         => MySqlDbType.Time,
                       DbColumnType.Bit            => MySqlDbType.Bit,
                       DbColumnType.VarBit         => MySqlDbType.Bit,
                       DbColumnType.Byte           => MySqlDbType.UByte,
                       DbColumnType.SByte          => MySqlDbType.Byte,
                       DbColumnType.Guid           => MySqlDbType.Guid,
                       DbColumnType.Xml            => MySqlDbType.Text,
                       DbColumnType.Json           => MySqlDbType.JSON,
                       DbColumnType.Jsonb          => MySqlDbType.JSON,
                       DbColumnType.Inet           => MySqlDbType.VarChar,
                       DbColumnType.Cidr           => MySqlDbType.VarChar,
                       DbColumnType.MacAddr        => MySqlDbType.VarChar,
                       DbColumnType.MacAddr8       => MySqlDbType.VarChar,
                       DbColumnType.Point          => MySqlDbType.Geometry,
                       DbColumnType.Line           => MySqlDbType.Geometry,
                       DbColumnType.LineSegment    => MySqlDbType.Geometry,
                       DbColumnType.Path           => MySqlDbType.Geometry,
                       DbColumnType.Polygon        => MySqlDbType.Geometry,
                       DbColumnType.Box            => MySqlDbType.Geometry,
                       DbColumnType.Circle         => MySqlDbType.Geometry,
                       DbColumnType.Geometry       => MySqlDbType.Geometry,
                       DbColumnType.Geography      => MySqlDbType.Geometry,
                       DbColumnType.NotSet         => MySqlDbType.Null,
                       _                           => throw new OutOfRangeException(self)
                   };
        }
    }
}
