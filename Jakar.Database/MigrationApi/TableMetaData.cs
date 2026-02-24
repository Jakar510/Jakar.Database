using ZLinq.Linq;



namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Class)]
public sealed class TableExtrasAttribute : Attribute
{
    public (string First, string Second)? PrimaryKeyPropertiesOverride { get; init; }
    public (string Left, string Right)[]? UniquePropertyPairs          { get; init; }


    public TableExtrasAttribute() { }
    public TableExtrasAttribute( string                               first, string second ) => PrimaryKeyPropertiesOverride = ( first, second );
    public TableExtrasAttribute( params (string Left, string Right)[] uniquePairs ) => UniquePropertyPairs = uniquePairs;
}



// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class TableMetaData<TSelf> : ITableMetaData
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    public static readonly TableMetaData<TSelf> Instance = Create();

    // ReSharper disable once StaticMemberInGenericType
    protected static MigrationRecord?                         _createTableSql;
    public readonly  FrozenDictionary<int, string>            Indexes;
    public readonly  FrozenDictionary<string, ColumnMetaData> Properties;


    public static ITableMetaData                                                                                   Default                     => Instance;
    public        ValueEnumerable<TableMetaDataEnumerator, PropertyColumn>                                         Values                      => AsValueEnumerable();
    public        ValueEnumerable<Select<TableMetaDataEnumerator, PropertyColumn, ColumnMetaData>, ColumnMetaData> Columns                     => Values.Select(static x => x.Column);
    public        int                                                                                              Count                       { get; }
    public        TableExtrasAttribute?                                                                            Extras                      { get; init; }
    public        int                                                                                              ForeignKeyCount             { get; }
    public        PooledArray<ColumnMetaData>                                                                      ForeignKeys                 { [Pure] [MustDisposeResource] get => Columns.Where(static x => x.HasForeignKeyConstraint).ToArrayPool(); }
    FrozenDictionary<int, string> ITableMetaData.                                                                  Indexes                     => Indexes;
    public string                                                                                                  SetLastModifiedFunctionName => field ??= $"{TSelf.TableName}_{nameof(MigrationRecord.SetLastModified).SqlColumnName()}";
    public ref readonly ColumnMetaData this[ string propertyName ] => ref Properties[propertyName];
    public PropertyColumn this[ int index ]
    {
        get
        {
            Guard.IsLessThan((uint)index, (uint)Properties.Keys.Length);
            ref readonly string propertyName = ref Indexes[index];
            return new PropertyColumn(propertyName, Properties[propertyName]);
        }
    }
    public int                                              MaxLength_ColumnName      { get; }
    public int                                              MaxLength_DataType        { get; }
    public int                                              MaxLength_IndexColumnName { get; }
    public int                                              MaxLength_KeyValuePair    { get; }
    public int                                              MaxLength_Variables       { get; }
    FrozenDictionary<string, ColumnMetaData> ITableMetaData.Properties                => Properties;
    public PooledArray<ColumnMetaData>                      SortedColumns             { [MustUseReturnValue] [MustDisposeResource] get => Columns.OrderBy(static x => x.Index).ToArrayPool(); }
    public string                                           TableName                 { [Pure] get => TSelf.TableName; }


    protected internal TableMetaData( FrozenDictionary<string, ColumnMetaData> properties )
    {
        Properties = properties;
        Indexes    = CreateAndValidateIndexes(in properties);

        MaxLength_IndexColumnName = properties.Values.Max(static x => Math.Max(x.Indexed?.Name.Length ?? 0, x.ForeignKey?.Index(x.ColumnName).Length ?? 0));
        MaxLength_KeyValuePair    = properties.Values.Max(static x => x.KeyValuePair.Length);
        MaxLength_Variables       = properties.Values.Max(static x => x.VariableName.Length);
        MaxLength_ColumnName      = properties.Values.Max(static x => x.ColumnName.Length);
        MaxLength_DataType        = properties.Values.Max(static x => x.DataType.Length);
        ForeignKeyCount           = properties.Values.Count(static x => x.ForeignKey?.IsValid is true);
        Count                     = properties.Count;
    }


    public static implicit operator TableMetaData<TSelf>( FrozenDictionary<string, ColumnMetaData> dictionary ) => new(dictionary);


    protected FrozenDictionary<int, string> CreateAndValidateIndexes( in FrozenDictionary<string, ColumnMetaData> properties )
    {
        FrozenDictionary<int, string> indexes = CreateIndexes(in properties);
        if ( indexes.Count != properties.Count ) { throw new InvalidOperationException($"Indexes.Count ({indexes.Count}) must match Properties.Count ({properties.Count})"); }

        for ( int i = 0; i < indexes.Count; i++ )
        {
            if ( !indexes.ContainsKey(i) ) { throw new InvalidOperationException($"Indexes must be sequential. invalid index: {i}"); }
        }

        return indexes;
    }


    /// <summary>
    ///     Sets the order of the columns in the generated SQL will be determined by the order of the properties in the provided dictionary. If not overridden, the columns will be ordered by the size of their data type (using the
    ///     <see
    ///         cref="PostgresTypeComparer"/>
    ///     ), then by whether they are
    ///     <see
    ///         cref="ColumnMetaData.IsFixed"/>
    ///     (a fixed or variable size/length), then by their (
    ///     <see
    ///         cref="ColumnMetaData.Length"/>
    ///     ), and finally by their (
    ///     <see
    ///         cref="ColumnMetaData.ColumnName"/>
    ///     ).
    ///     <para> IMPORTANT: If you override this method, you must ensure that the <see cref="ColumnMetaData.Index"/> of the columns are set correctly in the provided dictionary, as they will be used to generate the SQL for the table. If the indexes are not set correctly, the generated SQL may be incorrect and could lead to runtime errors when executing the SQL against the database. </para>
    /// </summary>
    protected virtual FrozenDictionary<int, string> CreateIndexes( in FrozenDictionary<string, ColumnMetaData> properties )
    {
        Dictionary<int, string> indexes = new(EqualityComparer<int>.Default);
        int                     i       = 0;

        foreach ( ( string propertyName, ColumnMetaData column ) in SortedProperties(in properties) )
        {
            column.Index = i;
            indexes[i++] = propertyName;
        }

        return indexes.ToFrozenDictionary();
    }


    /// <summary>
    ///     Sets the order of the columns in the generated SQL will be determined by the order of the properties in the provided dictionary. If not overridden, the columns will be ordered by the size of their data type (using the
    ///     <see
    ///         cref="PostgresTypeComparer"/>
    ///     ), then by whether they are
    ///     <see
    ///         cref="ColumnMetaData.IsFixed"/>
    ///     (a fixed or variable size/length), then by their (
    ///     <see
    ///         cref="ColumnMetaData.Length"/>
    ///     ), and finally by their (
    ///     <see
    ///         cref="ColumnMetaData.ColumnName"/>
    ///     ).
    /// </summary>
    protected virtual IOrderedEnumerable<KeyValuePair<string, ColumnMetaData>> SortedProperties( in FrozenDictionary<string, ColumnMetaData> properties )
    {
        return properties.OrderBy(static pair => pair.Value.DbType, PostgresTypeComparer.Instance).ThenBy(static pair => pair.Value.IsFixed, InvertedBoolComparer.Instance).ThenBy(static pair => pair.Value.Length, Comparer<DbSizeAttribute?>.Default).ThenBy(static pair => pair.Value.ColumnName, StringComparer.InvariantCultureIgnoreCase);
    }


    public StringBuilder ColumnNames( int indentLevel )
    {
        int           length = Columns.Sum(static x => x.ColumnName.Length + 2) + Properties.Count * indentLevel * 4;
        StringBuilder sb     = new(length);
        int           index  = 0;

        foreach ( string columnName in Columns.Select(x => x.ColumnName) )
        {
            sb.Append(columnName);

            if ( index++ < Count - 1 ) { sb.Append(',').Append('\n').Append(' ', indentLevel * 4); }
        }

        return sb;
    }


    public StringBuilder VariableNames( int indentLevel )
    {
        int           length = Columns.Sum(static x => x.VariableName.Length + 2) + Properties.Count * indentLevel * 4;
        StringBuilder sb     = new(length);
        int           index  = 0;

        foreach ( string columnName in Columns.Select(x => x.VariableName) )
        {
            sb.Append(columnName);

            if ( index++ < Count - 1 ) { sb.Append(',').Append('\n').Append(' ', indentLevel * 4); }
        }

        return sb;
    }


    public StringBuilder KeyValuePairs( int indentLevel )
    {
        int           length = Columns.Sum(static x => x.KeyValuePair.Length + 2) + Properties.Count * indentLevel * 4;
        StringBuilder sb     = new(length);
        int           index  = 0;

        foreach ( string columnName in Columns.Select(x => x.KeyValuePair) )
        {
            sb.Append(columnName);

            if ( index++ < Count - 1 ) { sb.Append(',').Append('\n').Append(' ', indentLevel * 4); }
        }

        return sb;
    }
    public string IndexName( string propertyName ) => Properties[propertyName].IndexColumnName_Padded(this);


    public ValueEnumerable<TableMetaDataEnumerator, PropertyColumn> AsValueEnumerable() => new(GetEnumerator());
    public TableMetaDataEnumerator                                  GetEnumerator()     => new(Properties);


    public ValueEnumerable<Select<SelectWhere<TableMetaDataEnumerator, PropertyColumn, ColumnMetaData>, ColumnMetaData, MigrationRecord>, MigrationRecord> IndexedColumnSql { [Pure] get => Columns.Where(static x => x.IsColumnIndexed).Select(CreateIndex); }


    private static MigrationRecord CreateIndex( ColumnMetaData column ) => CreateIndex(MigrationManager.MigrationID, column);
    private static MigrationRecord CreateIndex( ulong migrationID, ColumnMetaData column )
    {
        Debug.Assert(column.IsColumnIndexed);
        return MigrationRecord.Create<TSelf>(migrationID, $"Create Index for {column.PropertyName} on table {TSelf.TableName}", $"CREATE INDEX {column.IndexColumnName_Padded(Instance)} ON {TSelf.TableName}({column.ColumnName});");
    }


    public MigrationRecord CreateTable( ulong migrationID )
    {
        if ( _createTableSql is not null ) { return _createTableSql; }

        StringBuilder query = new(10240);

        query.Append("CREATE TABLE ");
        query.Append(TSelf.TableName);
        query.Append(" (\n");

        for ( int index = 0; index < Instance.Count; index++ )
        {
            ColumnMetaData column     = Instance[index].Column;
            string         columnName = column.ColumnName_Padded(Instance);
            string         dataType   = column.DataType_Padded(Instance);
            query.Append($"    {columnName} {dataType}");

            if ( column.IsPrimaryKey ) { query.Append(" PRIMARY KEY DEFAULT gen_random_uuid()"); }
            else
            {
                query.Append(column.IsNullable
                                 ? " NULL"
                                 : " NOT NULL");

                if ( column.IsUnique ) { query.Append(" UNIQUE"); }

                if ( column.IsAlwaysIdentity ) { query.Append(" GENERATED ALWAYS AS IDENTITY"); }

                else if ( column.IsDefaultIdentity ) { query.Append(" GENERATED BY DEFAULT AS IDENTITY"); }


                if ( column.Checks?.IsValid is true )
                {
                    query.Append(" CHECK ( ");

                    query.AppendJoin(column.Checks.And
                                         ? AND
                                         : OR,
                                     column.Checks.Constraints);

                    query.Append(" )");
                }

                if ( column.Defaults?.IsValid is true )
                {
                    query.Append(" DEFAULTS ");
                    query.Append(column.Defaults.Value);
                }
            }

            query.Append(',');
            query.Append('\n');
        }

        query.Length -= 2; // Remove the last \n and comma

        if ( Instance.ForeignKeyCount > 0 )
        {
            query.Append(',');
            query.Append('\n');
            using PooledArray<ColumnMetaData> foreignKeys                   = Instance.ForeignKeys;
            int                               maxForeignKeyColumnNameLength = foreignKeys.Span.AsValueEnumerable().Max(static x => x.ColumnName.Length);

            foreach ( ColumnMetaData column in foreignKeys.Span )
            {
                query.Append('\n');
                ForeignKeyAttribute foreignKey = Validate.ThrowIfNull(column.ForeignKey);
                StringBuilder       padding    = new(maxForeignKeyColumnNameLength);
                padding.Append(' ', maxForeignKeyColumnNameLength - column.ColumnName.Length);
                query.Append($"    FOREIGN KEY ({column.ColumnName}){padding} REFERENCES {foreignKey.TableName}({nameof(IUniqueID.ID).SqlColumnName()})");

                if ( !foreignKey.HasModifier )
                {
                    query.Append(',');
                    continue;
                }

                query.Append(' ');
                query.Append(foreignKey.Modifier);
                query.Append(',');
            }

            query.Length--; // Remove the last comma
        }

        TableExtrasAttribute? extras = Instance.Extras;

        if ( extras is not null )
        {
            if ( extras.PrimaryKeyPropertiesOverride.HasValue )
            {
                ( string first, string second ) = extras.PrimaryKeyPropertiesOverride.Value;
                query.Append($",\n    PRIMARY KEY ({first.SqlColumnName()}, {second.SqlColumnName()})");
            }

            if ( extras.UniquePropertyPairs?.Length is not > 0 ) { query.Append('\n'); }
            else
            {
                query.Append(',');
                query.Append('\n');

                for ( int index = 0; index < extras.UniquePropertyPairs.Length; index++ )
                {
                    ( string left, string right ) = extras.UniquePropertyPairs[index];
                    query.Append($"UNIQUE ({left.SqlColumnName()}, {right.SqlColumnName()})");
                    if ( index < extras.UniquePropertyPairs.Length - 1 ) { query.Append(','); }

                    query.Append('\n');
                }
            }
        }

        query.Append(';');
        query.Append('\n');
        query.Append(')');
        return _createTableSql = new MigrationRecord(migrationID, $"Create {TableName} table", TableName) { SQL = query.ToString() };
    }

    public MigrationRecord SetLastModifiedFunction( ulong migrationID ) => MigrationRecord.Create<TSelf>(migrationID,
                                                                                                         "Create SetLastModifiedFunctionName function",
                                                                                                         $"""
                                                                                                          CREATE TRIGGER IF NOT EXISTS {SetLastModifiedFunctionName}
                                                                                                          BEFORE INSERT OR UPDATE ON {TSelf.TableName}
                                                                                                          FOR EACH ROW
                                                                                                          EXECUTE FUNCTION {nameof(MigrationRecord.SetLastModified).SqlColumnName()}();
                                                                                                          """);


    public bool ContainsKey( string propertyName )                                                  => Properties.ContainsKey(propertyName);
    public bool TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value ) => Properties.TryGetValue(propertyName, out value);


    public static TableMetaData<TSelf> Create()
    {
        ref readonly ImmutableArray<PropertyInfo> properties = ref TSelf.ClassProperties;

        if ( properties.Length <= 0 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' does not have any public instance properties that are not marked with the '{nameof(DbIgnoreAttribute)}' attribute."); }

        string[] keys = properties.AsValueEnumerable().Where(ColumnMetaData.IsDbKey).Select(static x => x.Name).ToArray();

        if ( keys.Length != 1 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' should only have one property with the '{typeof(System.ComponentModel.DataAnnotations.KeyAttribute).FullName}' or '{typeof(KeyAttribute).FullName}' attribute. \n\n{keys.ToJson()}"); }


        SortedDictionary<string, ColumnMetaData> dictionary = new(StringComparer.InvariantCultureIgnoreCase);

        foreach ( PropertyInfo property in properties )
        {
            ColumnMetaData data = ColumnMetaData.Create(in property);
            if ( data.DbType is PostgresType.NotSet ) { continue; }

            dictionary[property.Name] = data;
        }

        return new TableMetaData<TSelf>(dictionary.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase)) { Extras = typeof(TSelf).GetCustomAttribute<TableExtrasAttribute>() };
    }
}
