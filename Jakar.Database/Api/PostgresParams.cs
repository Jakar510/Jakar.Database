// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

namespace Jakar.Database;


public static class PostgresParams
{
    public static readonly ConcurrentDictionary<string, string> NameSnakeCaseCache = new(StringComparer.InvariantCultureIgnoreCase)
                                                                                     {
                                                                                         [nameof(MimeType)]            = "mime_types",
                                                                                         [nameof(SupportedLanguage)]   = "languages",
                                                                                         [nameof(ProgrammingLanguage)] = "programming_languages",
                                                                                         [nameof(SubscriptionStatus)]  = "subscription_status",
                                                                                         [nameof(DeviceCategory)]      = "device_categories",
                                                                                         [nameof(DevicePlatform)]      = "device_platforms",
                                                                                         [nameof(DeviceTypes)]         = "device_types",
                                                                                         [nameof(DistanceUnit)]        = "distance_units",
                                                                                         [nameof(Status)]              = "statuses",
                                                                                         [nameof(NpgsqlDbType)]        = "db_types"
                                                                                     };
    public static  string SqlColumnName( this string name ) => NameSnakeCaseCache.GetOrAdd(name, ToSnakeCase);
    private static string ToSnakeCase( string        x )    => x.ToSnakeCase();
}
