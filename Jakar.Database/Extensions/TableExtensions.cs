namespace Jakar.Database;


public static class TableExtensions
{
    public static string GetCollectionTypeName( this PostgresCollectionType type )
    {
        return type switch
               {
                   PostgresCollectionType.METADATA_COLLECTIONS    => @"METADATACOLLECTIONS",
                   PostgresCollectionType.RESTRICTIONS            => @"RESTRICTIONS",
                   PostgresCollectionType.DATA_SOURCE_INFORMATION => @"DATASOURCEINFORMATION",
                   PostgresCollectionType.DATA_TYPES              => @"DATATYPES",
                   PostgresCollectionType.RESERVED_WORDS          => @"RESERVEDWORDS",
                   PostgresCollectionType.DATABASES               => @"DATABASES",
                   PostgresCollectionType.SCHEMATA                => @"SCHEMATA",
                   PostgresCollectionType.TABLES                  => @"TABLES",
                   PostgresCollectionType.COLUMNS                 => @"COLUMNS",
                   PostgresCollectionType.VIEWS                   => @"VIEWS",
                   PostgresCollectionType.MATERIALIZED_VIEWS      => @"MATERIALIZEDVIEWS",
                   PostgresCollectionType.USERS                   => @"USERS",
                   PostgresCollectionType.INDEXES                 => @"INDEXES",
                   PostgresCollectionType.INDEX_COLUMNS           => @"INDEXCOLUMNS",
                   PostgresCollectionType.CONSTRAINTS             => @"CONSTRAINTS",
                   PostgresCollectionType.PRIMARY_KEY             => @"PRIMARYKEY",
                   PostgresCollectionType.UNIQUE_KEYS             => @"UNIQUEKEYS",
                   PostgresCollectionType.FOREIGN_KEYS            => @"FOREIGNKEYS",
                   PostgresCollectionType.CONSTRAINT_COLUMNS      => @"CONSTRAINTCOLUMNS",
                   _                                              => throw new OutOfRangeException(type)
               };
    }


    public static ref ValueStringBuilder Spacer( this ref ValueStringBuilder builder, int indentLevel ) => ref builder.Append(' ', indentLevel * 4);


    [Pure] public static string Ids<TSelf>( this IEnumerable<RecordID<TSelf>> values )
        where TSelf : class, IPairRecord<TSelf> => values.AsValueEnumerable().Ids();
    [Pure] public static string Ids<TEnumerable, TSelf>( this ValueEnumerable<TEnumerable, RecordID<TSelf>> values )
        where TSelf : class, IPairRecord<TSelf>
        where TEnumerable : struct, IValueEnumerator<RecordID<TSelf>>, allows ref struct
    {
        const string                                        SEPARATOR  = ", ";
        ValueStringBuilder                                  ids        = new();
        using ValueEnumerator<TEnumerable, RecordID<TSelf>> enumerator = values.GetEnumerator();

        for ( int i = 0; enumerator.MoveNext(); i++ )
        {
            if ( i > 0 ) { ids.Append(SEPARATOR); }

            ids.Append('\'').AppendSpanFormattable(enumerator.Current.Value, default, CultureInfo.InvariantCulture).Append('\'');
        }

        return ids.ToString();
    }
    [Pure] public static string Ids<TSelf>( this IEnumerable<Guid> values )
        where TSelf : class, IPairRecord<TSelf> => values.AsValueEnumerable().Ids();
    [Pure] public static string Ids<TEnumerable>( this ValueEnumerable<TEnumerable, Guid> values )
        where TEnumerable : struct, IValueEnumerator<Guid>, allows ref struct
    {
        const string                             SEPARATOR  = ", ";
        ValueStringBuilder                       ids        = new();
        using ValueEnumerator<TEnumerable, Guid> enumerator = values.GetEnumerator();

        for ( int i = 0; enumerator.MoveNext(); i++ )
        {
            if ( i > 0 ) { ids.Append(SEPARATOR); }

            ids.Append('\'').AppendSpanFormattable(enumerator.Current, default, CultureInfo.InvariantCulture).Append('\'');
        }

        return ids.ToString();
    }



    extension<TSelf>( TSelf self )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        [Pure] public TSelf Validate()
        {
        #if DEBUG
            _ = self.ToDynamicParameters().Validate<TSelf>();
            return self;
        #else
            return self;
        #endif
        }
    }



    public static string GetTableName( this Type type, bool convertToSnakeCase = true )
    {
        string name = type.GetCustomAttribute<TableAttribute>()?.Name ?? type.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>()?.Name ?? type.Name;

        if ( convertToSnakeCase ) { name = name.ToSnakeCase(CultureInfo.InvariantCulture); }

        return name;
    }



    extension( DbDataReader self )
    {
        public DateTimeOffset DateCreated<TRecord>()
            where TRecord : TableRecord<TRecord>, ITableRecord<TRecord>, IDateCreated => self.GetFieldValue<TRecord, DateTimeOffset>(nameof(IDateCreated.DateCreated));

        public DateTimeOffset? LastModified<TRecord>()
            where TRecord : LastModifiedRecord<TRecord>, ITableRecord<TRecord>, ILastModified => self.GetFieldValue<TRecord, DateTimeOffset?>(nameof(ILastModified.LastModified));


        public JObjectNode? GetAdditionalData<TRecord>()
            where TRecord : TableRecord<TRecord>, ITableRecord<TRecord>
        {
            int ordinal = TRecord.MetaData[nameof(IJsonModel.AdditionalData)].Index;

            return self.IsDBNull(ordinal)
                       ? null
                       : JsonAdditionalData.Parse(self.GetValue<string>(ordinal));
        }
        public TValue GetFieldValue<TRecord, TValue>( string propertyName )
            where TRecord : TableRecord<TRecord>, ITableRecord<TRecord>
        {
            int ordinal = TRecord.MetaData[propertyName].Index;

            return self.IsDBNull(ordinal)
                       ? default!
                       : self.GetFieldValue<TValue>(ordinal);
        }
        public TValue GetFieldValue<TRecord, TValue>( string propertyName, TValue defaultValue )
            where TRecord : TableRecord<TRecord>, ITableRecord<TRecord>
            where TValue : IParsable<TValue>
        {
            int ordinal = TRecord.MetaData[propertyName].Index;

            return self.IsDBNull(ordinal)
                       ? defaultValue
                       : TValue.TryParse(self.GetFieldValue<string>(ordinal), CultureInfo.InvariantCulture, out TValue? result)
                           ? result
                           : defaultValue;
        }
        public TValue GetEnumValue<TRecord, TValue>( string propertyName, TValue defaultValue )
            where TRecord : TableRecord<TRecord>, ITableRecord<TRecord>
            where TValue : unmanaged, Enum
        {
            int ordinal = TRecord.MetaData[propertyName].Index;

            return self.IsDBNull(ordinal)
                       ? defaultValue
                       : EnumSqlHandler<TValue>.TryParse(self.GetFieldValue<string>(ordinal), defaultValue);
        }


        public TValue? GetData<TValue>( string propertyName )
            where TValue : IJsonSerializable<TValue> => self.GetFieldValue<object?>(propertyName) is string s
                                                            ? s.FromJson<TValue>()
                                                            : default;
        public TValue? GetData<TValue>( int ordinal )
            where TValue : IJsonSerializable<TValue> => self.GetFieldValue<object?>(ordinal) is string s
                                                            ? s.FromJson<TValue>()
                                                            : default;
        public TValue? GetValue<TValue>( string propertyName ) => self.GetFieldValue<object?>(propertyName) is TValue value
                                                                      ? value
                                                                      : default;
        public TValue? GetValue<TValue>( int ordinal ) => self.GetFieldValue<object?>(ordinal) is TValue value
                                                              ? value
                                                              : default;
    }
}
