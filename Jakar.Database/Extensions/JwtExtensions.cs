// Jakar.Extensions :: Jakar.Database
// 10/10/2022  5:01 PM

namespace Jakar.Database;


public static class JwtExtensions
{
    extension( IConfiguration self )
    {
        [Pure] public DateTimeOffset TokenExpiration() => self.TokenExpiration(TimeSpan.FromMinutes(30));
        [Pure] public DateTimeOffset TokenExpiration( TimeSpan defaultValue )
        {
            TimeSpan offset = self.TokenValidation()
                                  .GetValue(nameof(TokenExpiration), defaultValue);

            return DateTimeOffset.UtcNow + offset;
        }
        public IConfigurationSection TokenValidation()                            => self.GetSection(nameof(TokenValidation));
        public byte[]                GetJWTKey( DbOptions               options ) => Encoding.UTF8.GetBytes(self[options.JWTKey] ?? EMPTY);
        public SymmetricSecurityKey  GetSymmetricSecurityKey( DbOptions options ) => new(self.GetJWTKey(options));
        public SigningCredentials    GetSigningCredentials( DbOptions   options ) => new(self.GetSymmetricSecurityKey(options), options.JWTAlgorithm);
        public TokenValidationParameters GetTokenValidationParameters( DbOptions options )
        {
            IConfigurationSection section = self.TokenValidation();
            SymmetricSecurityKey  key     = self.GetSymmetricSecurityKey(options);
            return section.GetTokenValidationParameters(key, options);
        }
    }



    public static TokenValidationParameters GetTokenValidationParameters( this WebApplication        app,     DbOptions options ) => app.Configuration.GetTokenValidationParameters(options);
    public static TokenValidationParameters GetTokenValidationParameters( this WebApplicationBuilder builder, DbOptions options ) => builder.Configuration.GetTokenValidationParameters(options);
    public static TokenValidationParameters GetTokenValidationParameters( this IConfigurationSection section, SymmetricSecurityKey key, DbOptions options ) =>
        new()
        {
            IssuerSigningKey                          = key,
            AuthenticationType                        = options.AuthenticationType,
            IgnoreTrailingSlashWhenValidatingAudience = section.GetValue(nameof(TokenValidationParameters.IgnoreTrailingSlashWhenValidatingAudience), true),
            RequireAudience                           = section.GetValue(nameof(TokenValidationParameters.RequireAudience),                           true),
            RequireSignedTokens                       = section.GetValue(nameof(TokenValidationParameters.RequireSignedTokens),                       true),
            RequireExpirationTime                     = section.GetValue(nameof(TokenValidationParameters.RequireExpirationTime),                     true),
            ValidateLifetime                          = section.GetValue(nameof(TokenValidationParameters.ValidateLifetime),                          true),
            ValidateIssuerSigningKey                  = section.GetValue(nameof(TokenValidationParameters.ValidateIssuerSigningKey),                  true),
            ValidateIssuer                            = section.GetValue(nameof(TokenValidationParameters.ValidateIssuer),                            true),
            ValidateAudience                          = section.GetValue(nameof(TokenValidationParameters.ValidateAudience),                          true),
            ValidateActor                             = section.GetValue(nameof(TokenValidationParameters.ValidateActor),                             false),
            ValidateTokenReplay                       = section.GetValue(nameof(TokenValidationParameters.ValidateTokenReplay),                       false),
            ClockSkew                                 = section.GetValue(nameof(TokenValidationParameters.ClockSkew),                                 options.ClockSkew),
            ValidIssuer                               = section.GetValue(nameof(TokenValidationParameters.ValidIssuer),                               options.TokenIssuer),
            ValidAudience                             = section.GetValue(nameof(TokenValidationParameters.ValidAudience),                             options.TokenAudience)
        };
}
