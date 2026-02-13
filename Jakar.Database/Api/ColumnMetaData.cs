namespace Jakar.Database;


[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed class ColumnMetaData
{
    public static readonly ColumnMetaData AdditionalData = new(nameof(IJsonModel.AdditionalData), PostgresType.Jsonb, ColumnOptions.Nullable);
    public static readonly ColumnMetaData CreatedBy      = new(nameof(ICreatedBy.CreatedBy), PostgresType.Guid, ColumnOptions.Nullable, null, ForeignKeyInfo.Create<UserRecord>(OnActionInfo.OnDelete()));
    public static readonly ColumnMetaData DateCreated    = new(nameof(IDateCreated.DateCreated), PostgresType.DateTimeOffset, ColumnOptions.Indexed);
    public static readonly ColumnMetaData ID             = new(nameof(IUniqueID.ID), PostgresType.Guid, ColumnOptions.PrimaryKey                       | ColumnOptions.AlwaysIdentity);
    public static readonly ColumnMetaData LastModified   = new(nameof(ILastModified.LastModified), PostgresType.DateTimeOffset, ColumnOptions.Nullable | ColumnOptions.Indexed);


    public readonly bool            IsNullable;
    public readonly bool            IsPrimaryKey;
    public readonly bool            IsUnique;
    public readonly bool            IsAlwaysIdentity;
    public readonly bool            IsDefaultIdentity;
    public readonly ColumnChecks?   Checks;
    public readonly PostgresType    DbType;
    public readonly SizeInfo?       Length;
    public readonly string          ColumnName;
    public readonly string          KeyValuePair;
    public readonly string          PropertyName;
    public readonly string          VariableName;
    public readonly ForeignKeyInfo  ForeignKeyName;
    public readonly string?         IndexColumnName;
    public readonly string          DataType;
    public readonly NpgsqlDbType    PostgresDbType;
    public readonly ColumnDefaults? Defaults;
    public readonly bool            IsFixed;
    private         string?         _ColumnName_Padded;
    private         string?         _KeyValuePair_Padded;
    private         string?         _VariableName_Padded;
    private         string?         _IndexColumnName_Padded;
    private         string?         _DataType_Padded;


    public int  Index     { get; internal set; } = -1;
    public bool IsIndexed { [MemberNotNullWhen(true, nameof(IndexColumnName))] get => !string.IsNullOrWhiteSpace(IndexColumnName); }


    public ColumnMetaData( in string propertyName, in PostgresType dbType, in ColumnOptions options, in SizeInfo? length = null, in ForeignKeyInfo? foreignKeyName = null, in ColumnDefaults? defaults = null )
    {
        string columnName = Validate.ThrowIfNull(propertyName.SqlColumnName());
        IsNullable        = options.HasFlagValue(ColumnOptions.Nullable);
        IsPrimaryKey      = options.HasFlagValue(ColumnOptions.PrimaryKey);
        IsFixed           = options.HasFlagValue(ColumnOptions.Fixed);
        IsDefaultIdentity = options.HasFlagValue(ColumnOptions.DefaultIdentity);
        IsAlwaysIdentity  = options.HasFlagValue(ColumnOptions.AlwaysIdentity);
        IsUnique          = options.HasFlagValue(ColumnOptions.Unique);
        Checks            = length?.Check(columnName);
        Defaults          = defaults;
        DbType            = dbType;
        Length            = length;
        PropertyName      = propertyName;
        ColumnName        = columnName;
        KeyValuePair      = $"{columnName} = @{columnName}";
        VariableName      = $"@{columnName}";
        DataType          = GetPostgresDataType(in options, in dbType, in length);
        IndexColumnName   = propertyName.SqlColumnIndexName(in options);
        ForeignKeyName    = foreignKeyName ?? ForeignKeyInfo.Empty;
        PostgresDbType    = dbType.ToNpgsqlDbType();
    }
    public static ColumnMetaData Create( in PropertyInfo property ) => Create(in property, property.GetCustomAttribute<ColumnInfoAttribute>() ?? ColumnInfoAttribute.Empty);
    public static ColumnMetaData Create( in PropertyInfo property, in ColumnInfoAttribute attribute )
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(attribute, $"{property.DeclaringType?.Name}.{property.Name}");

        try
        {
            string propertyName = property.Name;
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

            ColumnOptions   options  = attribute.Options;
            SizeInfo?       length   = attribute.Length;
            ColumnDefaults? defaults = attribute.Defaults;
            PostgresType    dbType   = property.PropertyType.GetPostgresType(in property, ref options, ref length);
            if ( IsDbKey(property) ) { options |= ColumnOptions.PrimaryKey; }

            ForeignKeyInfo? foreignKey = attribute.ForeignKey;
            if ( options.HasFlagValue(ColumnOptions.ForeignKey) && foreignKey.HasValue && string.IsNullOrWhiteSpace(foreignKey.Value.ForeignTableName) ) { throw new ArgumentException($"Column '{propertyName}' has a {nameof(ColumnOptions.ForeignKey)} flag but {nameof(foreignKey)} is invalid.", nameof(foreignKey)); }

            if ( options.HasFlagValue(ColumnOptions.ForeignKey) && options.HasFlagValue(ColumnOptions.Indexed) ) { throw new ArgumentException($"Column '{propertyName}' cannot be both {nameof(ColumnOptions.Indexed)} and a {nameof(ColumnOptions.ForeignKey)}. {nameof(ColumnOptions.ForeignKey)} columns are automatically indexed.", nameof(options)); }

            return new ColumnMetaData(in propertyName, in dbType, in options, in length, in foreignKey, in defaults);
        }
        catch ( Exception ex ) { throw new InvalidOperationException($"Failed to create ColumnMetaData for property '{property.DeclaringType?.FullName}.{property.Name}'.", ex); }
    }

    internal      string ColumnName_Padded( ITableMetaData      table )    => _ColumnName_Padded ??= ColumnName.PadRight(table.MaxLength_ColumnName);
    internal      string KeyValuePair_Padded( ITableMetaData    table )    => _KeyValuePair_Padded ??= KeyValuePair.PadRight(table.MaxLength_KeyValuePair);
    internal      string VariableName_Padded( ITableMetaData    table )    => _VariableName_Padded ??= VariableName.PadRight(table.MaxLength_Variables);
    internal      string IndexColumnName_Padded( ITableMetaData table )    => _IndexColumnName_Padded ??= IndexColumnName?.PadRight(table.MaxLength_IndexColumnName) ?? EMPTY;
    internal      string DataType_Padded( ITableMetaData        table )    => _DataType_Padded ??= DataType.PadRight(table.MaxLength_DataType);
    public static string GetColumnName( ColumnMetaData          column )   => column.ColumnName;
    public static string GetVariableName( ColumnMetaData        column )   => column.VariableName;
    public static string GetKeyValuePair( ColumnMetaData        column )   => column.KeyValuePair;
    public static bool   IsDbKey( MemberInfo                    property ) => property.GetCustomAttribute<KeyAttribute>() is not null || property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null;


    public NpgsqlParameter ToParameter<T>( T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        NpgsqlParameter parameter = new(parameterName.SqlColumnName(), PostgresDbType, 0, ColumnName)
                                    {
                                        IsNullable    = IsNullable,
                                        SourceVersion = sourceVersion,
                                        Direction     = direction,
                                        Value         = value
                                    };

        return parameter;
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

                   PostgresType.Serial                   => "serial",
                   PostgresType.BigSerial                => @"bigserial",
                   PostgresType.SmallSerial              => @"smallserial",
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
                   PostgresType.NotSet                   => nameof(NotImplementedException),
                   _                                     => throw new OutOfRangeException(self)
               };
    }
}
