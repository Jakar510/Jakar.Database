namespace Jakar.Database;


public sealed class ColumnMetaData
{
    public static readonly ColumnMetaData AdditionalData = new(nameof(IJsonModel.AdditionalData), PostgresType.Jsonb, ColumnOptions.Nullable);
    public static readonly ColumnMetaData CreatedBy      = new(nameof(ICreatedBy.CreatedBy), PostgresType.Guid, ColumnOptions.Nullable, UserRecord.TABLE_NAME);
    public static readonly ColumnMetaData DateCreated    = new(nameof(IDateCreated.DateCreated), PostgresType.DateTimeOffset, ColumnOptions.Indexed);
    public static readonly ColumnMetaData ID             = new(nameof(IUniqueID.ID), PostgresType.Guid, ColumnOptions.PrimaryKey                       | ColumnOptions.AlwaysIdentity);
    public static readonly ColumnMetaData LastModified   = new(nameof(ILastModified.LastModified), PostgresType.DateTimeOffset, ColumnOptions.Nullable | ColumnOptions.Indexed);


    public readonly bool                   IsNullable;
    public readonly bool                   IsPrimaryKey;
    public readonly bool                   IsUnique;
    public readonly bool                   IsIndexed;
    public readonly bool                   IsAlwaysIdentity;
    public readonly bool                   IsDefaultIdentity;
    public readonly ColumnCheckMetaData?   Checks;
    public readonly PostgresType           DbType;
    public readonly SizeInfo?              Length;
    public readonly string                 ColumnName;
    public readonly string                 KeyValuePair;
    public readonly string                 SpacedName;
    public readonly string                 PropertyName;
    public readonly string                 VariableName;
    public readonly string?                ForeignKeyName;
    public readonly string?                IndexColumnName;
    public readonly string                 DataType;
    public readonly NpgsqlDbType           PostgresDbType;
    public readonly ColumnDefaultMetaData? Defaults;
    public readonly bool                   IsFixed;

    public int  Index        { get; internal set; } = -1;
    public bool IsForeignKey { [MemberNotNullWhen(true, nameof(IndexColumnName))] get => !string.IsNullOrWhiteSpace(IndexColumnName); }


    public ColumnMetaData( in string propertyName, in PostgresType dbType, in ColumnOptions options = ColumnOptions.None, in string? foreignKeyName = null, in SizeInfo? length = null, in ColumnCheckMetaData? checks = null, in ColumnDefaultMetaData? defaults = null )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        if ( ( options & ColumnOptions.ForeignKey ) != 0 && string.IsNullOrWhiteSpace(foreignKeyName) ) { throw new ArgumentException($"Column '{propertyName}' has a {nameof(ColumnOptions.ForeignKey)} flag but {nameof(foreignKeyName)} is invalid.", nameof(foreignKeyName)); }

        if ( ( options & ColumnOptions.ForeignKey ) != 0 && ( options & ColumnOptions.Indexed ) != 0 ) { throw new ArgumentException($"Column '{propertyName}' cannot be both {nameof(ColumnOptions.Indexed)} and a {nameof(ColumnOptions.ForeignKey)}. {nameof(ColumnOptions.ForeignKey)} columns are automatically indexed.", nameof(options)); }


        string columnName = Validate.ThrowIfNull(propertyName.SqlColumnName());
        IsNullable        = options.HasFlagValue(ColumnOptions.Nullable);
        IsPrimaryKey      = options.HasFlagValue(ColumnOptions.PrimaryKey);
        IsFixed           = options.HasFlagValue(ColumnOptions.Fixed);
        IsDefaultIdentity = options.HasFlagValue(ColumnOptions.DefaultIdentity);
        IsAlwaysIdentity  = options.HasFlagValue(ColumnOptions.AlwaysIdentity);
        IsIndexed         = options.HasFlagValue(ColumnOptions.Indexed);
        IsUnique          = options.HasFlagValue(ColumnOptions.Unique);
        Checks            = checks ?? length?.Check(columnName);
        Defaults          = defaults;
        DbType            = dbType;
        Length            = length;
        PropertyName      = propertyName;
        ColumnName        = columnName;
        KeyValuePair      = $" {columnName} = @{columnName} ";
        SpacedName        = $" {columnName} ";
        VariableName      = $" @{columnName} ";
        DataType          = GetPostgresDataType(in options, in dbType, in length);
        IndexColumnName   = propertyName.SqlColumnIndexName(in options);
        ForeignKeyName    = foreignKeyName?.SqlColumnName();
        PostgresDbType    = dbType.ToNpgsqlDbType();
    }


    public static string GetColumnName( ColumnMetaData   column )   => column.ColumnName;
    public static string GetVariableName( ColumnMetaData column )   => column.VariableName;
    public static string GetKeyValuePair( ColumnMetaData column )   => column.KeyValuePair;
    public static bool   IsDbKey( MemberInfo             property ) => property.GetCustomAttribute<KeyAttribute>() is not null || property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null;


    public static ColumnMetaData Create( in PropertyInfo property ) => Create(in property, property.GetCustomAttribute<ColumnMetaDataAttribute>() ?? ColumnMetaDataAttribute.Default);
    internal static ColumnMetaData Create( in PropertyInfo property, in ColumnMetaDataAttribute attribute )
    {
        ArgumentNullException.ThrowIfNull(attribute, $"{property.DeclaringType?.Name}.{property.Name}");

        try
        {
            attribute.Deconstruct(out ColumnOptions options, out SizeInfo? length, out ColumnCheckMetaData? checks, out string? foreignKeyName);

            PostgresType dbType = property.PropertyType.GetPostgresType(in property, ref options, ref length);
            if ( IsDbKey(property) ) { options |= ColumnOptions.PrimaryKey; }

            return new ColumnMetaData(property.Name, dbType, options, foreignKeyName, length, checks);
        }
        catch ( Exception ex ) { throw new InvalidOperationException($"Failed to create ColumnMetaData for property '{property.DeclaringType?.FullName}.{property.Name}'.", ex); }
    }


    public static Func<TSelf, object?> GetTablePropertyValueAccessor<TSelf>( string propertyName ) => GetTablePropertyValueAccessor<TSelf>(typeof(TSelf).GetProperty(propertyName) ?? throw new InvalidOperationException($"Property '{propertyName}' not found on type '{typeof(TSelf).FullName}'"));
    private static Func<TSelf, object?> GetTablePropertyValueAccessor<TSelf>( PropertyInfo property )
    {
        // Validate getter and declaring type once
        MethodInfo? getter = property.GetMethod;
        if ( getter is null ) { throw new InvalidOperationException($"Property '{property.Name}' does not have a getter."); }

        Type? declaringType = getter.DeclaringType;
        if ( declaringType is null ) { throw new InvalidOperationException($"Getter for property '{property.Name}' has no declaring type."); }

        // Clear, per-property name helps when inspecting emitted methods
        string methodName = string.Concat(declaringType.FullName, ".__get_", property.Name);

        // Create Emit for the delegate type we want: Func<TSelf, object?>
        Emit<Func<TSelf, object?>> emit = Emit<Func<TSelf, object?>>.NewDynamicMethod(declaringType, methodName);

        // Load the instance argument
        emit.LoadArgument(0);

        // Only cast when the getter's declaring type differs from TSelf
        if ( declaringType != typeof(TSelf) ) { emit.CastClass(declaringType); }

        // Call the getter
        emit.Call(getter);

        // Box value types (reference types require no boxing)
        if ( property.PropertyType.IsValueType ) { emit.Box(property.PropertyType); }

        // Return and create delegate
        emit.Return();
        return emit.CreateDelegate();
    }


    public static string GetPostgresDataType( in ColumnOptions options, in PostgresType self, in SizeInfo? sizeInfo )
    {
        SizeInfo info = sizeInfo ?? SizeInfo.Empty;

        int length = info.IsInt
                         ? info.AsInt
                         : -1;

        PrecisionInfo precision = info.IsPrecisionInfo
                                      ? info.AsPrecisionInfo
                                      : self switch
                                        {
                                            PostgresType.Double  => PrecisionInfo.Double,
                                            PostgresType.Single  => PrecisionInfo.Float,
                                            PostgresType.Int128  => PrecisionInfo.Int128,
                                            PostgresType.UInt128 => PrecisionInfo.Int128,
                                            PostgresType.Decimal => PrecisionInfo.Decimal,
                                            _                    => PrecisionInfo.Empty
                                        };

        return self switch
               {
                   /*
                       PostgresType.String => length > 0
                                                  ? length.Value < MAX_FIXED && options.HasFlagValue(ColumnOptions.Fixed)
                                                        ? $"character({length.Value})"
                                                        : $"varchar({length.Value})"
                                                  : "text",
                       */

                   // !IsValidLength() => throw new OutOfRangeException(Length, $"Max length for Unicode strings is {Constants.UNICODE_CAPACITY}"),
                   PostgresType.Binary => precision.IsValid
                                              ? @$"varbit({precision.Scope})"
                                              : "bytea",
                   PostgresType.String => options.HasFlagValue(ColumnOptions.Fixed)
                                              ? length > 0
                                                    ? $"varchar({length})"
                                                    : "text"
                                              : "text",
                   PostgresType.Byte => length > 0
                                            ? $"bit({length})"
                                            : "bit(8)",

                   PostgresType.SByte => length > 0
                                             ? $"bit({length})"
                                             : "bit(8)",

                   PostgresType.Bit => length > 0
                                           ? $"bit({length})"
                                           : "bit(8)",

                   PostgresType.VarBit => length > 0
                                              ? $"bit varying({length})"
                                              : "bit varying(8)",

                   PostgresType.Short                    => "smallint",
                   PostgresType.UShort                   => "smallint",
                   PostgresType.Int                      => "integer",
                   PostgresType.UInt                     => "integer",
                   PostgresType.Long                     => "bigint",
                   PostgresType.ULong                    => "bigint",
                   PostgresType.Single                   => "float4",
                   PostgresType.Double                   => "double precision",
                   PostgresType.Decimal                  => "numeric(28, 28)",
                   PostgresType.Boolean                  => "bool",
                   PostgresType.Date                     => "date",
                   PostgresType.Time                     => "time",
                   PostgresType.DateTime                 => "timestamp",
                   PostgresType.DateTimeOffset           => @"timestamptz",
                   PostgresType.Money                    => "money",
                   PostgresType.Guid                     => "uuid",
                   PostgresType.Json                     => "json",
                   PostgresType.Xml                      => "xml",
                   PostgresType.Enum                     => "enum",
                   PostgresType.Polygon                  => "polygon",
                   PostgresType.LineSegment              => @"lseg",
                   PostgresType.Point                    => "point",
                   PostgresType.Int128                   => "int16",
                   PostgresType.UInt128                  => "uint16",
                   PostgresType.Numeric                  => "numeric",
                   PostgresType.Box                      => "box",
                   PostgresType.Circle                   => "circle",
                   PostgresType.Line                     => "line",
                   PostgresType.Path                     => "path",
                   PostgresType.Char                     => "char",
                   PostgresType.CiText                   => @"citext",
                   PostgresType.TimeSpan                 => "interval",
                   PostgresType.TimeTz                   => "time with time zone",
                   PostgresType.Inet                     => "inet",
                   PostgresType.Cidr                     => "cidr",
                   PostgresType.MacAddr                  => @"macaddr",
                   PostgresType.MacAddr8                 => "macaddr8",
                   PostgresType.TsVector                 => @"tsvector",
                   PostgresType.TsQuery                  => @"tsquery",
                   PostgresType.RegConfig                => @"regconfig",
                   PostgresType.Jsonb                    => "jsonb",
                   PostgresType.JsonPath                 => "jsonpath",
                   PostgresType.Hstore                   => "hstore",
                   PostgresType.RefCursor                => @"refcursor",
                   PostgresType.OidVector                => @"oidvector",
                   PostgresType.Oid                      => "oid",
                   PostgresType.Xid                      => "xid",
                   PostgresType.Xid8                     => "xid8",
                   PostgresType.Cid                      => "cit",
                   PostgresType.RegType                  => @"regtype",
                   PostgresType.Tid                      => "tid",
                   PostgresType.PgLsn                    => @"pglsn",
                   PostgresType.Geometry                 => "geometry",
                   PostgresType.Geography                => "geodetic",
                   PostgresType.LTree                    => @"ltree",
                   PostgresType.LQuery                   => @"lquery",
                   PostgresType.LTxtQuery                => @"ltxtquery",
                   PostgresType.IntVector                => "integer[]",
                   PostgresType.LongVector               => "bigint[]",
                   PostgresType.FloatVector              => "float[]",
                   PostgresType.DoubleVector             => "double[]",
                   PostgresType.IntegerRange             => "int4range",
                   PostgresType.BigIntRange              => "int8range",
                   PostgresType.NumericRange             => @"numrange",
                   PostgresType.TimestampRange           => @"tsrange",
                   PostgresType.DateTimeOffsetRange      => @"tstzrange",
                   PostgresType.DateRange                => @"daterange",
                   PostgresType.IntMultirange            => "int4multirange",
                   PostgresType.LongMultirange           => "int8multirange",
                   PostgresType.NumericMultirange        => @"nummultirange",
                   PostgresType.TimestampMultirange      => @"tsmultirange",
                   PostgresType.DateTimeOffsetMultirange => @"tstzmultirange",
                   PostgresType.DateMultirange           => @"datemultirange",
                   PostgresType.NotSet                   => throw new OutOfRangeException(self),
                   _                                     => throw new OutOfRangeException(self)
               };
    }
}
