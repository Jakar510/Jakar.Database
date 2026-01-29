namespace Jakar.Database;


public sealed class ColumnMetaData
{
    public static readonly ColumnMetaData AdditionalData = new(nameof(IJsonModel.AdditionalData), PostgresType.Json, ColumnOptions.Nullable);
    public static readonly ColumnMetaData CreatedBy      = new(nameof(ICreatedBy.CreatedBy), PostgresType.Guid, ColumnOptions.Nullable, UserRecord.TABLE_NAME);
    public static readonly ColumnMetaData DateCreated    = new(nameof(ICreatedBy.DateCreated), PostgresType.DateTimeOffset, ColumnOptions.Indexed);
    public static readonly ColumnMetaData ID             = new(nameof(ICreatedBy.ID), PostgresType.Guid, ColumnOptions.PrimaryKey                      | ColumnOptions.AlwaysIdentity | ColumnOptions.Unique);
    public static readonly ColumnMetaData LastModified   = new(nameof(ILastModified.LastModified), PostgresType.DateTimeOffset, ColumnOptions.Nullable | ColumnOptions.Indexed);


    public readonly bool                IsNullable;
    public readonly bool                IsPrimaryKey;
    public readonly ColumnCheckMetaData Checks;
    public readonly ColumnOptions       Options;
    public readonly PostgresType        DbType;
    public readonly SizeInfo            Length;
    public readonly string              ColumnName;
    public readonly string              KeyValuePair;
    public readonly string              SpacedName;
    public readonly string              PropertyName;
    public readonly string              VariableName;
    public readonly string?             ForeignKeyName;
    public readonly string?             IndexColumnName;
    public readonly string              DataType;


    public bool IsForeignKey { [MemberNotNullWhen(true, nameof(IndexColumnName))] get => !string.IsNullOrWhiteSpace(IndexColumnName); }


    public ColumnMetaData( in string propertyName, in PostgresType dbType, in ColumnOptions options = ColumnOptions.None, in string? foreignKeyName = null, in string? indexColumnName = null, in SizeInfo length = default, in ColumnCheckMetaData? checks = null, in string? variableName = null, in string? spacedName = null, in string? keyValuePair = null )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        string columnName = Validate.ThrowIfNull(propertyName.SqlColumnName());
        IsNullable      = ( options & ColumnOptions.Nullable )   != 0;
        IsPrimaryKey    = ( options & ColumnOptions.PrimaryKey ) != 0;
        Checks          = checks ?? ColumnCheckMetaData.Default;
        Options         = options;
        DbType          = dbType;
        Length          = length;
        PropertyName    = propertyName;
        ColumnName      = columnName;
        KeyValuePair    = keyValuePair ?? $" {columnName} = @{columnName} ";
        SpacedName      = spacedName   ?? $" {columnName} ";
        VariableName    = variableName ?? $" @{columnName} ";
        ForeignKeyName  = foreignKeyName?.SqlColumnName();
        IndexColumnName = indexColumnName?.SqlColumnName();
        DataType        = dbType.GetPostgresDataType(in length, in options);
    }


    public static  string GetColumnName( ColumnMetaData   x )        => x.ColumnName;
    public static  string GetVariableName( ColumnMetaData x )        => x.VariableName;
    public static  string GetKeyValuePair( ColumnMetaData x )        => x.KeyValuePair;
    private static bool   IsDbKey( MemberInfo             property ) => property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null;


    public static FrozenDictionary<string, ColumnMetaData> Create<TSelf>()
        where TSelf : ITableRecord<TSelf>
    {
        const BindingFlags ATTRIBUTES = BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty;

        PropertyInfo[] properties = typeof(TSelf).GetProperties(ATTRIBUTES)
                                                 .Where(static x => !x.HasAttribute<DbIgnoreAttribute>())
                                                 .ToArray();

        if ( properties.Length <= 0 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' does not have any public instance properties that are not marked with the '{nameof(DbIgnoreAttribute)}' attribute."); }

        if ( !properties.Any(IsDbKey) ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' does not have a property with the '{nameof(System.ComponentModel.DataAnnotations.KeyAttribute)}' attribute."); }

        return properties.ToFrozenDictionary(static x => x.Name, Create);
    }
    public static ColumnMetaData Create( PropertyInfo property ) => Create(property, property.GetCustomAttribute<ColumnMetaDataAttribute>(), property.GetCustomAttribute<MaxLengthAttribute>(), property.GetCustomAttribute<LengthAttribute>());
    internal static ColumnMetaData Create( PropertyInfo property, ColumnMetaDataAttribute? attribute, MaxLengthAttribute? stringLength, LengthAttribute? maxLength )
    {
        attribute ??= ColumnMetaDataAttribute.Empty;
        attribute.Deconstruct(out string? columnName, out ColumnOptions options, out SizeInfo length, out PostgresType? postgresType, out string? foreignKeyName, out string? indexColumnName, out string? variableName, out string? keyValuePair, out string? spacedName, out ColumnCheckMetaData checks);
        string propertyName = property.Name;
        columnName ??= propertyName.SqlColumnName();
        PostgresType dbType = property.PropertyType.GetPostgresType(ref options, ref length);
        spacedName = attribute.Name ?? columnName;

        if ( postgresType.HasValue ) { dbType = postgresType.Value; }

        if ( IsDbKey(property) ) { options |= ColumnOptions.PrimaryKey; }

        return new ColumnMetaData(propertyName, dbType, options, foreignKeyName, indexColumnName, length, checks, variableName, spacedName, keyValuePair);
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
