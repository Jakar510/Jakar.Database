// Jakar.Extensions :: Jakar.Database
// 05/22/2023  11:24 AM

namespace Jakar.Database;


public static class LoggingExtensions
{
    public const string CONNECTION_STRINGS = "ConnectionStrings";
    public const string DEFAULT            = "Default";


    public static string ConnectionString( this IConfiguration configuration, string name = DEFAULT ) => configuration.GetSection(CONNECTION_STRINGS)
                                                                                                                      .GetValue<string>(name) ??
                                                                                                         throw new InvalidOperationException($"{CONNECTION_STRINGS}::{DEFAULT} is not found");
    public static string ConnectionString( this IServiceProvider provider, string name = DEFAULT ) => provider.GetRequiredService<IConfiguration>()
                                                                                                              .ConnectionString(name);
    public static string ConnectionString( this WebApplication configuration, string name = DEFAULT ) => configuration.Services.ConnectionString(name);



    extension( WebApplicationBuilder self )
    {
        public IConfigurationBuilder AddCommandLine( string[]                               args )                                             => self.Configuration.AddCommandLine(args);
        public IConfigurationBuilder AddCommandLine( string[]                               args, IDictionary<string, string> switchMappings ) => self.Configuration.AddCommandLine(args, switchMappings);
        public IConfigurationBuilder AddCommandLine( Action<CommandLineConfigurationSource> configureSource ) => self.Configuration.AddCommandLine(configureSource);
        public IConfigurationBuilder AddEnvironmentVariables()                                                => self.Configuration.AddEnvironmentVariables();
        public IConfigurationBuilder AddEnvironmentVariables( string prefix )                                 => self.Configuration.AddEnvironmentVariables(prefix);
        public IConfigurationBuilder AddEnvironmentVariables( params ReadOnlySpan<string> prefix )
        {
            foreach ( string s in prefix ) { self.Configuration.AddEnvironmentVariables(s); }

            return self.Configuration;
        }
        public IConfigurationBuilder AddIniFile( string                           path )                                                          => self.Configuration.AddIniFile(path);
        public IConfigurationBuilder AddIniFile( string                           path,     bool   optional )                                     => self.Configuration.AddIniFile(path,     optional);
        public IConfigurationBuilder AddIniFile( string                           path,     bool   optional, bool reloadOnChange )                => self.Configuration.AddIniFile(path,     optional, reloadOnChange);
        public IConfigurationBuilder AddIniFile( IFileProvider                    provider, string path,     bool optional, bool reloadOnChange ) => self.Configuration.AddIniFile(provider, path,     optional, reloadOnChange);
        public IConfigurationBuilder AddIniFile( Action<IniConfigurationSource>   configureSource )                                               => self.Configuration.AddIniFile(configureSource);
        public IConfigurationBuilder AddIniStream( Stream                         stream )                                                        => self.Configuration.AddIniStream(stream);
        public IConfigurationBuilder AddJsonFile( string                          path )                                                          => self.Configuration.AddJsonFile(path);
        public IConfigurationBuilder AddJsonFile( string                          path,     bool   optional )                                     => self.Configuration.AddJsonFile(path,     optional);
        public IConfigurationBuilder AddJsonFile( string                          path,     bool   optional, bool reloadOnChange )                => self.Configuration.AddJsonFile(path,     optional, reloadOnChange);
        public IConfigurationBuilder AddJsonFile( IFileProvider                   provider, string path,     bool optional, bool reloadOnChange ) => self.Configuration.AddJsonFile(provider, path,     optional, reloadOnChange);
        public IConfigurationBuilder AddJsonFile( Action<JsonConfigurationSource> configureSource ) => self.Configuration.AddJsonFile(configureSource);
        public IConfigurationBuilder AddJsonStream( Stream                        stream )          => self.Configuration.AddJsonStream(stream);
        public ILoggingBuilder AddDefaultLogging<TValue>()
            where TValue : class =>
            self.AddDefaultLogging<TValue>(self.Environment.EnvironmentName == Environments.Development);
        public ILoggingBuilder AddDefaultLogging<TValue>( bool isDevEnvironment )
            where TValue : class =>
            self.AddDefaultLogging<TValue>(isDevEnvironment
                                               ? LogLevel.Trace
                                               : LogLevel.Information);
        public ILoggingBuilder AddDefaultLogging<TValue>( in LogLevel minimumLevel )
            where TValue : class =>
            self.AddDefaultLogging(minimumLevel, typeof(TValue).Name);
        public ILoggingBuilder AddDefaultLogging( in LogLevel minimumLevel, in string name )
        {
            self.Logging.ClearProviders();
            self.Logging.SetMinimumLevel(minimumLevel);
            self.Logging.AddProvider(new DebugLoggerProvider());

            self.Logging.AddSimpleConsole(options =>
                                          {
                                              options.ColorBehavior = LoggerColorBehavior.Enabled;
                                              options.SingleLine    = false;
                                              options.IncludeScopes = true;
                                          });


            if ( OperatingSystem.IsWindows() )
            {
                self.Logging.AddProvider(new EventLogLoggerProvider(new EventLogSettings
                                                                    {
                                                                        SourceName  = name,
                                                                        LogName     = name,
                                                                        MachineName = GetMachineName(),
                                                                        Filter      = ( category, level ) => level > LogLevel.Information
                                                                    }));
            }
            else { self.Logging.AddSystemdConsole(options => options.UseUtcTimestamp = true); }


            return self.Logging;
        }
    }



    public static string GetMachineName()
    {
    #pragma warning disable RS1035
        try { return Environment.MachineName; }
        catch ( InvalidOperationException ) { return Dns.GetHostName(); }
    #pragma warning restore RS1035
    }
}
