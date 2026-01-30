namespace Jakar.Database;


public sealed class ColumnMetaData
{
    public static readonly ColumnMetaData AdditionalData = new(nameof(IJsonModel.AdditionalData), PostgresType.Json, ColumnOptions.Nullable);
    public static readonly ColumnMetaData CreatedBy      = new(nameof(ICreatedBy.CreatedBy), PostgresType.Guid, ColumnOptions.Nullable, UserRecord.TABLE_NAME);
    public static readonly ColumnMetaData DateCreated    = new(nameof(IDateCreated.DateCreated), PostgresType.DateTimeOffset, ColumnOptions.Indexed);
    public static readonly ColumnMetaData ID             = new(nameof(IUniqueID.ID), PostgresType.Guid, ColumnOptions.PrimaryKey                       | ColumnOptions.AlwaysIdentity);
    public static readonly ColumnMetaData LastModified   = new(nameof(ILastModified.LastModified), PostgresType.DateTimeOffset, ColumnOptions.Nullable | ColumnOptions.Indexed);


    public readonly bool                 IsNullable;
    public readonly bool                 IsPrimaryKey;
    public readonly ColumnCheckMetaData? Checks;
    public readonly ColumnOptions        Options;
    public readonly PostgresType         DbType;
    public readonly SizeInfo?            Length;
    public readonly string               ColumnName;
    public readonly string               KeyValuePair;
    public readonly string               SpacedName;
    public readonly string               PropertyName;
    public readonly string               VariableName;
    public readonly string?              ForeignKeyName;
    public readonly string?              IndexColumnName;
    public readonly string               DataType;


    public bool IsForeignKey { [MemberNotNullWhen(true, nameof(IndexColumnName))] get => !string.IsNullOrWhiteSpace(IndexColumnName); }


    public ColumnMetaData( in string propertyName, in PostgresType dbType, in ColumnOptions options = ColumnOptions.None, in string? foreignKeyName = null, in SizeInfo? length = null, in ColumnCheckMetaData? checks = null )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        if ( ( options & ColumnOptions.ForeignKey ) != 0 && string.IsNullOrWhiteSpace(foreignKeyName) ) { throw new ArgumentException($"Column '{propertyName}' has a {nameof(ColumnOptions.ForeignKey)} flag but {nameof(foreignKeyName)} is invalid.", nameof(foreignKeyName)); }

        if ( ( options & ColumnOptions.ForeignKey ) != 0 && ( options & ColumnOptions.Indexed ) != 0 ) { throw new ArgumentException($"Column '{propertyName}' cannot be both {nameof(ColumnOptions.Indexed)} and a {nameof(ColumnOptions.ForeignKey)}. {nameof(ColumnOptions.ForeignKey)} columns are automatically indexed.", nameof(options)); }


        string columnName = Validate.ThrowIfNull(propertyName.SqlColumnName());
        IsNullable     = ( options & ColumnOptions.Nullable )   != 0;
        IsPrimaryKey   = ( options & ColumnOptions.PrimaryKey ) != 0;
        Checks         = checks;
        Options        = options;
        DbType         = dbType;
        Length         = length;
        PropertyName   = propertyName;
        ColumnName     = columnName;
        KeyValuePair   = $" {columnName} = @{columnName} ";
        SpacedName     = $" {columnName} ";
        VariableName   = $" @{columnName} ";
        ForeignKeyName = foreignKeyName?.SqlColumnName();
        DataType       = dbType.GetPostgresDataType(in length, in options);

        IndexColumnName = ( options & ColumnOptions.Indexed ) != 0
                              ? $"{columnName}_index"
                              : null;
    }


    public static string GetColumnName( ColumnMetaData   column )   => column.ColumnName;
    public static string GetVariableName( ColumnMetaData column )   => column.VariableName;
    public static string GetKeyValuePair( ColumnMetaData column )   => column.KeyValuePair;
    public static bool   IsDbKey( MemberInfo             property ) => property.GetCustomAttribute<KeyAttribute>() is not null || property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null;


    public static ColumnMetaData Create( PropertyInfo property ) => Create(property, property.GetCustomAttribute<ColumnMetaDataAttribute>(), property.GetCustomAttribute<MaxLengthAttribute>(), property.GetCustomAttribute<LengthAttribute>());
    internal static ColumnMetaData Create( PropertyInfo property, ColumnMetaDataAttribute? attribute, MaxLengthAttribute? stringLength, LengthAttribute? maxLength )
    {
        attribute ??= ColumnMetaDataAttribute.Empty;
        attribute.Deconstruct(out ColumnOptions options, out string? foreignKeyName, out SizeInfo length, out ColumnCheckMetaData checks);
        PostgresType dbType = property.PropertyType.GetPostgresType(ref options, ref length);

        if ( IsDbKey(property) ) { options |= ColumnOptions.PrimaryKey; }

        // length = new SizeInfo(Math.Max(maxLength?.MaximumLength ?? -1, stringLength?.Length ?? -1));
        // checks = new ColumnCheckMetaData(true, $"CHECK (char_length({columnName}) BETWEEN {maxLength?.MinimumLength} AND {maxLength?.MaximumLength})");

        return new ColumnMetaData(property.Name, dbType, options, foreignKeyName, length, checks);
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
