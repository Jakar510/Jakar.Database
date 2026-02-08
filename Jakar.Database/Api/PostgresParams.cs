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
                                                                                            [nameof(ICreatedBy.CreatedBy)]       = "created_by",
                                                                                            [nameof(IJsonModel.AdditionalData)]  = "additional_data"
                                                                                        };



    extension( string name )
    {
        public string SqlColumnName()      => __nameSnakeCaseCache.GetOrAdd(name, Strings.ToSnakeCase);
        public string SqlColumnIndexName() => __indexNameSnakeCaseCache.GetOrAdd(name, static x => $"{x.SqlColumnName()}_index");
        public string? SqlColumnIndexName( in ColumnOptions options ) => options.HasFlagValue(ColumnOptions.Indexed)
                                                                             ? name.SqlColumnIndexName()
                                                                             : null;
    }
}
