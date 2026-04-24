using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


SecuredStringResolverOptions connectionString = $"User ID=dev;Password=dev;Host=localhost;Port=5432;Database=jakar_database_sample";

DbOptions options = new()
                    {
                        TelemetrySource          = new TelemetrySource(AppVersion.Default, Guid.NewGuid(), nameof(SampleApi), typeof(Program).Assembly.FullName),
                        ConnectionStringResolver = connectionString,
                        CommandTimeout           = 30,
                        TokenIssuer              = SampleDatabase.AppName,
                        TokenAudience            = SampleDatabase.AppName,
                        LoggerOptions            = new AppLoggerOptions(),
                        ConfigureCookieAuth = static cookie =>
                                              {
                                                  cookie.Cookie.Name      = "sample_api_auth";
                                                  cookie.LoginPath        = "/auth/login";
                                                  cookie.AccessDeniedPath = "/auth/denied";
                                              }
                    };

builder.AddDatabase<SampleDatabase>(options);


await using WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if ( app.Environment.IsDevelopment() ) { app.MapOpenApi(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseTelemetry();

app.MapGet("/",     static () => DateTimeOffset.UtcNow);
app.MapGet("/Ping", static () => DateTimeOffset.UtcNow);

app.MapPost("/auth/dev/cookie",
            async ( HttpContext context ) =>
            {
                await context.SignInAsync(options.CookieAuthenticationScheme, createPrincipal(options.CookieAuthenticationScheme, "sample-cookie-user"));
                return Results.NoContent();
            });

app.MapGet("/auth/dev/token",
           ( IConfiguration configuration ) =>
           {
               string accessToken = DbTokenHandler.Instance.CreateToken(new SecurityTokenDescriptor
                                                                        {
                                                                            Subject            = (ClaimsIdentity)createPrincipal(options.BearerAuthenticationScheme, "sample-bearer-user").Identity!,
                                                                            Expires            = DateTime.UtcNow.AddMinutes(15),
                                                                            Issuer             = options.TokenIssuer,
                                                                            Audience           = options.TokenAudience,
                                                                            IssuedAt           = DateTime.UtcNow,
                                                                            SigningCredentials = configuration.GetSigningCredentials(options)
                                                                        });

               return TypedResults.Ok(new { accessToken });
           });

app.MapGet("/api/me", static ( ClaimsPrincipal user ) => TypedResults.Ok(describePrincipal(user))).RequireAuthorization();
app.MapGet("/app/me", static ( ClaimsPrincipal user ) => TypedResults.Ok(describePrincipal(user))).RequireAuthorization();


await app.RunWithMigrationsAsync(["localhost:8181", "0.0.0.0:8181"], SampleDatabase.TestAll);
return;


static ClaimsPrincipal createPrincipal( string scheme, string userName )
{
    Claim[] claims = [new Claim(ClaimType.UserName.ToClaimTypes(), userName), new Claim(ClaimType.UserID.ToClaimTypes(), Guid.CreateVersion7().ToString())];

    return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme, ClaimType.UserName.ToClaimTypes(), ClaimType.Role.ToClaimTypes()));
}

static object describePrincipal( ClaimsPrincipal user )
{
    string authenticationType = user.Identity?.AuthenticationType                                                                  ?? "<none>";
    string userName           = user.Claims.FirstOrDefault(static claim => claim.Type == ClaimType.UserName.ToClaimTypes())?.Value ?? "<missing>";

    return new
           {
               authenticationType,
               userName
           };
}
