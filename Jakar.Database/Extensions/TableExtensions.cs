namespace Jakar.Database;


public static class TableExtensions
{
    [Pure] public static StringBuilder Ids<TSelf>( this IEnumerable<RecordID<TSelf>> values )
        where TSelf : class, ITableRecord<TSelf> => values.AsValueEnumerable()
                                                          .Ids();
    [Pure] public static StringBuilder Ids<TEnumerable, TSelf>( this ValueEnumerable<TEnumerable, RecordID<TSelf>> values )
        where TSelf : class, ITableRecord<TSelf>
        where TEnumerable : struct, IValueEnumerator<RecordID<TSelf>>, allows ref struct
    {
        const string                                        SEPARATOR  = ", ";
        StringBuilder                                       ids        = new();
        using ValueEnumerator<TEnumerable, RecordID<TSelf>> enumerator = values.GetEnumerator();

        for ( int i = 0; enumerator.MoveNext(); i++ )
        {
            if ( i > 0 ) { ids.Append(SEPARATOR); }

            ids.Append('\'');
            ids.Append(enumerator.Current.Value);
            ids.Append('\'');
        }

        return ids;
    }


    [Pure] public static TSelf Validate<TSelf>( this TSelf self )
        where TSelf : class, ITableRecord<TSelf>
    {
        if ( !Debugger.IsAttached ) { return self; }

        if ( !string.Equals(TSelf.TableName, TSelf.TableName.ToSnakeCase()) ) { throw new InvalidOperationException($"{typeof(TSelf).Name}: {nameof(TSelf.TableName)} is not snake_case: '{TSelf.TableName}'"); }

        PostgresParameters parameters     = self.ToDynamicParameters();
        string[]           parameterNames = parameters.ParameterNames.ToArray();
        int                length         = parameterNames.Length;


        if ( length == TSelf.ClassProperties.Length ) { return self; }


        HashSet<string> missing =
        [
            .. TSelf.ClassProperties.AsValueEnumerable()
                    .Select(static x => x.Name)
        ];

        missing.ExceptWith(parameterNames);

        string message = $"""
                          {typeof(TSelf).Name}: {nameof(self.ToDynamicParameters)}.Length ({length}) != {nameof(TSelf.ClassProperties)}.Length ({TSelf.ClassProperties.Length})
                          {missing.ToJson()}
                          """;

        throw new InvalidOperationException(message);
    }



    extension( NpgsqlDataReader self )
    {
        public JObject? GetAdditionalData<TRecord>()
            where TRecord : class, ITableRecord<TRecord>
        {
            int ordinal = TRecord.PropertyMetaData[nameof(IJsonModel.AdditionalData)].Index;

            return self.IsDBNull(ordinal)
                       ? null
                       : self.GetValue<string>(ordinal)
                            ?.GetAdditionalData();
        }
        public TValue GetFieldValue<TRecord, TValue>( string propertyName )
            where TRecord : class, ITableRecord<TRecord>
        {
            int ordinal = TRecord.PropertyMetaData[propertyName].Index;

            return self.IsDBNull(ordinal)
                       ? default!
                       : self.GetFieldValue<TValue>(ordinal);
        }
        public TValue GetFieldValue<TRecord, TValue>( string propertyName, TValue defaultValue )
            where TRecord : class, ITableRecord<TRecord>
            where TValue : IParsable<TValue>
        {
            int ordinal = TRecord.PropertyMetaData[propertyName].Index;

            return self.IsDBNull(ordinal)
                       ? defaultValue
                       : TValue.TryParse(self.GetFieldValue<string>(ordinal), CultureInfo.InvariantCulture, out TValue? result)
                           ? result
                           : defaultValue;
        }
        public TValue GetEnumValue<TRecord, TValue>( string propertyName, TValue defaultValue )
            where TRecord : class, ITableRecord<TRecord>
            where TValue : unmanaged, Enum
        {
            int ordinal = TRecord.PropertyMetaData[propertyName].Index;

            return self.IsDBNull(ordinal)
                       ? defaultValue
                       : EnumSqlHandler<TValue>.TryParse(self.GetFieldValue<string>(ordinal), defaultValue);
        }


        public TValue? GetData<TValue>( string propertyName ) => self.GetFieldValue<object?>(propertyName) is string s
                                                                     ? s.FromJson<TValue>()
                                                                     : default;
        public TValue? GetData<TValue>( int ordinal ) => self.GetFieldValue<object?>(ordinal) is string s
                                                             ? s.FromJson<TValue>()
                                                             : default;
        public TValue? GetValue<TValue>( string propertyName ) => self.GetFieldValue<object?>(propertyName) is TValue value
                                                                      ? value
                                                                      : default;
        public TValue? GetValue<TValue>( int ordinal ) => self.GetFieldValue<object?>(ordinal) is TValue value
                                                              ? value
                                                              : default;
    }



    public static string GetTableName( this Type type, bool convertToSnakeCase = true )
    {
        string name = type.GetCustomAttribute<TableAttribute>()
                         ?.Name ??
                      type.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>()
                         ?.Name ??
                      type.Name;

        if ( convertToSnakeCase ) { name = name.ToSnakeCase(CultureInfo.InvariantCulture); }

        return name;
    }
}
