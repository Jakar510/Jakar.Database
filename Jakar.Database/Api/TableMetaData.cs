using ZLinq.Linq;



namespace Jakar.Database;


[SuppressMessage("ReSharper", "InconsistentNaming")]
public interface ITableMetaData
{
    public abstract static ITableMetaData                                                                             Default     { get; }
    public                 ValueEnumerable<FromImmutableArray<ColumnMetaData>, ColumnMetaData>                        Columns     { get; }
    public                 int                                                                                        Count       { get; }
    public                 ValueEnumerable<Where<FromImmutableArray<ColumnMetaData>, ColumnMetaData>, ColumnMetaData> ForeignKeys { get; }
    public                 FrozenDictionary<int, string>                                                              Indexes     { get; }
    public ref readonly ColumnMetaData this[ string propertyName ] { get; }
    public PropertyColumn this[ int                 index ] { get; }
    public int                                      MaxLength_ColumnName      { get; }
    public int                                      MaxLength_DataType        { get; }
    public int                                      MaxLength_IndexColumnName { get; }
    public int                                      MaxLength_KeyValuePair    { get; }
    public int                                      MaxLength_Variables       { get; }
    public FrozenDictionary<string, ColumnMetaData> Properties                { get; }
    public PooledArray<ColumnMetaData>              SortedColumns             { get; }
    public string                                   TableName                 { [Pure] get; }


    public StringBuilder ColumnNames( int   indentLevel );
    public StringBuilder VariableNames( int indentLevel );
    public StringBuilder KeyValuePairs( int indentLevel );


    public string                  CreateTable();
    public TableMetaDataEnumerator GetEnumerator();
    public bool                    ContainsKey( string propertyName );
    public bool                    TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value );
}



public ref struct TableMetaDataEnumerator
{
    private readonly ITableMetaData __table;
    private          int            __index;


    internal TableMetaDataEnumerator( ITableMetaData table )
    {
        __table = table;
        __index = -1;
    }

    public readonly PropertyColumn Current => __table[__index];

    public bool MoveNext()
    {
        __index++;
        if ( (uint)__index < (uint)__table.Properties.Count ) { return true; }

        __index = __table.Properties.Count;
        return false;
    }

    public TableMetaDataEnumerator GetEnumerator() => this;
    public void                    Reset()         => __index = -1;
    public void                    Dispose()       { }
}



public readonly ref struct PropertyColumn( ref readonly string propertyName, ref readonly ColumnMetaData column )
{
    public readonly ref readonly    string         PropertyName = ref propertyName;
    public readonly ref readonly    ColumnMetaData Column       = ref column;
    public static implicit operator ColumnMetaData( PropertyColumn value ) => value.Column;
    public void Deconstruct( out string propertyName, out ColumnMetaData column )
    {
        column       = Column;
        propertyName = PropertyName;
    }
}



[AttributeUsage(AttributeTargets.Class)]
public sealed class TableExtrasAttribute() : Attribute
{
    public (string First, string Second)?              PrimaryKeyOverride { get; init; }
    public ImmutableArray<(string Left, string Right)> UniquePairs        { get; init; }
}



public class TableMetaData<TSelf> : ITableMetaData
    where TSelf : class, ITableRecord<TSelf>
{
    public static readonly TableMetaData<TSelf> Instance = Create();
    protected static       string?              _createTableSql;

    // ReSharper disable once StaticMemberInGenericType


    public readonly FrozenDictionary<int, string>            Indexes;
    public readonly FrozenDictionary<string, ColumnMetaData> Properties;


    public static ITableMetaData                                                      Default => Instance;
    public        ValueEnumerable<FromImmutableArray<ColumnMetaData>, ColumnMetaData> Columns => Properties.Values.AsValueEnumerable();


    public int                   Count           { get; }
    public TableExtrasAttribute? Extras          { get; init; }
    public int                   ForeignKeyCount { get; }

    public ValueEnumerable<Where<FromImmutableArray<ColumnMetaData>, ColumnMetaData>, ColumnMetaData> ForeignKeys => Columns.Where(static x => x.IsForeignKey);

    FrozenDictionary<int, string> ITableMetaData.Indexes => Indexes;
    public ref readonly ColumnMetaData this[ string propertyName ] => ref Properties[propertyName];
    public PropertyColumn this[ int index ]
    {
        get
        {
            Guard.IsLessThan((uint)index, (uint)Properties.Keys.Length);
            ref readonly string         propertyName = ref Indexes[index];
            ref readonly ColumnMetaData column       = ref Properties[propertyName];
            return new PropertyColumn(in propertyName, in column);
        }
    }
    public int                                              MaxLength_ColumnName      { get; }
    public int                                              MaxLength_DataType        { get; }
    public int                                              MaxLength_IndexColumnName { get; }
    public int                                              MaxLength_KeyValuePair    { get; }
    public int                                              MaxLength_Variables       { get; }
    FrozenDictionary<string, ColumnMetaData> ITableMetaData.Properties                => Properties;

    public PooledArray<ColumnMetaData> SortedColumns
    {
        [MustUseReturnValue] [MustDisposeResource] get => Columns.OrderBy(static x => x.Index)
                                                                 .ToArrayPool();
    }
    public string TableName { [Pure] get => TSelf.TableName; }


    protected internal TableMetaData( FrozenDictionary<string, ColumnMetaData> properties )
    {
        Properties                = properties;
        Indexes                   = CreateAndValidateIndexes(in properties);
        MaxLength_IndexColumnName = properties.Values.Max(static x => x.IndexColumnName?.Length ?? 0);
        MaxLength_KeyValuePair    = properties.Values.Max(static x => x.KeyValuePair.Length);
        MaxLength_Variables       = properties.Values.Max(static x => x.VariableName.Length);
        MaxLength_ColumnName      = properties.Values.Max(static x => x.ColumnName.Length);
        MaxLength_DataType        = properties.Values.Max(static x => x.DataType.Length);
        ForeignKeyCount           = properties.Values.Count(static x => x.IsForeignKey);
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
        return properties.OrderBy(static pair => pair.Value.DbType, PostgresTypeComparer.Instance)
                         .ThenBy(static pair => pair.Value.IsFixed,    InvertedBoolComparer.Instance)
                         .ThenBy(static pair => pair.Value.Length,     Comparer<SizeInfo?>.Default)
                         .ThenBy(static pair => pair.Value.ColumnName, StringComparer.InvariantCultureIgnoreCase);
    }


    public StringBuilder ColumnNames( int indentLevel )
    {
        int           length = Columns.Sum(static x => x.ColumnName.Length + 2) + Properties.Count * indentLevel * 4;
        StringBuilder sb     = new(length);
        int           index  = 0;

        foreach ( string columnName in Columns.Select(x => x.ColumnName) )
        {
            sb.Append(columnName);

            if ( index++ < Count - 1 )
            {
                sb.Append(',')
                  .Append('\n')
                  .Append(' ', indentLevel * 4);
            }
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

            if ( index++ < Count - 1 )
            {
                sb.Append(',')
                  .Append('\n')
                  .Append(' ', indentLevel * 4);
            }
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

            if ( index++ < Count - 1 )
            {
                sb.Append(',')
                  .Append('\n')
                  .Append(' ', indentLevel * 4);
            }
        }

        return sb;
    }


    public TableMetaDataEnumerator GetEnumerator() => new(this);
    string ITableMetaData.         CreateTable()   => CreateTable();
    public static string CreateTable()
    {
        if ( !string.IsNullOrWhiteSpace(_createTableSql) ) { return _createTableSql; }

        StringBuilder query     = new(10240);
        string        tableName = TSelf.TableName;

        query.Append("CREATE TABLE IF NOT EXISTS ");
        query.Append(tableName);
        query.Append(" (\n");

        for ( int index = 0; index < Instance.Count; index++ )
        {
            ref readonly ColumnMetaData column     = ref Instance[index].Column;
            string                      columnName = column.ColumnName_Padded(Instance);
            string                      dataType   = column.DataType_Padded(Instance);
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

                    query.AppendJoin(column.Checks.Value.And
                                         ? AND
                                         : OR,
                                     column.Checks.Value.Checks);

                    query.Append(" )");
                }

                if ( column.Defaults?.IsValid is true )
                {
                    query.Append(" DEFAULTS ");
                    query.Append(column.Defaults.Value.Defaults);
                }
            }

            if ( index < Instance.Count ) { query.Append(','); }

            query.Append('\n');
        }

        if ( Instance.ForeignKeyCount > 0 )
        {
            foreach ( ColumnMetaData column in Instance.ForeignKeys )
            {
                query.Append('\n');
                string foreignKeyName = Validate.ThrowIfNull(column.ForeignKeyName);
                query.Append($"    FOREIGN KEY ({column.ColumnName}) REFERENCES {foreignKeyName}({ColumnMetaData.ID.ColumnName}),"); //  ON DELETE CASCADE
            }

            query.Remove(query.Length - 1, 1);
            query.Append('\n');
        }

        query.Append(')');
        query.Append('\n');
        query.Append('\n');

        foreach ( ColumnMetaData column in Instance )
        {
            if ( !column.IsIndexed ) { continue; }

            query.Append($"CREATE INDEX IF NOT EXISTS {column.IndexColumnName_Padded(Instance)} ON {tableName}({column.ColumnName});\n");
        }

        query.Append($"""

                      CREATE TRIGGER IF NOT EXISTS {tableName}_{nameof(MigrationRecord.SetLastModified).SqlColumnName()}
                      BEFORE INSERT OR UPDATE ON {tableName}
                      FOR EACH ROW
                      EXECUTE FUNCTION {nameof(MigrationRecord.SetLastModified).SqlColumnName()}();
                      """);

        return _createTableSql = query.ToString();
    }


    public bool ContainsKey( string propertyName )                                                  => Properties.ContainsKey(propertyName);
    public bool TryGetValue( string propertyName, [MaybeNullWhen(false)] out ColumnMetaData value ) => Properties.TryGetValue(propertyName, out value);


    public static TableMetaData<TSelf> Create()
    {
        const BindingFlags ATTRIBUTES = BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty;

        ImmutableArray<PropertyInfo> properties = typeof(TSelf).GetProperties(ATTRIBUTES)
                                                               .AsValueEnumerable()
                                                               .Where(static x => !x.HasAttribute<DbIgnoreAttribute>())
                                                               .ToImmutableArray();

        if ( properties.Length <= 0 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' does not have any public instance properties that are not marked with the '{nameof(DbIgnoreAttribute)}' attribute."); }

        if ( properties.Count(ColumnMetaData.IsDbKey) != 1 ) { throw new InvalidOperationException($"Type '{typeof(TSelf)}' should only have one property with the '{typeof(System.ComponentModel.DataAnnotations.KeyAttribute).FullName}' or '{typeof(KeyAttribute).FullName}' attribute."); }


        SortedDictionary<string, ColumnMetaData> dictionary = new(StringComparer.InvariantCultureIgnoreCase);
        foreach ( PropertyInfo property in properties ) { dictionary[property.Name] = ColumnMetaData.Create(in property); }

        return new TableMetaData<TSelf>(dictionary.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase)) { Extras = typeof(TSelf).GetCustomAttribute<TableExtrasAttribute>() };
    }
}
