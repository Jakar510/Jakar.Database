// Jakar.Extensions :: Jakar.Database
// 1/10/2024  14:10


namespace Jakar.Database;


[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
public static class DbServices
{
    public const string AUTHENTICATION_SCHEME              = JwtBearerDefaults.AuthenticationScheme;
    public const string AUTHENTICATION_SCHEME_DISPLAY_NAME = $"Jwt.{AUTHENTICATION_SCHEME}";
    public const string OTEL_EXPORTER_OTLP_ENDPOINT        = nameof(OTEL_EXPORTER_OTLP_ENDPOINT);



    extension<TSelf>( TSelf self )
        where TSelf : class, ITableRecord<TSelf>
    {
        public bool IsValid()    => self.ID.IsValid();
        public bool IsNotValid() => !self.IsValid();
    }



    public static string GetFullName( this Type type ) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;



    extension( IHostApplicationBuilder self )
    {
        public IHostApplicationBuilder OpenTelemetry()
        {
            self.Logging.AddOpenTelemetry(static x =>
                                          {
                                              x.IncludeScopes           = true;
                                              x.IncludeFormattedMessage = true;
                                          });

            self.Services.AddOpenTelemetry()
                .WithMetrics(static x =>
                             {
                                 x.AddRuntimeInstrumentation()
                                  .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "System.Net.Http", "WeatherApp.Api");
                             })
                .WithTracing(x =>
                             {
                                 if ( self.Environment.IsDevelopment() ) { x.SetSampler<AlwaysOnSampler>(); }

                                 x.AddAspNetCoreInstrumentation()
                                  .AddGrpcClientInstrumentation()
                                  .AddHttpClientInstrumentation();
                             });

            return self.AddOpenTelemetryExporters();
        }
        public IHostApplicationBuilder AddOpenTelemetryExporters()
        {
            bool useOtlpExporter = !string.IsNullOrWhiteSpace(self.Configuration[OTEL_EXPORTER_OTLP_ENDPOINT]);

            if ( useOtlpExporter )
            {
                self.Services.Configure<OpenTelemetryLoggerOptions>(static logging => logging.AddOtlpExporter());
                self.Services.ConfigureOpenTelemetryMeterProvider(static metrics => metrics.AddOtlpExporter());
                self.Services.ConfigureOpenTelemetryTracerProvider(static tracing => tracing.AddOtlpExporter());
            }

            self.Services.AddOpenTelemetry()
                .WithMetrics(static x => x.AddPrometheusExporter());

            return self;
        }
    }



    private static LogLevel GetLogLevel( this bool isDevEnvironment ) =>
        isDevEnvironment
            ? LogLevel.Trace
            : LogLevel.Information;



    extension( ILoggingBuilder self )
    {
        public ILoggingBuilder AddDefaultLogging<TApp>( bool isDevEnvironment )
            where TApp : IAppName =>
            self.AddDefaultLogging<TApp>(isDevEnvironment.GetLogLevel());

        public ILoggingBuilder AddDefaultLogging<TApp>( in LogLevel minimumLevel )
            where TApp : IAppName =>
            self.AddDefaultLogging(minimumLevel, TApp.AppName);

        public ILoggingBuilder AddDefaultLogging( in LogLevel minimumLevel, in string name )
        {
            self.ClearProviders();
            self.SetMinimumLevel(minimumLevel);
            self.AddProvider(new DebugLoggerProvider());

            self.AddSimpleConsole(static options =>
                                  {
                                      options.ColorBehavior = LoggerColorBehavior.Enabled;
                                      options.SingleLine    = false;
                                      options.IncludeScopes = true;
                                  });


            if ( OperatingSystem.IsWindows() ) { self.AddProvider(name.GetEventLogLoggerProvider()); }
            else if ( OperatingSystem.IsLinux() ) { self.AddSystemdConsole(static options => options.UseUtcTimestamp = true); }

            return self;
        }
    }



    extension( string self )
    {
        [SupportedOSPlatform("Windows")] public EventLogLoggerProvider GetEventLogLoggerProvider()
        {
            return self.GetEventLogLoggerProvider(filter);
            static bool filter( string category, LogLevel level ) => level > LogLevel.Information;
        }
        [SupportedOSPlatform("Windows")] public EventLogLoggerProvider GetEventLogLoggerProvider( Func<string, LogLevel, bool> filter ) =>
            new(new EventLogSettings
                {
                    SourceName  = self,
                    LogName     = self,
                    MachineName = GetMachineName(),
                    Filter      = filter
                });
    }



    public static string GetMachineName()
    {
    #pragma warning disable RS1035
        try { return Environment.MachineName; }
        catch ( InvalidOperationException ) { return Dns.GetHostName(); }
    #pragma warning restore RS1035
    }



    extension( IServiceCollection self )
    {
        public IHealthChecksBuilder AddHealthCheck<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValue>()
            where TValue : IHealthCheck => self.AddHealthCheck(HealthChecks.Create<TValue>());

        public IHealthChecksBuilder AddHealthCheck( HealthCheckRegistration registration ) => self.AddHealthChecks()
                                                                                                  .Add(registration);
    }



    public static Assembly[] GetAssemblies<TApp>( params ReadOnlySpan<Assembly> assemblies )
        where TApp : IAppName
    {
        Assembly? entry = Assembly.GetEntryAssembly();

        return entry is not null
                   ? [typeof(TApp).Assembly, typeof(Database).Assembly, Assembly.GetExecutingAssembly(), entry, ..assemblies]
                   : [typeof(TApp).Assembly, typeof(Database).Assembly, Assembly.GetExecutingAssembly(), ..assemblies];
    }



    extension( WebApplicationBuilder self )
    {
        public WebApplicationBuilder AddDatabase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase>( DbOptions options )
            where TDatabase : Database => self.AddDatabase<TDatabase, UserStore, UserManager, RoleStore, RoleManager, SignInManager, UserValidator, RoleValidator, TokenProvider, UserPasswordValidator, DataProtectorTokenProvider, EmailTokenProvider, PhoneNumberTokenProvider, OtpAuthenticatorTokenProvider>(options);

        public WebApplicationBuilder AddDatabase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase, [DynamicallyAccessedMembers(                DynamicallyAccessedMemberTypes.PublicConstructors)] TUserStore, [DynamicallyAccessedMembers(                 DynamicallyAccessedMemberTypes.PublicConstructors)] TUserManager,
                                                 [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRoleStore, [DynamicallyAccessedMembers(               DynamicallyAccessedMemberTypes.PublicConstructors)] TRoleManager, [DynamicallyAccessedMembers(               DynamicallyAccessedMemberTypes.PublicConstructors)] TSignInManager,
                                                 [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TUserValidator, [DynamicallyAccessedMembers(           DynamicallyAccessedMemberTypes.PublicConstructors)] TRoleValidator, [DynamicallyAccessedMembers(             DynamicallyAccessedMemberTypes.PublicConstructors)] TTokenProvider,
                                                 [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TUserPasswordValidator, [DynamicallyAccessedMembers(   DynamicallyAccessedMemberTypes.PublicConstructors)] TDataProtectorTokenProvider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEmailTokenProvider,
                                                 [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPhoneNumberTokenProvider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAuthenticatorTokenProvider>( DbOptions options )
            where TDatabase : Database
            where TUserStore : UserStore
            where TUserManager : UserManager
            where TRoleStore : RoleStore
            where TRoleManager : RoleManager
            where TSignInManager : SignInManager
            where TUserValidator : UserValidator
            where TRoleValidator : RoleValidator
            where TTokenProvider : TokenProvider
            where TUserPasswordValidator : UserPasswordValidator
            where TDataProtectorTokenProvider : DataProtectorTokenProvider
            where TEmailTokenProvider : EmailTokenProvider
            where TPhoneNumberTokenProvider : PhoneNumberTokenProvider
            where TAuthenticatorTokenProvider : OtpAuthenticatorTokenProvider
        {
            self.Services.AddSingleton(options);
            self.Services.AddTransient<IOptions<DbOptions>>(static provider => provider.GetRequiredService<DbOptions>());

            self.AddOpenTelemetry<TestDatabase>(tracerOtlpExporter => { }, meterOtlpExporter => { });

            self.AddSerilog(options.LoggerOptions, Validate.ThrowIfNull(options.TelemetrySource), options.SeqConfig, out Logger logger);
            options.Serilogger = logger;

            options.ConfigureFusionCache(self.Services.AddFusionCache());

            self.Services.AddSingleton<TDatabase>();
            self.Services.AddTransient<Database>(static provider => provider.GetRequiredService<TDatabase>());
            self.Services.AddHealthCheck<TDatabase>();

            self.Services.AddIdentityServices<TUserStore, TUserManager, TRoleStore, TRoleManager, TSignInManager, TUserValidator, TRoleValidator, TTokenProvider, TUserPasswordValidator, TDataProtectorTokenProvider, TEmailTokenProvider, TPhoneNumberTokenProvider, TAuthenticatorTokenProvider>(options);

            self.Services.AddDataProtection();

            self.Services.AddPasswordValidator();

            self.Services.AddInMemoryTokenCaches();

            options.AddAuthentication(self);

            self.Services.AddAuthorizationBuilder()
                .RequireMultiFactorAuthentication();

            return self;
        }

        public ILoggingBuilder AddDefaultLogging<TApp>()
            where TApp : IAppName => self.Logging.AddDefaultLogging<TApp>(self.Environment.IsDevelopment());
    }



    extension( IServiceCollection self )
    {
        public void AddIdentityServices( DbOptions options ) => self.AddIdentityServices<UserStore, UserManager, RoleStore, RoleManager, SignInManager, UserValidator, RoleValidator, TokenProvider, UserPasswordValidator, DataProtectorTokenProvider, EmailTokenProvider, PhoneNumberTokenProvider, OtpAuthenticatorTokenProvider>(options);

        /// <summary>
        ///     <see href="https://stackoverflow.com/a/46775832/9530917"> Using ASP.NET Identity in an ASP.NET Core MVC application without Entity Framework and Migrations </see>
        ///     <para>
        ///         <see cref="AUTHENTICATION_SCHEME"/>
        ///     </para>
        /// </summary>
        public IdentityBuilder AddIdentityServices<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TUserStore, [DynamicallyAccessedMembers(                 DynamicallyAccessedMemberTypes.PublicConstructors)] TUserManager, [DynamicallyAccessedMembers(       DynamicallyAccessedMemberTypes.PublicConstructors)] TRoleStore,
                                                   [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRoleManager, [DynamicallyAccessedMembers(               DynamicallyAccessedMemberTypes.PublicConstructors)] TSignInManager, [DynamicallyAccessedMembers(     DynamicallyAccessedMemberTypes.PublicConstructors)] TUserValidator,
                                                   [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRoleValidator, [DynamicallyAccessedMembers(             DynamicallyAccessedMemberTypes.PublicConstructors)] TTokenProvider, [DynamicallyAccessedMembers(     DynamicallyAccessedMemberTypes.PublicConstructors)] TUserPasswordValidator,
                                                   [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDataProtectorTokenProvider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEmailTokenProvider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPhoneNumberTokenProvider,
                                                   [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAuthenticatorTokenProvider>( DbOptions options )
            where TUserStore : UserStore
            where TUserManager : UserManager
            where TRoleStore : RoleStore
            where TRoleManager : RoleManager
            where TSignInManager : SignInManager
            where TUserValidator : UserValidator
            where TRoleValidator : RoleValidator
            where TTokenProvider : TokenProvider
            where TUserPasswordValidator : UserPasswordValidator
            where TDataProtectorTokenProvider : DataProtectorTokenProvider
            where TEmailTokenProvider : EmailTokenProvider
            where TPhoneNumberTokenProvider : PhoneNumberTokenProvider
            where TAuthenticatorTokenProvider : OtpAuthenticatorTokenProvider
        {
            self.AddOptions(options.ConfigureIdentityOptions);

            self.AddUserStore<TUserStore>();
            self.AddRoleStore<TRoleStore>();


            return self.AddIdentity<UserRecord, RoleRecord>()
                       .AddUserStore<TUserStore>()
                       .AddUserManager<TUserManager>()
                       .AddRoleStore<TRoleStore>()
                       .AddRoleManager<TRoleManager>()
                       .AddSignInManager<TSignInManager>()
                       .AddUserValidator<TUserValidator>()
                       .AddRoleValidator<TRoleValidator>()
                       .AddPasswordValidator<TUserPasswordValidator>()
                       .AddTokenProvider(TokenOptions.DefaultProvider,              typeof(TDataProtectorTokenProvider))
                       .AddTokenProvider(TokenOptions.DefaultEmailProvider,         typeof(TEmailTokenProvider))
                       .AddTokenProvider(TokenOptions.DefaultPhoneProvider,         typeof(TPhoneNumberTokenProvider))
                       .AddTokenProvider(TokenOptions.DefaultAuthenticatorProvider, typeof(TAuthenticatorTokenProvider))
                       .AddTokenProvider<TTokenProvider>(nameof(TTokenProvider));
        }

        public IServiceCollection AddRoleStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRoleStore>()
            where TRoleStore : RoleStore
        {
            self.AddScoped<TRoleStore>();
            self.AddTransient<IRoleStore<RoleRecord>>(static provider => provider.GetRequiredService<TRoleStore>());
            return self;
        }

        public IServiceCollection AddUserStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TUserStore>()
            where TUserStore : UserStore
        {
            self.AddScoped<TUserStore>()
                .AddTransient<IUserStore>(getUserStore)
                .AddTransient<IUserStore<UserRecord>>(getUserStore)
                .AddTransient<IUserLoginStore<UserRecord>>(getUserStore)
                .AddTransient<IUserClaimStore<UserRecord>>(getUserStore)
                .AddTransient<IUserPasswordStore<UserRecord>>(getUserStore)
                .AddTransient<IUserSecurityStampStore<UserRecord>>(getUserStore)
                .AddTransient<IUserTwoFactorStore<UserRecord>>(getUserStore)
                .AddTransient<IUserEmailStore<UserRecord>>(getUserStore)
                .AddTransient<IUserLockoutStore<UserRecord>>(getUserStore)
                .AddTransient<IUserAuthenticatorKeyStore<UserRecord>>(getUserStore)
                .AddTransient<IUserTwoFactorRecoveryCodeStore<UserRecord>>(getUserStore)
                .AddTransient<IUserPhoneNumberStore<UserRecord>>(getUserStore);

            return self;

            static TUserStore getUserStore( IServiceProvider provider ) => provider.GetRequiredService<TUserStore>();
        }

        public IServiceCollection AddOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValue>( Action<TValue> options, string? name = null )
            where TValue : class
        {
            self.AddSingleton<TValue>();
            self.Configure(name ?? Options.DefaultName, options);
            self.AddTransient(getValue);
            return self;

            static IOptions<TValue> getValue( IServiceProvider provider )
            {
                TValue value = provider.GetRequiredService<TValue>();
                return value as IOptions<TValue> ?? Options.Create(value);
            }
        }
    }



    public static JwtBearerOptions GetJwtBearerOptions( this IServiceProvider provider )
    {
        JwtBearerOptions? bearer = provider.GetService<JwtBearerOptions>();
        if ( bearer is not null ) { return bearer; }

        IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
        DbOptions      options       = provider.GetRequiredService<DbOptions>();

        JwtBearerOptions jwt = new()
                               {
                                   Audience                  = options.TokenAudience,
                                   ClaimsIssuer              = options.TokenIssuer,
                                   TokenValidationParameters = configuration.GetTokenValidationParameters(options)
                               };

        jwt.TokenHandlers.Add(DbTokenHandler.Instance);
        return jwt;
    }



    extension( IServiceCollection self )
    {
        public IServiceCollection AddDataProtection()
        {
            DataProtectionServiceCollectionExtensions.AddDataProtection(self);
            ProtectedDataProvider.Register(self);
            return self;
        }

        public IServiceCollection AddEmailer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEmailer>( Action<EmailerOptions> options )
            where TEmailer : Emailer
        {
            self.AddOptions(options);
            self.AddScoped<TEmailer>();
            return self;
        }

        public IServiceCollection AddPasswordValidator()
        {
            self.AddTransient<IOptions<PasswordRequirements>>(static provider => PasswordRequirements.Current);
            self.AddScoped<UserPasswordValidator>();
            self.AddTransient<IPasswordValidator<UserRecord>>(static provider => provider.GetRequiredService<UserPasswordValidator>());
            return self;
        }
    }



    public static AuthorizationBuilder RequireMultiFactorAuthentication( this AuthorizationBuilder builder ) => builder.AddPolicy(nameof(RequireMfa), static policy => policy.Requirements.Add(RequireMfa.Instance));
}
