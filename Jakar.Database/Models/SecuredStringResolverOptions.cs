// Jakar.Extensions :: Jakar.Database
// 1/20/2024  21:17

namespace Jakar.Database;


public readonly struct SecuredStringResolverOptions
{
    public const     string                                                                DEFAULT_SQL_CONNECTION_STRING_KEY         = "DEFAULT";
    public const     string                                                                DEFAULT_SQL_CONNECTION_STRING_SECTION_KEY = "ConnectionStrings";
    private readonly Func<CancellationToken, Task<ConnectionString>>?                      __value0                                  = null;
    private readonly Func<CancellationToken, ValueTask<ConnectionString>>?                 __value1                                  = null;
    private readonly Func<IConfiguration, CancellationToken, Task<ConnectionString>>?      __value2                                  = null;
    private readonly Func<IConfiguration, CancellationToken, ValueTask<ConnectionString>>? __value3                                  = null;
    private readonly Func<IConfiguration, ConnectionString>?                               __value4                                  = null;
    private readonly Func<ConnectionString>?                                               __value5                                  = null;
    private readonly ConnectionString?                                                     __value6                                  = null;


    public SecuredStringResolverOptions( ConnectionString                                                     value ) => __value6 = value;
    public SecuredStringResolverOptions( Func<ConnectionString>                                               value ) => __value5 = value;
    public SecuredStringResolverOptions( Func<CancellationToken, Task<ConnectionString>>                      value ) => __value0 = value;
    public SecuredStringResolverOptions( Func<CancellationToken, ValueTask<ConnectionString>>                 value ) => __value1 = value;
    public SecuredStringResolverOptions( Func<IConfiguration, ConnectionString>                               value ) => __value4 = value;
    public SecuredStringResolverOptions( Func<IConfiguration, CancellationToken, Task<ConnectionString>>      value ) => __value2 = value;
    public SecuredStringResolverOptions( Func<IConfiguration, CancellationToken, ValueTask<ConnectionString>> value ) => __value3 = value;


    public static implicit operator SecuredStringResolverOptions( string                                                               value ) => new(value);
    public static implicit operator SecuredStringResolverOptions( ConnectionString                                                     value ) => new(value);
    public static implicit operator SecuredStringResolverOptions( Func<ConnectionString>                                               value ) => new(value);
    public static implicit operator SecuredStringResolverOptions( Func<CancellationToken, Task<ConnectionString>>                      value ) => new(value);
    public static implicit operator SecuredStringResolverOptions( Func<CancellationToken, ValueTask<ConnectionString>>                 value ) => new(value);
    public static implicit operator SecuredStringResolverOptions( Func<IConfiguration, ConnectionString>                               value ) => new(value);
    public static implicit operator SecuredStringResolverOptions( Func<IConfiguration, CancellationToken, Task<ConnectionString>>      value ) => new(value);
    public static implicit operator SecuredStringResolverOptions( Func<IConfiguration, CancellationToken, ValueTask<ConnectionString>> value ) => new(value);


    public static ConnectionString GetSecuredString( IConfiguration configuration, string key = DEFAULT_SQL_CONNECTION_STRING_KEY, string section = DEFAULT_SQL_CONNECTION_STRING_SECTION_KEY ) => configuration.GetSection(section).GetValue<string?>(key) ?? throw new KeyNotFoundException(key);
    public async ValueTask<ConnectionString> GetSecuredStringAsync( IConfiguration configuration, CancellationToken token, string key = DEFAULT_SQL_CONNECTION_STRING_KEY, string section = DEFAULT_SQL_CONNECTION_STRING_SECTION_KEY )
    {
        if ( __value0 is not null ) { return await __value0(token); }

        if ( __value1 is not null ) { return await __value1(token); }

        if ( __value2 is not null ) { return await __value2(configuration, token); }

        if ( __value3 is not null ) { return await __value3(configuration, token); }

        if ( __value4 is not null ) { return __value4(configuration); }

        if ( __value5 is not null ) { return __value5(); }

        if ( __value6 is not null ) { return __value6; }

        return configuration.GetSection(section).GetValue<string?>(key) ?? throw new KeyNotFoundException(key);
    }
}



[NotSerializable]
public sealed class ConnectionString( string value )
{
    private readonly string __value = value ?? throw new ArgumentNullException(nameof(value));

    public static implicit operator string( ConnectionString               wrapper ) => wrapper.ToString();
    public static implicit operator ReadOnlySpan<char>( ConnectionString   wrapper ) => wrapper.ToString();
    public static implicit operator ConnectionString( string               value )   => new(value);
    public static implicit operator ConnectionString( ReadOnlySpan<byte>   value )   => new(value.ToString());
    public static implicit operator ConnectionString( ReadOnlySpan<char>   value )   => new(value.ToString());
    public static implicit operator ConnectionString( ReadOnlyMemory<char> value )   => new(value.Span.ToString());
    public static implicit operator ConnectionString( Memory<char>         value )   => new(value.Span.ToString());


    public override string ToString() => __value;
}
