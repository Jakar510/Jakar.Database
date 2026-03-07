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
    public string                                                                                                  SetLastModifiedFunctionName => field ??= $"{TSelf.TableName}_{MigrationRecord.SetLastModifiedName}";
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
    public PooledArray<ColumnMetaData>                      SortedColumns             { [Pure] [MustUseReturnValue] [MustDisposeResource] get => Columns.OrderBy(static x => x.Index).ToArrayPool(); }
    public string                                           TableName                 { [Pure] get => TSelf.TableName; }
    public PooledArray<Func<long, MigrationRecord>>         IndexedColumns            { [Pure] [MustUseReturnValue] [MustDisposeResource] get => Columns.Where(static x => x.IsColumnIndexed).Select(CreateIndex).ToArrayPool(); }


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
        StringBuilder sb = new();
        ColumnNames(sb, ref indentLevel);
        return sb;
    }
    public void ColumnNames( StringBuilder sb, ref int indentLevel )
    {
        int length = ( MaxLength_ColumnName + 2 ) * Count + Count * 4 * indentLevel;
        sb.EnsureCapacity(length);

        int index = 0;

        foreach ( string columnName in Columns.Select(static x => x.ColumnName) )
        {
            sb.Append(' ', indentLevel * 4).Append(columnName);
            if ( index++ < Count - 1 ) { sb.Append(",\n"); }
        }
    }


    public void VariableNames( StringBuilder sb, ref int indentLevel )
    {
        int length = ( MaxLength_Variables + 2 ) * Count + Count * 4 * indentLevel;
        sb.EnsureCapacity(length);

        int index = 0;

        foreach ( string columnName in Columns.Select(static x => x.VariableName) )
        {
            sb.Append(' ', indentLevel * 4).Append(columnName);
            if ( index++ < Count - 1 ) { sb.Append(",\n"); }
        }
    }


    public void KeyValuePairs( StringBuilder sb, ref int indentLevel )
    {
        int length = ( MaxLength_KeyValuePair + 2 ) * Count + Count * 4 * indentLevel;
        sb.EnsureCapacity(length);

        int index = 0;

        foreach ( string columnName in Columns.Select(static x => x.KeyValuePair) )
        {
            sb.Append(' ', indentLevel * 4).Append(columnName);
            if ( index++ < Count - 1 ) { sb.Append(",\n"); }
        }
    }


    public string IndexName( string propertyName ) => Properties[propertyName].IndexColumnName_Padded(this);


    public ValueEnumerable<TableMetaDataEnumerator, PropertyColumn> AsValueEnumerable() => new(GetEnumerator());
    public TableMetaDataEnumerator                                  GetEnumerator()     => new(Properties);


    private static Func<long, MigrationRecord> CreateIndex( ColumnMetaData column ) => migrationID => CreateIndex(migrationID, column);
    private static MigrationRecord CreateIndex( long migrationID, ColumnMetaData column )
    {
        Debug.Assert(column.IsColumnIndexed);
        return MigrationRecord.Create<TSelf>(migrationID, $"Create Index for {column.PropertyName} on table {TSelf.TableName}", column.CreateIndex(Instance));
    }


    [Pure] public string CreateTableSql()
    {
        StringBuilder query = new(10240);

        query.Append("CREATE TABLE ");
        query.Append(TSelf.TableName);
        query.Append(" (\n");

        for ( int index = 0; index < Instance.Count; index++ )
        {
            Instance[index].Column.AddData(query, this);

            query.Append(',');
            query.Append('\n');
        }

        query.Length -= 2; // Remove the last \n and comma

        tryAddForeignKeys(query);

        tryAddExtras(query);

        query.Append('\n');
        query.Append(')');
        query.Append(';');

        return query.ToString();

        static void tryAddExtras( StringBuilder query )
        {
            TableExtrasAttribute? extras = Instance.Extras;
            if ( extras is null ) { return; }

            if ( extras.PrimaryKeyPropertiesOverride.HasValue )
            {
                ( string first, string second ) = extras.PrimaryKeyPropertiesOverride.Value;
                query.Append($",\n    PRIMARY KEY ({first.SqlName()}, {second.SqlName()})");
            }

            if ( extras.UniquePropertyPairs?.Length is not > 0 ) { query.Append('\n'); }
            else
            {
                query.Append(',');
                query.Append('\n');

                for ( int index = 0; index < extras.UniquePropertyPairs.Length; index++ )
                {
                    ( string left, string right ) = extras.UniquePropertyPairs[index];
                    query.Append($"UNIQUE ({left.SqlName()}, {right.SqlName()})");
                    if ( index < extras.UniquePropertyPairs.Length - 1 ) { query.Append(','); }

                    query.Append('\n');
                }
            }
        }

        static void tryAddForeignKeys( StringBuilder query )
        {
            if ( Instance.ForeignKeyCount <= 0 ) { return; }

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
                query.Append($"    FOREIGN KEY ({column.ColumnName}){padding} REFERENCES {foreignKey.TableName}({nameof(IUniqueID.ID).SqlName()})");

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
    }
    public MigrationRecord CreateTable( long migrationID ) => _createTableSql ??= new MigrationRecord
                                                                                  {
                                                                                      MigrationID = migrationID,
                                                                                      Description = $"Create {TableName} table",
                                                                                      ReferenceID = TableName,
                                                                                      SQL         = CreateTableSql()
                                                                                  };


    public MigrationRecord SetLastModifiedFunction( long migrationID ) => MigrationRecord.Create<TSelf>(migrationID,
                                                                                                        "Create SetLastModifiedFunctionName function",
                                                                                                        $"""
                                                                                                         CREATE TRIGGER IF NOT EXISTS {SetLastModifiedFunctionName}
                                                                                                         BEFORE INSERT OR UPDATE ON {TSelf.TableName}
                                                                                                         FOR EACH ROW
                                                                                                         EXECUTE FUNCTION {MigrationRecord.SetLastModifiedName}();
                                                                                                         """);


    public bool ContainsKey( string propertyName )                                                  => Properties.ContainsKey(propertyName);
    public bool TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value ) => Properties.TryGetValue(propertyName, out value);


    public static TableMetaData<TSelf> Create()
    {
        ref readonly ImmutableArray<PropertyInfo> properties = ref TSelf.ClassProperties;
        if ( properties.Length <= 0 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' does not have any public instance properties that are not marked with the '{nameof(DbIgnoreAttribute)}' attribute."); }

        using PooledArray<string> keys = properties.AsValueEnumerable().Where(ColumnMetaData.IsDbKey).Select(static x => x.Name).ToArrayPool();
        if ( keys.Size != 1 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' should only have one property with the '{typeof(System.ComponentModel.DataAnnotations.KeyAttribute).FullName}' or '{typeof(KeyAttribute).FullName}' attribute. \n\n{keys.Array.ToJson()}"); }


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
