namespace Jakar.Database;


public abstract class JsonSqlHandler<TSelf, TValue> : SqlConverter<TSelf, TValue>
    where TSelf : JsonSqlHandler<TSelf, TValue>, new()
    where TValue : IJsonSerializable<TValue>
{
    public override TValue Parse( object? value )
    {
        string? s = value?.ToString();

        return s is not null
                   ? s.FromJson<TValue>()
                   : default!;
    }
    public override void SetValue( IDbDataParameter parameter, TValue? value )
    {
        parameter.Value = value is null
                              ? null
                              : JsonCodec.ToJson(in value);

        parameter.DbType = DbType.String;
    }
}



public sealed class JsonSqlHandler<TValue> : JsonSqlHandler<JsonSqlHandler<TValue>, TValue>
    where TValue : IJsonSerializable<TValue>;
