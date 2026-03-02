namespace Jakar.Database;


[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed class ColumnMetaData
{
    private static readonly ConcurrentDictionary<string, string> parameterNameCache = new(Environment.ProcessorCount, DEFAULT_CAPACITY, StringComparer.InvariantCulture);
    public readonly         bool                                 IsNullable;
    public readonly         bool                                 IsPrimaryKey;
    public readonly         bool                                 IsUnique;
    public readonly         bool                                 IsAlwaysIdentity;
    public readonly         bool                                 IsDefaultIdentity;
    public readonly         ChecksAttribute?                     Checks;
    public readonly         PostgresType                         DbType;
    public readonly         string                               ColumnName;
    public readonly         string                               KeyValuePair;
    public readonly         string                               PropertyName;
    public readonly         string                               VariableName;
    public readonly         ForeignKeyAttribute?                 ForeignKey;
    public readonly         IndexedAttribute?                    Indexed;
    public readonly         string                               DataType;
    public readonly         NpgsqlDbType                         PostgresDbType;
    public readonly         DefaultsAttribute?                   Defaults;
    public readonly         bool                                 IsFixed;
    public readonly         DbSizeAttribute?                     Length;


    public int  Index                   { get; internal set; } = -1;
    public bool IsColumnIndexed         { [MemberNotNullWhen(true, nameof(Indexed))] get => Indexed?.IsValid is true || HasForeignKeyConstraint; }
    public bool HasForeignKeyConstraint { [MemberNotNullWhen(true, nameof(ForeignKey))] get => ForeignKey?.IsValid is true; }
    public bool HasDefaultConstraint    { [MemberNotNullWhen(true, nameof(Defaults))] get => Defaults?.IsValid is true; }
    public bool HasCheckConstraint      { [MemberNotNullWhen(true, nameof(Checks))] get => Checks?.IsValid is true; }
    public bool HasLengthConstraint     { [MemberNotNullWhen(true, nameof(Length))] get => Length is not null; }


    public ColumnMetaData( in PropertyInfo property )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(property);
            string propertyName = property.Name;
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            string columnName = Validate.ThrowIfNull(propertyName.SqlName());

            IsPrimaryKey      = IsDbKey(property);
            IsFixed           = property.HasAttribute<FixedAttribute>();
            IsDefaultIdentity = property.HasAttribute<DefaultIdentityAttribute>();
            IsAlwaysIdentity  = property.HasAttribute<AlwaysIdentityAttribute>();
            IsUnique          = property.HasAttribute<UniqueAttribute>();
            Checks            = property.GetCustomAttribute<ChecksAttribute>();
            Defaults          = property.GetCustomAttribute<DefaultsAttribute>();
            ForeignKey        = property.GetCustomAttribute<ForeignKeyAttribute>();
            Indexed           = property.GetCustomAttribute<IndexedAttribute>();
            Length            = property.GetCustomAttribute<DbSizeAttribute>();
            DataType          = property.GetPostgresDataType(out DbType, out PostgresDbType, out IsNullable);
            PropertyName      = propertyName;
            ColumnName        = columnName;
            KeyValuePair      = $"{columnName} = @{columnName}";
            VariableName      = $"@{columnName}";

            if ( IsPrimaryKey && ForeignKey?.IsValid is true ) { throw new ArgumentException($"Column '{propertyName}' has a PrimaryKey flag but {nameof(ForeignKey)} is invalid.", nameof(ForeignKey)); }
        }
        catch ( Exception ex ) { throw new InvalidOperationException($"Failed to create ColumnMetaData for property '{property.DeclaringType?.FullName}.{property.Name}'.", ex); }
    }
    public static ColumnMetaData Create( in PropertyInfo property ) => new(in property);


    internal void AddData<TMetaData>( StringBuilder query, TMetaData Instance )
        where TMetaData : ITableMetaData
    {
        string columnName = ColumnName_Padded(Instance);
        string dataType   = DataType_Padded(Instance);
        query.Append($"    {columnName} {dataType}");


        if ( IsPrimaryKey )
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            query.Append(DbType switch
                         {
                             PostgresType.Guid                                           => " PRIMARY KEY DEFAULT gen_random_uuid()",
                             PostgresType.Long or PostgresType.Int or PostgresType.Short => " PRIMARY KEY GENERATED ALWAYS AS IDENTITY",
                             _                                                           => " PRIMARY KEY"
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


    internal      string ColumnName_Padded( ITableMetaData      table )    => ColumnName.GetPadded(table.MaxLength_ColumnName);
    internal      string KeyValuePair_Padded( ITableMetaData    table )    => KeyValuePair.GetPadded(table.MaxLength_KeyValuePair);
    internal      string VariableName_Padded( ITableMetaData    table )    => VariableName.GetPadded(table.MaxLength_Variables);
    internal      string CreateIndex( ITableMetaData            table )    => $"CREATE INDEX {IndexColumnName_Padded(table)} ON {table.TableName}({ColumnName});";
    internal      string IndexColumnName_Padded( ITableMetaData table )    => Indexed?.Name.GetPadded(table.MaxLength_IndexColumnName) ?? ForeignKey?.Index(ColumnName, table.MaxLength_IndexColumnName) ?? EMPTY;
    internal      string DataType_Padded( ITableMetaData        table )    => DataType.GetPadded(table.MaxLength_DataType);
    public static string GetColumnName( ColumnMetaData          column )   => column.ColumnName;
    public static string GetVariableName( ColumnMetaData        column )   => column.VariableName;
    public static string GetKeyValuePair( ColumnMetaData        column )   => column.KeyValuePair;
    public static bool   IsDbKey( MemberInfo                    property ) => property.GetCustomAttribute<KeyAttribute>() is not null || property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null;


    public NpgsqlParameter ToParameter( object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        if ( parameterName.Contains('.') ) { parameterName = parameterNameCache.GetOrAdd(parameterName, x => x.Split('.')[^1]); }

        NpgsqlParameter parameter = new(parameterName.SqlName(), PostgresDbType, 0, ColumnName)
                                    {
                                        IsNullable    = IsNullable,
                                        SourceVersion = sourceVersion,
                                        Direction     = direction,
                                        Value         = value ?? DBNull.Value
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
}
