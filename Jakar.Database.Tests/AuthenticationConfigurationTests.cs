using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Jakar.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class AuthenticationConfigurationTests
{
    [Test]
    public async Task Hybrid_authentication_accepts_bearer_headers_for_api_requests()
    {
        await using TestHost host = await TestHost.CreateAsync();
        using HttpClient client = host.App.GetTestClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", host.CreateJwt("api-user"));

        using HttpResponseMessage response = await client.GetAsync("/api/secure");
        string                    body     = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
                        {
                            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                            Assert.That(body,                Does.Contain("Bearer"));
                            Assert.That(body,                Does.Contain("api-user"));
                        });
    }

    [Test]
    public async Task Hybrid_authentication_accepts_cookies_for_interactive_requests()
    {
        await using TestHost host = await TestHost.CreateAsync();
        using HttpClient client = host.App.GetTestClient();

        using HttpResponseMessage login = await client.PostAsync("/auth/cookie-login", content: null);
        string                    cookie = login.Headers.GetValues("Set-Cookie").First().Split(';', 2)[0];

        client.DefaultRequestHeaders.Add("Cookie", cookie);

        using HttpResponseMessage response = await client.GetAsync("/app/secure");
        string                    body     = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
                        {
                            Assert.That(login.StatusCode,    Is.EqualTo(HttpStatusCode.NoContent));
                            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                            Assert.That(body,                Does.Contain(IdentityConstants.ApplicationScheme));
                            Assert.That(body,                Does.Contain("cookie-user"));
                        });
    }

    [Test]
    public async Task Hybrid_authentication_returns_401_for_api_requests_without_credentials()
    {
        await using TestHost host = await TestHost.CreateAsync();
        using HttpClient client = host.App.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync("/api/secure");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Bearer_header_is_honored_for_non_api_requests()
    {
        await using TestHost host = await TestHost.CreateAsync();
        using HttpClient client = host.App.GetTestClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", host.CreateJwt("bearer-on-app"));

        using HttpResponseMessage response = await client.GetAsync("/app/secure");
        string                    body     = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
                        {
                            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                            Assert.That(body,                Does.Contain("Bearer"));
                            Assert.That(body,                Does.Contain("bearer-on-app"));
                        });
    }

    [Test]
    public async Task Bearer_options_resolve_from_di_without_invalid_authority()
    {
        await using TestHost host = await TestHost.CreateAsync();

        JwtBearerOptions options = host.App.Services.GetJwtBearerOptions();

        Assert.Multiple(() =>
                        {
                            Assert.That(options.Audience,               Is.EqualTo(host.Options.TokenAudience));
                            Assert.That(options.ClaimsIssuer,           Is.EqualTo(host.Options.TokenIssuer));
                            Assert.That(options.Authority,              Is.Null.Or.Empty);
                            Assert.That(options.RequireHttpsMetadata,   Is.False);
                            Assert.That(options.TokenValidationParameters.ValidIssuer,   Is.EqualTo(host.Options.TokenIssuer));
                            Assert.That(options.TokenValidationParameters.ValidAudience, Is.EqualTo(host.Options.TokenAudience));
                        });
    }



    private sealed class TestHost : IAsyncDisposable
    {
        public required WebApplication App           { get; init; }
        public required IConfiguration  Configuration { get; init; }
        public required DbOptions       Options       { get; init; }


        public static async Task<TestHost> CreateAsync()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
                                                                         {
                                                                             EnvironmentName = Environments.Development,
                                                                             ApplicationName = typeof(AuthenticationConfigurationTests).Assembly.FullName,
                                                                             ContentRootPath = TestContext.CurrentContext.WorkDirectory
                                                                         });

            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                                                        {
                                                            [DbOptions.JWT_KEY] = "test-secret-for-auth-configuration-tests-12345-with-enough-length-for-hmac-sha512"
                                                        });

            SecuredStringResolverOptions connectionString = "Host=localhost;Port=5432;Database=auth_tests;Username=dev;Password=dev";

            DbOptions options = new()
                                {
                                    TelemetrySource          = new TelemetrySource(AppVersion.Default, Guid.NewGuid(), nameof(AuthenticationConfigurationTests), typeof(AuthenticationConfigurationTests).Assembly.FullName),
                                    ConnectionStringResolver = connectionString,
                                    CommandTimeout           = 30,
                                    TokenIssuer              = nameof(AuthenticationConfigurationTests),
                                    TokenAudience            = nameof(AuthenticationConfigurationTests),
                                    LoggerOptions            = new AppLoggerOptions(),
                                    ConfigureCookieAuth = static cookie =>
                                                          {
                                                              cookie.Cookie.Name    = "jakar.auth";
                                                              cookie.LoginPath      = "/login";
                                                              cookie.AccessDeniedPath = "/denied";
                                                          }
                                };

            builder.Services.AddSingleton(options);
            options.AddAuthentication(builder);
            builder.Services.AddAuthorization();

            WebApplication app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapPost("/auth/cookie-login",
                        async ( HttpContext context ) =>
                        {
                            ClaimsPrincipal principal = CreatePrincipal(options.CookieAuthenticationScheme, "cookie-user");
                            await context.SignInAsync(options.CookieAuthenticationScheme, principal);
                            return Results.NoContent();
                        });

            app.MapGet("/api/secure", static ( ClaimsPrincipal user ) => Results.Text(DescribePrincipal(user))).RequireAuthorization();
            app.MapGet("/app/secure", static ( ClaimsPrincipal user ) => Results.Text(DescribePrincipal(user))).RequireAuthorization();

            await app.StartAsync();

            return new TestHost
                   {
                       App           = app,
                       Configuration = builder.Configuration,
                       Options       = options
                   };
        }

        public string CreateJwt( string userName )
        {
            string accessToken = DbTokenHandler.Instance.CreateToken(new SecurityTokenDescriptor
                                                                     {
                                                                         Subject            = (ClaimsIdentity)CreatePrincipal(Options.BearerAuthenticationScheme, userName).Identity!,
                                                                         Expires            = DateTime.UtcNow.AddMinutes(15),
                                                                         Issuer             = Options.TokenIssuer,
                                                                         Audience           = Options.TokenAudience,
                                                                         IssuedAt           = DateTime.UtcNow,
                                                                         SigningCredentials = Configuration.GetSigningCredentials(Options)
                                                                     });

            return accessToken;
        }
        public async ValueTask DisposeAsync() => await App.DisposeAsync();


        private static ClaimsPrincipal CreatePrincipal( string scheme, string userName )
        {
            Claim[] claims =
            [
                new Claim(ClaimType.UserName.ToClaimTypes(), userName),
                new Claim(ClaimType.UserID.ToClaimTypes(), Guid.CreateVersion7().ToString())
            ];

            return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme, ClaimType.UserName.ToClaimTypes(), ClaimType.Role.ToClaimTypes()));
        }
        private static string DescribePrincipal( ClaimsPrincipal user )
        {
            string scheme   = user.Identity?.AuthenticationType ?? "<none>";
            string userName = user.Claims.FirstOrDefault(static claim => claim.Type == ClaimType.UserName.ToClaimTypes())?.Value ?? "<missing>";
            return $"{scheme}:{userName}";
        }
    }
}
