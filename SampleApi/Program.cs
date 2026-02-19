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
                        LoggerOptions            = new AppLoggerOptions()
                    };

builder.AddDatabase<SampleDatabase>(options);


await using WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if ( app.Environment.IsDevelopment() ) { app.MapOpenApi(); }

app.UseHttpsRedirection();

app.UseAuthorization();

// app.MapControllers();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseTelemetry();

app.MapGet("/",     static () => DateTimeOffset.UtcNow);
app.MapGet("/Ping", static () => DateTimeOffset.UtcNow);


await app.RunWithMigrationsAsync(["localhost:8181", "0.0.0.0:8181"], SampleDatabase.TestAll);
