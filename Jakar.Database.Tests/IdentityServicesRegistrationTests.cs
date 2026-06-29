using System.Data.Common;
using Jakar.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using ZiggyCreatures.Caching.Fusion;



namespace Jakar.Database.Tests;


[TestFixture]
public sealed class IdentityServicesRegistrationTests
{
    [SetUp] public void SetUp() => TrackingProviders.Reset();


    [Test] public void AddIdentityServices_registers_identity_managers_stores_and_interfaces()
    {
        using ServiceProvider provider = CreateProvider(static ( services, options ) => services.AddIdentityServices(options));
        using IServiceScope   scope    = provider.CreateScope();

        IUserStore<UserRecord>                      userStore              = scope.ServiceProvider.GetRequiredService<IUserStore<UserRecord>>();
        IRoleStore<RoleRecord>                      roleStore              = scope.ServiceProvider.GetRequiredService<IRoleStore<RoleRecord>>();
        UserManager<UserRecord>                     userManager            = scope.ServiceProvider.GetRequiredService<UserManager<UserRecord>>();
        RoleManager<RoleRecord>                     roleManager            = scope.ServiceProvider.GetRequiredService<RoleManager<RoleRecord>>();
        SignInManager<UserRecord>                   signInManager          = scope.ServiceProvider.GetRequiredService<SignInManager<UserRecord>>();
        IUserValidator<UserRecord>                  userValidator          = scope.ServiceProvider.GetRequiredService<IUserValidator<UserRecord>>();
        IRoleValidator<RoleRecord>                  roleValidator          = scope.ServiceProvider.GetRequiredService<IRoleValidator<RoleRecord>>();
        IPasswordValidator<UserRecord>              passwordValidator      = scope.ServiceProvider.GetRequiredService<IPasswordValidator<UserRecord>>();
        IDataProtectionProvider                     dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        IHttpContextAccessor                        httpContextAccessor    = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        TelemetrySource                             telemetrySource        = scope.ServiceProvider.GetRequiredService<TelemetrySource>();
        IUserLoginStore<UserRecord>                 userLoginStore         = scope.ServiceProvider.GetRequiredService<IUserLoginStore<UserRecord>>();
        IUserClaimStore<UserRecord>                 userClaimStore         = scope.ServiceProvider.GetRequiredService<IUserClaimStore<UserRecord>>();
        IUserPasswordStore<UserRecord>              userPasswordStore      = scope.ServiceProvider.GetRequiredService<IUserPasswordStore<UserRecord>>();
        IUserSecurityStampStore<UserRecord>         securityStampStore     = scope.ServiceProvider.GetRequiredService<IUserSecurityStampStore<UserRecord>>();
        IUserTwoFactorStore<UserRecord>             userTwoFactorStore     = scope.ServiceProvider.GetRequiredService<IUserTwoFactorStore<UserRecord>>();
        IUserEmailStore<UserRecord>                 userEmailStore         = scope.ServiceProvider.GetRequiredService<IUserEmailStore<UserRecord>>();
        IUserLockoutStore<UserRecord>               userLockoutStore       = scope.ServiceProvider.GetRequiredService<IUserLockoutStore<UserRecord>>();
        IUserAuthenticatorKeyStore<UserRecord>      authenticatorKeyStore  = scope.ServiceProvider.GetRequiredService<IUserAuthenticatorKeyStore<UserRecord>>();
        IUserTwoFactorRecoveryCodeStore<UserRecord> recoveryCodeStore      = scope.ServiceProvider.GetRequiredService<IUserTwoFactorRecoveryCodeStore<UserRecord>>();
        IUserPhoneNumberStore<UserRecord>           userPhoneNumberStore   = scope.ServiceProvider.GetRequiredService<IUserPhoneNumberStore<UserRecord>>();


        Assert.Multiple(() =>
                        {
                            Assert.That(userStore,              Is.TypeOf<UserStore>());
                            Assert.That(roleStore,              Is.TypeOf<RoleStore>());
                            Assert.That(userManager,            Is.TypeOf<UserManager>());
                            Assert.That(roleManager,            Is.TypeOf<RoleManager>());
                            Assert.That(signInManager,          Is.TypeOf<SignInManager>());
                            Assert.That(userValidator,          Is.TypeOf<UserValidator>());
                            Assert.That(roleValidator,          Is.TypeOf<RoleValidator>());
                            Assert.That(passwordValidator,      Is.TypeOf<UserPasswordValidator>());
                            Assert.That(dataProtectionProvider, Is.Not.Null);
                            Assert.That(httpContextAccessor,    Is.Not.Null);
                            Assert.That(telemetrySource,        Is.SameAs(scope.ServiceProvider.GetRequiredService<FakeDatabase>().Options.TelemetrySource));
                            Assert.That(userLoginStore,         Is.TypeOf<UserStore>());
                            Assert.That(userClaimStore,         Is.TypeOf<UserStore>());
                            Assert.That(userPasswordStore,      Is.TypeOf<UserStore>());
                            Assert.That(securityStampStore,     Is.TypeOf<UserStore>());
                            Assert.That(userTwoFactorStore,     Is.TypeOf<UserStore>());
                            Assert.That(userEmailStore,         Is.TypeOf<UserStore>());
                            Assert.That(userLockoutStore,       Is.TypeOf<UserStore>());
                            Assert.That(authenticatorKeyStore,  Is.TypeOf<UserStore>());
                            Assert.That(recoveryCodeStore,      Is.TypeOf<UserStore>());
                            Assert.That(userPhoneNumberStore,   Is.TypeOf<UserStore>());
                        });
    }

    [Test] public async Task AddIdentityServices_supports_default_identity_token_workflows()
    {
        using ServiceProvider provider = CreateProvider(static ( services, options ) => services.AddIdentityServices(options));
        using IServiceScope   scope    = provider.CreateScope();

        UserManager<UserRecord> manager = scope.ServiceProvider.GetRequiredService<UserManager<UserRecord>>();
        IdentityOptions         options = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        UserRecord              user    = CreateUser();

        string passwordResetToken = await manager.GeneratePasswordResetTokenAsync(user);
        string emailToken         = await manager.GenerateEmailConfirmationTokenAsync(user);
        string authenticatorToken = await manager.GenerateTwoFactorTokenAsync(user, options.Tokens.AuthenticatorTokenProvider);

        Assert.Multiple(() =>
                        {
                            Assert.That(passwordResetToken,                            Is.Not.Null.And.Not.Empty);
                            Assert.That(emailToken,                                    Is.Not.Null.And.Not.Empty);
                            Assert.That(authenticatorToken,                            Is.Not.Null.And.Not.Empty);
                            Assert.That(options.Tokens.PasswordResetTokenProvider,     Is.EqualTo(TokenOptions.DefaultProvider));
                            Assert.That(options.Tokens.EmailConfirmationTokenProvider, Is.EqualTo(TokenOptions.DefaultEmailProvider));
                            Assert.That(options.Tokens.ChangeEmailTokenProvider,       Is.EqualTo(TokenOptions.DefaultEmailProvider));
                            Assert.That(options.Tokens.ChangePhoneNumberTokenProvider, Is.EqualTo(TokenOptions.DefaultPhoneProvider));
                            Assert.That(options.Tokens.AuthenticatorTokenProvider,     Is.EqualTo(TokenOptions.DefaultAuthenticatorProvider));
                        });
    }

    [Test] public async Task AddIdentityServices_generic_overload_uses_custom_identity_types_and_token_providers()
    {
        using ServiceProvider provider = CreateProvider(static ( services, options ) => services
                                                           .AddIdentityServices<TrackingUserStore, TrackingUserManager, TrackingRoleStore, TrackingRoleManager, TrackingSignInManager, TrackingUserValidator, TrackingRoleValidator, TrackingTokenProvider, TrackingUserPasswordValidator, TrackingDataProtectorTokenProvider, TrackingEmailTokenProvider, TrackingPhoneNumberTokenProvider,
                                                                TrackingOtpAuthenticatorTokenProvider>(options));

        using IServiceScope scope = provider.CreateScope();

        UserManager<UserRecord>   manager         = scope.ServiceProvider.GetRequiredService<UserManager<UserRecord>>();
        SignInManager<UserRecord> signInManager   = scope.ServiceProvider.GetRequiredService<SignInManager<UserRecord>>();
        RoleManager<RoleRecord>   roleManager     = scope.ServiceProvider.GetRequiredService<RoleManager<RoleRecord>>();
        IUserStore<UserRecord>    userStore       = scope.ServiceProvider.GetRequiredService<IUserStore<UserRecord>>();
        IRoleStore<RoleRecord>    roleStore       = scope.ServiceProvider.GetRequiredService<IRoleStore<RoleRecord>>();
        IdentityOptions           identityOptions = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        DbOptions                 dbOptions       = scope.ServiceProvider.GetRequiredService<DbOptions>();
        UserRecord                user            = CreateUser();

        string passwordResetToken = await manager.GeneratePasswordResetTokenAsync(user);
        string emailToken         = await manager.GenerateEmailConfirmationTokenAsync(user);
        string authenticatorToken = await manager.GenerateTwoFactorTokenAsync(user, identityOptions.Tokens.AuthenticatorTokenProvider);
        string namedCustomToken   = await manager.GenerateUserTokenAsync(user, nameof(TrackingTokenProvider),    "custom-purpose");
        string appNamedToken      = await manager.GenerateUserTokenAsync(user, dbOptions.AppInformation.AppName, "custom-purpose");

        Assert.Multiple(() =>
                        {
                            Assert.That(userStore,                                    Is.TypeOf<TrackingUserStore>());
                            Assert.That(roleStore,                                    Is.TypeOf<TrackingRoleStore>());
                            Assert.That(manager,                                      Is.TypeOf<TrackingUserManager>());
                            Assert.That(roleManager,                                  Is.TypeOf<TrackingRoleManager>());
                            Assert.That(signInManager,                                Is.TypeOf<TrackingSignInManager>());
                            Assert.That(passwordResetToken,                           Is.EqualTo(TrackingProviders.DATA_PROTECTOR_TOKEN));
                            Assert.That(emailToken,                                   Is.EqualTo(TrackingProviders.EMAIL_TOKEN));
                            Assert.That(authenticatorToken,                           Is.EqualTo(TrackingProviders.AUTHENTICATOR_TOKEN));
                            Assert.That(namedCustomToken,                             Is.EqualTo(TrackingProviders.CUSTOM_TOKEN));
                            Assert.That(appNamedToken,                                Is.EqualTo(TrackingProviders.CUSTOM_TOKEN));
                            Assert.That(TrackingProviders.DataProtectorGenerateCalls, Is.EqualTo(1));
                            Assert.That(TrackingProviders.EmailGenerateCalls,         Is.EqualTo(1));
                            Assert.That(TrackingProviders.AuthenticatorGenerateCalls, Is.EqualTo(1));
                            Assert.That(TrackingProviders.CustomGenerateCalls,        Is.EqualTo(2));
                        });
    }

    [Test] public async Task UserValidator_accepts_non_empty_user_names_and_rejects_missing_user_names()
    {
        using ServiceProvider provider = CreateProvider(static ( services, options ) => services.AddIdentityServices(options));
        using IServiceScope   scope    = provider.CreateScope();

        UserManager<UserRecord> manager       = scope.ServiceProvider.GetRequiredService<UserManager<UserRecord>>();
        UserValidator           validator     = (UserValidator)scope.ServiceProvider.GetRequiredService<IUserValidator<UserRecord>>();
        IdentityResult          validResult   = await validator.ValidateAsync(manager, CreateUser());
        IdentityResult          invalidResult = await validator.ValidateAsync(manager, CreateUser() with { UserName = string.Empty });

        Assert.Multiple(() =>
                        {
                            Assert.That(validResult.Succeeded,                                  Is.True);
                            Assert.That(invalidResult.Succeeded,                                Is.False);
                            Assert.That(invalidResult.Errors.Select(static x => x.Description), Does.Contain($"{nameof(UserRecord.UserName)} is invalid"));
                        });
    }


    private static ServiceProvider CreateProvider( Action<IServiceCollection, DbOptions> registerIdentity )
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [DbOptions.JWT_KEY] = "identity-services-tests-secret-key-with-enough-length-for-hmac-sha512-signatures" }).Build();

        DbOptions options = CreateOptions();

        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddSingleton(configuration);
        services.AddSingleton(options);
        services.AddSingleton<IOptions<DbOptions>>(Options.Create(options));
        options.ConfigureFusionCache(services.AddFusionCache());
        services.AddSingleton<FakeDatabase>();
        services.AddSingleton<Database>(static provider => provider.GetRequiredService<FakeDatabase>());

        registerIdentity(services, options);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static DbOptions CreateOptions()
    {
        SecuredStringResolverOptions connectionString = "Host=localhost;Port=5432;Database=identity_tests;Username=dev;Password=dev";

        return new DbOptions
               {
                   TelemetrySource          = new TelemetrySource(AppVersion.Default, Guid.NewGuid(), nameof(IdentityServicesRegistrationTests), typeof(IdentityServicesRegistrationTests).Assembly.FullName),
                   ConnectionStringResolver = connectionString,
                   CommandTimeout           = 30,
                   TokenIssuer              = nameof(IdentityServicesRegistrationTests),
                   TokenAudience            = nameof(IdentityServicesRegistrationTests),
                   LoggerOptions            = new AppLoggerOptions()
               };
    }
    private static UserRecord CreateUser()
    {
        UserRecord user = UserRecord.Create("identity-user", "P@ssword123!", new UserRights(string.Empty));
        user.Email                  = "identity-user@example.com";
        user.PhoneNumber            = "5551234567";
        user.SecurityStamp          = Guid.CreateVersion7().ToString();
        user.AuthenticatorKey       = "authenticator-key";
        user.IsEmailConfirmed       = true;
        user.IsPhoneNumberConfirmed = true;
        return user;
    }



    private sealed class FakeDatabase( IConfiguration configuration, IOptions<DbOptions> options, IFusionCache cache ) : Database(configuration, options, cache)
    {
        public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;


        protected override DbConnection CreateConnection( in ConnectionString secure ) => new NpgsqlConnection(secure);
    }



    private static class TrackingProviders
    {
        public const string DATA_PROTECTOR_TOKEN = "tracking-data-protector-token";
        public const string EMAIL_TOKEN          = "tracking-email-token";
        public const string AUTHENTICATOR_TOKEN  = "tracking-authenticator-token";
        public const string CUSTOM_TOKEN         = "tracking-custom-token";


        public static int DataProtectorGenerateCalls { get; set; }
        public static int EmailGenerateCalls         { get; set; }
        public static int AuthenticatorGenerateCalls { get; set; }
        public static int CustomGenerateCalls        { get; set; }


        public static void Reset()
        {
            DataProtectorGenerateCalls = 0;
            EmailGenerateCalls         = 0;
            AuthenticatorGenerateCalls = 0;
            CustomGenerateCalls        = 0;
        }
    }



    private sealed class TrackingUserStore( Database dbContext ) : UserStore(dbContext);



    private sealed class TrackingRoleStore( Database dbContext ) : RoleStore(dbContext);



    private sealed class TrackingUserManager( Database                                    database,
                                              TrackingUserStore                           store,
                                              IOptions<IdentityOptions>                   optionsAccessor,
                                              IPasswordHasher<UserRecord>                 passwordHasher,
                                              IEnumerable<IUserValidator<UserRecord>>     userValidators,
                                              IEnumerable<IPasswordValidator<UserRecord>> passwordValidators,
                                              ILookupNormalizer                           keyNormalizer,
                                              IdentityErrorDescriber                      errors,
                                              IServiceProvider                            services,
                                              ILogger<UserManager>                        logger ) : UserManager(database, store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger);



    private sealed class TrackingRoleManager( TrackingRoleStore store, IEnumerable<IRoleValidator<RoleRecord>> roleValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, ILogger<RoleManager> logger ) : RoleManager(store, roleValidators, keyNormalizer, errors, logger);



    private sealed class TrackingSignInManager( TrackingUserManager userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<UserRecord> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<UserRecord> confirmation )
        : SignInManager(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation);



    private sealed class TrackingUserValidator : UserValidator;



    private sealed class TrackingRoleValidator : RoleValidator;



    private sealed class TrackingUserPasswordValidator( IOptions<PasswordRequirements> options ) : UserPasswordValidator(options);



    private sealed class TrackingTokenProvider( Database database ) : TokenProvider(database)
    {
        public override Task<string> GenerateAsync( string purpose, UserManager<UserRecord> manager, UserRecord user )
        {
            TrackingProviders.CustomGenerateCalls++;
            return Task.FromResult(TrackingProviders.CUSTOM_TOKEN);
        }
        public override Task<bool> ValidateAsync( string                                   purpose, string     token, UserManager<UserRecord> manager, UserRecord user ) => Task.FromResult(string.Equals(token, TrackingProviders.CUSTOM_TOKEN, StringComparison.Ordinal));
        public override Task<bool> CanGenerateTwoFactorTokenAsync( UserManager<UserRecord> manager, UserRecord user ) => Task.FromResult(true);
    }



    private sealed class TrackingDataProtectorTokenProvider( IDataProtectionProvider dataProtectionProvider, IOptions<DataProtectionTokenProviderOptions> options, ILogger<DataProtectorTokenProvider<UserRecord>> logger ) : DataProtectorTokenProvider(dataProtectionProvider, options, logger)
    {
        public override Task<string> GenerateAsync( string purpose, UserManager<UserRecord> manager, UserRecord user )
        {
            TrackingProviders.DataProtectorGenerateCalls++;
            return Task.FromResult(TrackingProviders.DATA_PROTECTOR_TOKEN);
        }
        public override Task<bool> ValidateAsync( string purpose, string token, UserManager<UserRecord> manager, UserRecord user ) => Task.FromResult(string.Equals(token, TrackingProviders.DATA_PROTECTOR_TOKEN, StringComparison.Ordinal));
    }



    private sealed class TrackingEmailTokenProvider : EmailTokenProvider
    {
        public override Task<string> GenerateAsync( string purpose, UserManager<UserRecord> manager, UserRecord user )
        {
            TrackingProviders.EmailGenerateCalls++;
            return Task.FromResult(TrackingProviders.EMAIL_TOKEN);
        }
        public override Task<bool> ValidateAsync( string purpose, string token, UserManager<UserRecord> manager, UserRecord user ) => Task.FromResult(string.Equals(token, TrackingProviders.EMAIL_TOKEN, StringComparison.Ordinal));
    }



    private sealed class TrackingPhoneNumberTokenProvider : PhoneNumberTokenProvider;



    private sealed class TrackingOtpAuthenticatorTokenProvider( TelemetrySource source ) : OtpAuthenticatorTokenProvider(source)
    {
        public override Task<string> GenerateAsync( string purpose, UserManager<UserRecord> manager, UserRecord user )
        {
            TrackingProviders.AuthenticatorGenerateCalls++;
            return Task.FromResult(TrackingProviders.AUTHENTICATOR_TOKEN);
        }
        public override Task<bool> ValidateAsync( string purpose, string token, UserManager<UserRecord> manager, UserRecord user ) => Task.FromResult(string.Equals(token, TrackingProviders.AUTHENTICATOR_TOKEN, StringComparison.Ordinal));
    }
}
