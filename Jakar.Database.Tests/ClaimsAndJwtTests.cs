using System.Security.Claims;
using Jakar.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class ClaimsAndJwtTests
{
    [Test]
    public void ClaimsPrincipal_TryParse_reads_user_identity_roles_and_groups()
    {
        RecordID<UserRecord> expectedId = RecordID<UserRecord>.Create(Guid.CreateVersion7());
        Claim[] claims =
        [
            new(ClaimType.UserID.ToClaimTypes(),   expectedId.Value.ToString()),
            new(ClaimType.UserName.ToClaimTypes(), "tester"),
            new(ClaimType.Role.ToClaimTypes(),     "admin"),
            new(ClaimType.Group.ToClaimTypes(),    "group-a")
        ];

        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "test"));

        bool parsed = principal.TryParse(out RecordID<UserRecord> userId, out string userName, out Claim[] roles, out Claim[] groups);
        string[] roleValues  = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(roles,  static x => x.Value));
        string[] groupValues = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(groups, static x => x.Value));

        Assert.Multiple(() =>
                        {
                            Assert.That(parsed,    Is.True);
                            Assert.That(userId,    Is.EqualTo(expectedId));
                            Assert.That(userName,  Is.EqualTo("tester"));
                            Assert.That(roleValues,  Is.EquivalentTo(new[] { "admin" }));
                            Assert.That(groupValues, Is.EquivalentTo(new[] { "group-a" }));
                        });
    }

    [Test]
    public void ClaimsPrincipal_TryParse_returns_false_for_invalid_user_ids()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
                                        [
                                            new Claim(ClaimType.UserID.ToClaimTypes(), "not-a-guid"),
                                            new Claim(ClaimType.UserName.ToClaimTypes(), "tester")
                                        ],
                                        "test"));

        bool parsedStruct = principal.TryParse(out RecordID<UserRecord> userId);
        bool parsedNullable = principal.TryParse(out RecordID<UserRecord>? nullableUserId);

        Assert.Multiple(() =>
                        {
                            Assert.That(parsedStruct,  Is.False);
                            Assert.That(userId,        Is.EqualTo(RecordID<UserRecord>.Empty));
                            Assert.That(parsedNullable, Is.False);
                            Assert.That(nullableUserId, Is.Null);
                        });
    }

    [Test]
    public void JwtExtensions_GetTokenValidationParameters_applies_defaults_and_overrides()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                                                        {
                                                                                            [DbOptions.JWT_KEY]                                  = "jwt-validation-parameters-test-key-with-enough-length-for-hmac-sha512-signatures",
                                                                                            [$"TokenValidation:{nameof(TokenValidationParameters.ValidIssuer)}"]     = "issuer-override",
                                                                                            [$"TokenValidation:{nameof(TokenValidationParameters.ValidAudience)}"]   = "audience-override",
                                                                                            [$"TokenValidation:{nameof(TokenValidationParameters.RequireAudience)}"] = bool.FalseString,
                                                                                            [$"TokenValidation:{nameof(TokenValidationParameters.ClockSkew)}"]       = "00:00:42"
                                                                                        })
                                                                  .Build();

        DbOptions options = CreateOptions();

        TokenValidationParameters parameters = configuration.GetTokenValidationParameters(options);

        Assert.Multiple(() =>
                        {
                            Assert.That(parameters.AuthenticationType,   Is.EqualTo(options.AuthenticationType));
                            Assert.That(parameters.ValidIssuer,          Is.EqualTo("issuer-override"));
                            Assert.That(parameters.ValidAudience,        Is.EqualTo("audience-override"));
                            Assert.That(parameters.RequireAudience,      Is.False);
                            Assert.That(parameters.ValidateIssuer,       Is.True);
                            Assert.That(parameters.ValidateAudience,     Is.True);
                            Assert.That(parameters.ClockSkew,            Is.EqualTo(TimeSpan.FromSeconds(42)));
                            Assert.That(parameters.IssuerSigningKey,     Is.TypeOf<SymmetricSecurityKey>());
                        });
    }

    [Test]
    public void JwtExtensions_TokenExpiration_uses_defaults_and_configuration()
    {
        IConfiguration defaultConfiguration = new ConfigurationBuilder().Build();
        DateTimeOffset beforeDefault = DateTimeOffset.UtcNow;
        DateTimeOffset defaultExpiration = defaultConfiguration.TokenExpiration();
        DateTimeOffset afterDefault = DateTimeOffset.UtcNow;

        IConfiguration configured = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                                                     {
                                                                                         ["TokenValidation:TokenExpiration"] = "00:05:00"
                                                                                     })
                                                               .Build();

        DateTimeOffset beforeConfigured = DateTimeOffset.UtcNow;
        DateTimeOffset configuredExpiration = configured.TokenExpiration();
        DateTimeOffset afterConfigured = DateTimeOffset.UtcNow;

        Assert.Multiple(() =>
                        {
                            Assert.That(defaultExpiration,    Is.InRange(beforeDefault.AddMinutes(29),  afterDefault.AddMinutes(31)));
                            Assert.That(configuredExpiration, Is.InRange(beforeConfigured.AddMinutes(4), afterConfigured.AddMinutes(6)));
                        });
    }

    [Test]
    public void JwtExtensions_GetSigningCredentials_uses_configured_key_and_algorithm()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                                                        {
                                                                                            [DbOptions.JWT_KEY] = "jwt-signing-credentials-test-key-with-enough-length-for-hmac-sha512-signatures"
                                                                                        })
                                                                  .Build();

        DbOptions options = CreateOptions();

        SigningCredentials credentials = configuration.GetSigningCredentials(options);

        Assert.Multiple(() =>
                        {
                            Assert.That(credentials.Algorithm, Is.EqualTo(options.JWTAlgorithm));
                            Assert.That(credentials.Key,       Is.TypeOf<SymmetricSecurityKey>());
                            Assert.That(((SymmetricSecurityKey)credentials.Key).Key.Length, Is.GreaterThanOrEqualTo(64));
                        });
    }

    [Test]
    public void AddAuth_registers_http_context_accessor_and_authentication_service()
    {
        ServiceProvider provider = new ServiceCollection().AddAuth<FakeAuthenticationService>().BuildServiceProvider();

        Assert.Multiple(() =>
                        {
                            Assert.That(provider.GetRequiredService<IHttpContextAccessor>(), Is.Not.Null);
                            Assert.That(provider.GetRequiredService<IAuthenticationService>(), Is.TypeOf<FakeAuthenticationService>());
                        });
    }



    private static DbOptions CreateOptions()
    {
        SecuredStringResolverOptions connectionString = "Host=localhost;Port=5432;Database=unit_tests;Username=dev;Password=dev";

        return new DbOptions
               {
                   TelemetrySource          = new TelemetrySource(AppVersion.Default, Guid.NewGuid(), nameof(ClaimsAndJwtTests), typeof(ClaimsAndJwtTests).Assembly.FullName),
                   ConnectionStringResolver = connectionString,
                   CommandTimeout           = 30,
                   TokenIssuer              = "issuer-default",
                   TokenAudience            = "audience-default",
                   LoggerOptions            = new AppLoggerOptions()
               };
    }



    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync( HttpContext context, string? scheme ) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync( HttpContext context, string? scheme, AuthenticationProperties? properties ) => Task.CompletedTask;
        public Task ForbidAsync( HttpContext context, string? scheme, AuthenticationProperties? properties ) => Task.CompletedTask;
        public Task SignInAsync( HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties ) => Task.CompletedTask;
        public Task SignOutAsync( HttpContext context, string? scheme, AuthenticationProperties? properties ) => Task.CompletedTask;
    }
}
