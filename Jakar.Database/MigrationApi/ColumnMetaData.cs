namespace Jakar.Database;


[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed class ColumnMetaData : IEquatable<ColumnMetaData>, IComparable<ColumnMetaData>, IComparable
{
    public readonly              bool                                   IsAlwaysIdentity;
    public readonly              bool                                   IsDefaultIdentity;
    public readonly              bool                                   IsFixed;
    public readonly              bool                                   IsNullable;
    public readonly              bool                                   IsPrimaryKey;
    public readonly              bool                                   IsUnique;
    public readonly              ChecksAttribute?                       Checks;
    public readonly              DbSizeAttribute?                       Length;
    public readonly              DefaultsAttribute?                     Defaults;
    public readonly              ForeignKeyAttribute?                   ForeignKey;
    public readonly              IndexedAttribute?                      Indexed;
    public readonly              DbColumnType                           DbType;
    public readonly              string                                 ColumnName;
    public readonly              string                                 KeyValuePair;
    public readonly              string                                 PropertyName;
    public readonly              string                                 VariableName;
    [JsonIgnore] public readonly Type                                   PropertyType;
    private                      NpgsqlDbType?                          __postgresDbType;
    private                      SqlDbType?                             __sqlDbType;
    public readonly              FrozenDictionary<DatabaseType, string> DataTypes;
    private                      Type?                                  _propertyType;

    [JsonIgnore] public DataColumn DataColumn
    {
        get
        {
            _propertyType ??= PropertyType.TryGetUnderlyingType(out Type? type)
                                  ? type
                                  : PropertyType;

            DataColumn column = new(ColumnName, _propertyType, null, MappingType.Element)
                                {
                                    AllowDBNull       = IsNullable,
                                    AutoIncrement     = DbType is DbColumnType.BigSerial or DbColumnType.Serial or DbColumnType.SmallSerial,
                                    AutoIncrementSeed = 1,
                                    AutoIncrementStep = 1,
                                    MaxLength         = Length?.Max ?? -1,
                                    Unique            = IsUnique
                                };

            if ( DbType is DbColumnType.DateTime ) { column.DateTimeMode = DataSetDateTime.Utc; }

            return column;
        }
    }

    public bool         HasCheckConstraint      { [MemberNotNullWhen(true, nameof(Checks))] get => Checks?.IsValid is true; }
    public bool         HasDefaultConstraint    { [MemberNotNullWhen(true, nameof(Defaults))] get => Defaults?.IsValid is true; }
    public bool         HasForeignKeyConstraint { [MemberNotNullWhen(true, nameof(ForeignKey))] get => ForeignKey?.IsValid is true; }
    public bool         HasLengthConstraint     { [MemberNotNullWhen(true, nameof(Length))] get => Length is not null; }
    public int          Index                   { get; internal set; } = -1;
    public bool         IsColumnIndexed         { [MemberNotNullWhen(true, nameof(Indexed))] get => Indexed?.IsValid is true || HasForeignKeyConstraint; }
    public NpgsqlDbType PostgresDbType          => __postgresDbType ??= DbType.ToNpgsqlDbType();
    public SqlDbType    SqlDbType               => __sqlDbType ??= DbType.ToSqlDbType();
    public ref readonly string this[ DatabaseType type ] => ref DataTypes[type];


    public ColumnMetaData( in PropertyInfo property )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(property);
            string propertyName = property.Name;
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            string columnName = Validate.ThrowIfNull(propertyName.SqlName());

            IsPrimaryKey      = IsDbKey(property);
            PropertyType      = property.PropertyType;
            IsFixed           = property.HasAttribute<FixedAttribute>();
            IsDefaultIdentity = property.HasAttribute<DefaultIdentityAttribute>();
            IsAlwaysIdentity  = property.HasAttribute<AlwaysIdentityAttribute>();
            IsUnique          = property.HasAttribute<UniqueAttribute>();
            Checks            = property.GetCustomAttribute<ChecksAttribute>();
            Defaults          = property.GetCustomAttribute<DefaultsAttribute>();
            ForeignKey        = property.GetCustomAttribute<ForeignKeyAttribute>();
            Indexed           = property.GetCustomAttribute<IndexedAttribute>();
            Length            = property.GetCustomAttribute<DbSizeAttribute>();
            PropertyName      = propertyName;
            ColumnName        = columnName;
            KeyValuePair      = $"{columnName} = @{columnName}";
            VariableName      = $"@{columnName}";
            DataTypes         = property.GetDataTypes(out DbType, out IsNullable);

            if ( IsPrimaryKey && ForeignKey?.IsValid is true ) { throw new ArgumentException($"Column '{PropertyName}' has a PrimaryKey flag but {nameof(ForeignKey)} is invalid.", nameof(ForeignKey)); }
        }
        catch ( Exception ex ) { throw new InvalidOperationException($"Failed to create ColumnMetaData for property '{property.DeclaringType?.FullName}.{property.Name}'.", ex); }
    }
    public static ColumnMetaData Create( in PropertyInfo property ) => new(in property);


    internal void AddData<TMetaData>( StringBuilder query, TMetaData Instance, ref readonly DatabaseType type )
        where TMetaData : ITableMetaData
    {
        string columnName = ColumnName.GetPadded(Instance.MaxLength_ColumnName);
        string dataType   = DataTypes[type].GetPadded(Instance.MaxLength_DataType);
        query.Append($"    {columnName} {dataType}");


        if ( IsPrimaryKey )
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            query.Append(DbType switch
                         {
                             // DbColumnType.Long or DbColumnType.Int or DbColumnType.Short => " PRIMARY KEY",
                             DbColumnType.Guid => " DEFAULT gen_random_uuid() PRIMARY KEY",
                             _                 => " PRIMARY KEY"
                         });

            return;
        }


        query.Append(IsNullable
                         ? " NULL"
                         : " NOT NULL");

        if ( IsUnique ) { query.Append(" UNIQUE"); }

        if ( IsAlwaysIdentity ) { query.Append(" GENERATED ALWAYS AS IDENTITY"); }
        else if ( IsDefaultIdentity ) { query.Append(" GENERATED BY DEFAULT AS IDENTITY"); }

        if ( Checks?.IsValid is true )
        {
            query.Append(" CHECK ( ");

            query.AppendJoin(Checks.And
                                 ? AND
                                 : OR,
                             Checks.Constraints);

            query.Append(" )");
        }

        if ( Defaults?.IsValid is not true ) { return; }

        query.Append(" DEFAULTS ");
        query.Append(Defaults.Value);
    }


    internal      string KeyValuePair_Padded( ITableMetaData    table )    => KeyValuePair.GetPadded(table.MaxLength_KeyValuePair);
    internal      string VariableName_Padded( ITableMetaData    table )    => VariableName.GetPadded(table.MaxLength_Variables);
    internal      string CreateIndex( ITableMetaData            table )    => $"CREATE INDEX IF NOT EXISTS {IndexColumnName_Padded(table)} ON {table.TableName}({ColumnName});";
    internal      string IndexColumnName_Padded( ITableMetaData table )    => Indexed?.Name.GetPadded(table.MaxLength_IndexColumnName) ?? ForeignKey?.Index(ColumnName, table.MaxLength_IndexColumnName) ?? EMPTY;
    public static string GetColumnName( ColumnMetaData          column )   => column.ColumnName;
    public static string GetVariableName( ColumnMetaData        column )   => column.VariableName;
    public static string GetKeyValuePair( ColumnMetaData        column )   => column.KeyValuePair;
    public static bool   IsDbKey( MemberInfo                    property ) => property.GetCustomAttribute<KeyAttribute>() is not null || property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null;


    public Microsoft.Data.SqlClient.SqlParameter ToSqlParameter( object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        Microsoft.Data.SqlClient.SqlParameter parameter = new(parameterName.Parameterize(), SqlDbType, 0, ColumnName)
                                                          {
                                                              IsNullable    = IsNullable,
                                                              SourceVersion = sourceVersion,
                                                              Direction     = direction,
                                                              Value         = value ?? DBNull.Value
                                                          };

        return parameter;
    }
    public NpgsqlParameter ToPostgresParameter( object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        NpgsqlParameter parameter = new(parameterName.Parameterize(), PostgresDbType, 0, ColumnName)
                                    {
                                        IsNullable    = IsNullable,
                                        SourceVersion = sourceVersion,
                                        Direction     = direction,
                                        Value         = value ?? DBNull.Value
                                    };

        return parameter;
    }
    public SqlParameter ToParameter( object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        Debug.Assert(Index >= 0);
        SqlParameter parameter = new(value, parameterName.Parameterize(), this, direction, sourceVersion);
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


    public int CompareTo( ColumnMetaData? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        return Index.CompareTo(other.Index);
    }
    public int CompareTo( object? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        return other is ColumnMetaData x
                   ? CompareTo(x)
                   : throw new ExpectedValueTypeException(other, typeof(ColumnMetaData));
    }
    public bool Equals( ColumnMetaData? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return DbType == other.DbType && PropertyName == other.PropertyName;
    }
    public override bool Equals( object? obj )                                      => ReferenceEquals(this, obj) || ( obj is ColumnMetaData other && Equals(other) );
    public override int  GetHashCode()                                              => HashCode.Combine((int)DbType, PropertyName);
    public static   bool operator ==( ColumnMetaData? left, ColumnMetaData? right ) => Equals(left, right);
    public static   bool operator !=( ColumnMetaData? left, ColumnMetaData? right ) => !Equals(left, right);
    public static   bool operator <( ColumnMetaData?  left, ColumnMetaData? right ) => Comparer<ColumnMetaData>.Default.Compare(left, right) < 0;
    public static   bool operator >( ColumnMetaData?  left, ColumnMetaData? right ) => Comparer<ColumnMetaData>.Default.Compare(left, right) > 0;
    public static   bool operator <=( ColumnMetaData? left, ColumnMetaData? right ) => Comparer<ColumnMetaData>.Default.Compare(left, right) <= 0;
    public static   bool operator >=( ColumnMetaData? left, ColumnMetaData? right ) => Comparer<ColumnMetaData>.Default.Compare(left, right) >= 0;
}
