// Jakar.Extensions :: Jakar.Database
// 09/09/2023  4:23 PM

namespace Jakar.Database;


public sealed class JsonAdditionalData : SqlConverter<JsonAdditionalData, JObjectNode?>
{
    public override void         SetValue( IDbDataParameter parameter, JObjectNode? value ) => parameter.Value = value?.ToJson();
    public override JObjectNode? Parse( object?             value ) => Parse(value as string);
    public static   JObjectNode? Parse( string?             value ) => value.TryFromJson() as JObjectNode;
}
