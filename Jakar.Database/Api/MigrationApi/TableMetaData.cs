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
    where TSelf : class, ITableRecord<TSelf>
{
    // ReSharper disable once StaticMemberInGenericType
    public static readonly TableMetaData<TSelf>                     Instance = Create();
    public readonly        FrozenDictionary<int, string>            Indexes;
    public readonly        FrozenDictionary<string, ColumnMetaData> Properties;
    protected              MigrationRecord?                         _createTableSql;


    public static ITableMetaData                                                                                   Default     => Instance;
    public        int                                                                                              ColumnCount => Properties.Count;
    public        ValueEnumerable<Select<TableMetaDataEnumerator, PropertyColumn, ColumnMetaData>, ColumnMetaData> Columns     => Values.Select(static x => x.Column);
    public DataTable DataTable
    {
        [Pure] [MustDisposeResource] get
        {
            DataTable table = new(TSelf.TableName.Value);
            foreach ( ColumnMetaData column in Properties.Values ) { table.Columns.Add(column.DataColumn); }

            return table;
        }
    }
    public TableExtrasAttribute?                                                                                                                                                   Extras                 { get; init; }
    public int                                                                                                                                                                     ForeignKeyCount        { get; }
    public ValueEnumerable<SelectWhere<TableMetaDataEnumerator, PropertyColumn, ColumnMetaData>, ColumnMetaData>                                                                   ForeignKeys            { [Pure] get => Columns.Where(static x => x.HasForeignKeyConstraint); }
    public ValueEnumerable<Select<SelectWhere<TableMetaDataEnumerator, PropertyColumn, ColumnMetaData>, ColumnMetaData, Func<long, MigrationRecord>>, Func<long, MigrationRecord>> IndexedColumns         { [Pure] [MustUseReturnValue] get => Columns.Where(static x => x.IsColumnIndexed).Select(CreateIndex); }
    FrozenDictionary<int, string> ITableMetaData.                                                                                                                                  Indexes                => Indexes;
    public string                                                                                                                                                                  PrimaryKeyPropertyName => field ??= Columns.First(static x => x.IsPrimaryKey).PropertyName;
    public ref readonly ColumnMetaData this[ string propertyName ] => ref Properties[propertyName];
    public ref readonly string this[ string         propertyName, DatabaseType type ] => ref Properties[propertyName][type];

    // ---- Jakar.SqlBuilder.ISqlCommandTable (what CommandParameters binds against) ----
    public int                 KeyValuePairsLength                                 { get; }
    ISqlCommandColumn ISqlCommandTable.GetColumn( string        propertyName )  => Properties[propertyName];
    public string              GetParameterName( string         parameterName ) => parameterName.Parameterize();

    // ---- Jakar.SqlBuilder lean view ----
    public IReadOnlyList<ISqlColumn> SqlColumns => field ??= Properties.Values.OrderBy(static x => x.Index).ToArray();

    /// <summary> The <see cref="TableExtrasAttribute"/> composite primary key, as column names (empty when the columns carry their own keys). </summary>
    public IReadOnlyList<string> SqlPrimaryKey => field ??= Extras?.PrimaryKeyPropertiesOverride is ( string first, string second )
                                                                 ? [first.SqlName(), second.SqlName()]
                                                                 : [];

    /// <summary> The <see cref="TableExtrasAttribute"/> unique column pairs, as column names. </summary>
    public IReadOnlyList<IReadOnlyList<string>> SqlUniqueKeys => field ??= Extras?.UniquePropertyPairs is { Length: > 0 } pairs
                                                                               ? Array.ConvertAll(pairs, static pair => (IReadOnlyList<string>)[pair.Left.SqlName(), pair.Right.SqlName()])
                                                                               : [];
    public bool TrySqlColumn( string propertyName, out ISqlColumn? column )
    {
        if ( Properties.TryGetValue(propertyName, out ColumnMetaData? found) )
        {
            column = found;
            return true;
        }

        column = null;
        return false;
    }
    public PropertyColumn this[ int index ]
    {
        get
        {
            Guard.IsLessThan((uint)index, (uint)Properties.Keys.Length);
            ref readonly string propertyName = ref Indexes[index];
            return new PropertyColumn(propertyName, Properties[propertyName]);
        }
    }
    public int                                                                                                                                         MaxLengthColumnName        { get; }
    public int                                                                                                                                         MaxLength_DataType          { get; }
    public int                                                                                                                                         MaxLength_IndexColumnName   { get; }
    public int                                                                                                                                         MaxLength_KeyValuePair      { get; }
    public int                                                                                                                                         MaxLength_Variables         { get; }
    FrozenDictionary<string, ColumnMetaData> ITableMetaData.                                                                                           Properties                  => Properties;
    public              string                                                                                                                         SetLastModifiedFunctionName => field ??= $"{TSelf.TableName}_{MigrationRecord.SetLastModifiedName}";
    public              ValueEnumerable<OrderBy<Select<TableMetaDataEnumerator, PropertyColumn, ColumnMetaData>, ColumnMetaData, int>, ColumnMetaData> SortedColumns               { [Pure] [MustUseReturnValue] get => Columns.OrderBy(static x => x.Index); }
    public ref readonly SqlName                                                                                                                        TableName                   { [Pure] get => ref TSelf.TableName; }
    public              ValueEnumerable<TableMetaDataEnumerator, PropertyColumn>                                                                       Values                      => AsValueEnumerable();
    public              ParameterSorter                                                                                                                Sorter                      { get; }


    protected internal TableMetaData( FrozenDictionary<string, ColumnMetaData> properties )
    {
        Properties                = properties;
        Indexes                   = CreateAndValidateIndexes(in properties);
        ForeignKeyCount           = properties.Values.Count(static x => x.ForeignKey?.IsValid is true);
        MaxLength_IndexColumnName = properties.Values.Max(static x => Math.Max(x.Indexed?.Name.Length ?? 0, x.ForeignKey?.Index(x.ColumnName).Length ?? 0));
        MaxLength_KeyValuePair    = properties.Values.Max(static x => x.KeyValuePair.Length);
        KeyValuePairsLength       = properties.Values.Sum(static x => x.KeyValuePair.Length);
        MaxLength_Variables       = properties.Values.Max(static x => x.VariableName.Length);
        MaxLengthColumnName      = properties.Values.Max(static x => x.ColumnName.Length);
        MaxLength_DataType        = properties.Values.SelectMany(static x => x.DataTypes.Values).Max(static x => x.Length);
        Sorter                    = new ParameterSorter(this);
    }


    public static implicit operator TableMetaData<TSelf>( FrozenDictionary<string, ColumnMetaData> dictionary ) => new(dictionary);
    public                          void Dispose()                                                              => DataTable.Dispose();


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

        foreach ( ( string propertyName, ColumnMetaData column ) in OrderedColumns(in properties) )
        {
            column.Index = i;
            indexes[i++] = propertyName;
        }

        return indexes.ToFrozenDictionary();
    }


    /// <summary>
    ///     Returns the columns in the build-time, size-packed order emitted by the source generator (<see cref="GeneratedColumnOrderAttribute"/>). When the attribute is absent or does not cover every column, the remaining columns fall back to <see cref="SortedProperties"/>, so ordering is always complete and correct even if the generated order is missing.
    /// </summary>
    private IEnumerable<KeyValuePair<string, ColumnMetaData>> OrderedColumns( in FrozenDictionary<string, ColumnMetaData> properties )
    {
        string[]? order = typeof(TSelf).GetCustomAttribute<GeneratedColumnOrderAttribute>()?.PropertyNames;
        if ( order is null ) { return SortedProperties(in properties); }

        List<KeyValuePair<string, ColumnMetaData>> result = new(properties.Count);
        HashSet<string>                            seen   = new(StringComparer.Ordinal);

        foreach ( string propertyName in order )
        {
            if ( properties.TryGetValue(propertyName, out ColumnMetaData? column) && seen.Add(propertyName) ) { result.Add(new KeyValuePair<string, ColumnMetaData>(propertyName, column)); }
        }

        if ( seen.Count < properties.Count )
        {
            foreach ( KeyValuePair<string, ColumnMetaData> pair in SortedProperties(in properties) )
            {
                if ( seen.Add(pair.Key) ) { result.Add(pair); }
            }
        }

        return result;
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


    /// <summary> The column list, one per line, at <paramref name="indentLevel"/>: a raw SQL fragment for interpolated commands. </summary>
    public ColumnNames ColumnNames( int indentLevel ) => new(this, indentLevel);
    public void ColumnNames( ref ValueStringBuilder builder, ref int indentLevel )
    {
        int index = 0;

        foreach ( string columnName in SortedColumns.Select(static x => x.ColumnName) )
        {
            builder.Append(' ', indentLevel * 4).Append(columnName);
            if ( index++ < ColumnCount - 1 ) { builder.Append(",\n"); }
        }
    }


    public string IndexName( string propertyName ) => Properties[propertyName].IndexColumnName_Padded(this);


    public ValueEnumerable<TableMetaDataEnumerator, PropertyColumn> AsValueEnumerable() => new(GetEnumerator());
    public TableMetaDataEnumerator                                  GetEnumerator()     => new(this);


    private static Func<long, MigrationRecord> CreateIndex( ColumnMetaData column ) => migrationID => CreateIndex(migrationID, column);
    private static MigrationRecord CreateIndex( long migrationID, ColumnMetaData column )
    {
        Debug.Assert(column.IsColumnIndexed);
        DatabaseType databaseType = Validate.ThrowIfNull(Database.Current).DatabaseType;
        return MigrationRecord.Create<TSelf>(migrationID, $"Create Index for {column.PropertyName} on table {TSelf.TableName}", column.CreateIndex(Instance, databaseType), column.DropIndex(Instance, databaseType));
    }


    [Pure] public SqlCommand CreateTableSql() => SqlCommand.Create<TSelf>(CreateTableSql(Validate.ThrowIfNull(Database.Current).DatabaseType));
    /// <summary> <c>CREATE TABLE</c> for <paramref name="type"/>, written by Jakar.SqlBuilder from this table's metadata (<c>SqlBuilder.CreateTable&lt;TSelf&gt;()</c>). </summary>
    [Pure] public string CreateTableSql( in DatabaseType type ) => Jakar.SqlBuilder.SqlBuilder.For(type.ToSqlDialectKind()).CreateTable<TSelf>().BuildSql();

    [Pure] public SqlCommand DropTableSql() => SqlCommand.Create<TSelf>(DropTableSql(Validate.ThrowIfNull(Database.Current).DatabaseType));

    /// <summary> <c>DROP TABLE [IF EXISTS] ... [CASCADE]</c>, with each clause only where <paramref name="type"/> supports it. </summary>
    [Pure] public string DropTableSql( in DatabaseType type )
    {
        SqlDialectKind dialect = type.ToSqlDialectKind();
        return Jakar.SqlBuilder.SqlBuilder.For(dialect).DropTable(TSelf.SqlTableName, dialect.SupportsDropIfExists(), dialect.SupportsDropCascade()).BuildSql();
    }
    public MigrationRecord CreateTable( long migrationID ) => _createTableSql ??= new MigrationRecord
                                                                                  {
                                                                                      MigrationID = migrationID,
                                                                                      Description = $"Create {TableName} table",
                                                                                      ReferenceID = TableName,
                                                                                      UpSQL       = CreateTableSql(),
                                                                                      DownSQL     = DropTableSql()
                                                                                  };


    public MigrationRecord SetLastModifiedFunction( long migrationID ) => SetLastModifiedFunction(migrationID, Validate.ThrowIfNull(Database.Current).DatabaseType);

    /// <summary> The trigger that keeps <c>last_modified</c> current on insert and update, in <paramref name="type"/>'s form (<see cref="LastModifiedTrigger"/>), and its rollback. </summary>
    /// <exception cref="InvalidOperationException"> The table has no <c>LastModified</c> column. </exception>
    public MigrationRecord SetLastModifiedFunction( long migrationID, DatabaseType type ) => MigrationRecord.Create<TSelf>(migrationID,
                                                                                                                          $"Create {SetLastModifiedFunctionName} trigger",
                                                                                                                          SetLastModifiedSql(type),
                                                                                                                          LastModifiedTrigger.Drop(type, SetLastModifiedFunctionName, TSelf.SqlTableName));

    /// <exception cref="InvalidOperationException"> The table has no <c>LastModified</c> column. </exception>
    [Pure] public string SetLastModifiedSql( DatabaseType type )
    {
        if ( !Properties.TryGetValue(nameof(ILastModified.LastModified), out ColumnMetaData? column) ) { throw new InvalidOperationException($"{TSelf.SqlTableName} has no {nameof(ILastModified.LastModified)} column to maintain."); }

        return LastModifiedTrigger.Create(type, SetLastModifiedFunctionName, TSelf.SqlTableName, column.ColumnName, KeyColumns());
    }

    /// <summary> The key's column names: the <see cref="TableExtrasAttribute"/> composite key, or the primary-key column. </summary>
    private IReadOnlyList<string> KeyColumns()
    {
        if ( SqlPrimaryKey.Count > 0 ) { return SqlPrimaryKey; }

        List<string> keys = [];
        foreach ( ColumnMetaData column in Properties.Values )
        {
            if ( column.IsPrimaryKey ) { keys.Add(column.ColumnName); }
        }

        return keys;
    }


    public bool ContainsKey( string propertyName )                                                  => Properties.ContainsKey(propertyName);
    public bool TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value ) => Properties.TryGetValue(propertyName, out value);


    public static TableMetaData<TSelf> Create()
    {
        ref readonly ImmutableArray<PropertyInfo> properties = ref TSelf.ClassProperties;
        if ( properties.Length <= 0 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' does not have any public instance properties that are not marked with the '{nameof(DbIgnoreAttribute)}' attribute."); }

        using PooledArray<string> keys = properties.AsValueEnumerable().Where(ColumnMetaData.IsDbKey).Select(static x => x.Name).ToArrayPool();
        if ( keys.Size != 1 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' should only have one property with the '{typeof(System.ComponentModel.DataAnnotations.KeyAttribute).FullName}' or '{typeof(KeyAttribute).FullName}' attribute. \n\n{Jakar.Extensions.Json.Serialize(keys.Array.AsSpan(0, keys.Size))}"); }


        SortedDictionary<string, ColumnMetaData> dictionary = new(StringComparer.InvariantCultureIgnoreCase);

        foreach ( PropertyInfo property in properties )
        {
            ColumnMetaData data = ColumnMetaData.Create(in property);
            if ( data.DbType is DbColumnType.NotSet ) { continue; }

            dictionary[property.Name] = data;
        }

        return new TableMetaData<TSelf>(dictionary.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase)) { Extras = typeof(TSelf).GetCustomAttribute<TableExtrasAttribute>() };
    }
}
