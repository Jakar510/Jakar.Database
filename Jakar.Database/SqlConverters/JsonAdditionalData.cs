// Jakar.Extensions :: Jakar.Database
// 09/09/2023  4:23 PM

namespace Jakar.Database;


public sealed class JsonAdditionalData : JsonSqlHandler<JsonAdditionalData, JObject?>
{
    public override void     SetValue( IDbDataParameter parameter, JObject? value ) => parameter.Value = value?.ToJson();
    public override JObject? Parse( object?             value ) => Parse(value as string);
    public static   JObject? Parse( string?             value ) => ( value ?? EMPTY ).GetAdditionalData();
}
