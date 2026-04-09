// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

namespace Jakar.Database;


public static class PostgresParams
{
    private static readonly ConcurrentDictionary<string, string> __indexNameSnakeCaseCache = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> __nameSnakeCaseCache = new(StringComparer.InvariantCultureIgnoreCase)
                                                                                        {
                                                                                            [nameof(MimeType)]                   = "mime_types",
                                                                                            [nameof(SupportedLanguage)]          = "languages",
                                                                                            [nameof(ProgrammingLanguage)]        = "programming_languages",
                                                                                            [nameof(SubscriptionStatus)]         = "subscription_status",
                                                                                            [nameof(DeviceCategory)]             = "device_categories",
                                                                                            [nameof(DevicePlatform)]             = "device_platforms",
                                                                                            [nameof(DeviceTypes)]                = "device_types",
                                                                                            [nameof(DistanceUnit)]               = "distance_units",
                                                                                            [nameof(Status)]                     = "statuses",
                                                                                            [nameof(NpgsqlDbType)]               = "db_types",
                                                                                            [nameof(IUniqueID.ID)]               = "id",
                                                                                            [nameof(IDateCreated.DateCreated)]   = "date_created",
                                                                                            [nameof(ILastModified.LastModified)] = "last_modified",
                                                                                            [nameof(IUserRecordID.UserID)]       = "user_id",
                                                                                            [nameof(IJsonModel.AdditionalData)]  = "additional_data"
                                                                                        };
    private static readonly ConcurrentDictionary<(string Original, int MaxLength), string> __paddedCache        = new();
    private static readonly ConcurrentDictionary<string, string>                           __parameterNameCache = new(Environment.ProcessorCount, DEFAULT_CAPACITY, StringComparer.InvariantCulture);


    public static string AddSqlName<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => AddSqlName<TSelf>(TSelf.TableName);
    public static  string AddSqlName<TSelf>( string  sqlName )              => AddSqlName(typeof(TSelf).Name, sqlName);
    public static  string AddSqlName( string         name, string sqlName ) => __nameSnakeCaseCache.AddOrUpdate(name, sqlName, UpdateValueFactory);
    private static string UpdateValueFactory( string key,  string value )   => value;


    public static string SqlName( this Type type ) => __nameSnakeCaseCache.GetOrAdd(type.Name, Strings.ToSnakeCase);


    public static string SqlName( this ReadOnlySpan<char> propertyName )
    {
        ConcurrentDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> lookup = __nameSnakeCaseCache.GetAlternateLookup<ReadOnlySpan<char>>();
        if ( lookup.TryGetValue(propertyName, out string? result) ) { return result; }

        result = propertyName.ToSnakeCase(CultureInfo.InvariantCulture);
        lookup.TryAdd(propertyName, result);
        return result;
    }



    extension( string parameterName )
    {
        public string Parameterize()
        {
            parameterName = __parameterNameCache.GetOrAdd(parameterName,
                                                          static x => x.Contains('.')
                                                                          ? x.Split('.')[^1]
                                                                          : x);

            return parameterName.SqlName();
        }
    }



    extension( string propertyName )
    {
        public  string GetPadded( int maxLength )       => __paddedCache.GetOrAdd(( propertyName, maxLength ), static pair => pair.Original.PadRight(pair.MaxLength));
        public  string SqlName()                        => __nameSnakeCaseCache.GetOrAdd(Validate.ThrowIfNull(propertyName), Strings.ToSnakeCase);
        public  string SqlIndexName( string tableName ) => __indexNameSnakeCaseCache.GetOrAdd(Validate.ThrowIfNull(propertyName), GetIndexName, Validate.ThrowIfNull(tableName));
        private string GetIndexName( string tableName ) => $"idx_{tableName}_{propertyName.SqlName()}";
    }
}
