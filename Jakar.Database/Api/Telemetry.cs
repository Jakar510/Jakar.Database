// Jakar.Extensions :: Jakar.Database
// 12/02/2023  12:12 PM

using System.Security.Cryptography.X509Certificates;
using OpenTelemetry;
using OpenTelemetry.Resources;



namespace Jakar.Database;


public static class Telemetry
{
    public const           string                          ATTRIBUTE_SERVICE_INSTANCE       = "service.instance.id";
    public const           string                          ATTRIBUTE_SERVICE_NAME           = "service.name";
    public const           string                          ATTRIBUTE_SERVICE_NAMESPACE      = "service.namespace";
    public const           string                          ATTRIBUTE_SERVICE_VERSION        = "service.version";
    public const           string                          ATTRIBUTE_TELEMETRY_SDK_LANGUAGE = "telemetry.sdk.language";
    public const           string                          ATTRIBUTE_TELEMETRY_SDK_NAME     = "telemetry.sdk.name";
    public const           string                          ATTRIBUTE_TELEMETRY_SDK_VERSION  = "telemetry.sdk.version";
    public const           string                          METER_NAME                       = "Jakar.Database";
    public static readonly Version                         DefaultVersion                   = new(1, 0, 0);
    public static readonly KeyValuePair<string, object?>[] Pairs                            = [];
    public static readonly Meter                           DbMeter                          = CreateMeter(METER_NAME);
    public static readonly ActivitySource                  DbSource                         = CreateSource(METER_NAME);


    public static readonly ConcurrentDictionary<string, Instrument> Instruments = [];
    public static readonly ConcurrentDictionary<string, Meter>      Meters      = [];


    public static Meter          Meter  { get; set; } = DbMeter;
    public static ActivitySource Source { get; set; } = DbSource;



    extension( OtlpExporterOptions exporter )
    {
        public void ConfigureExporter( Uri endpoint, ExportProcessorType type, OtlpExportProtocol protocol, string? headers = null, int timeout = 10000 )
        {
            exporter.Endpoint            = endpoint;
            exporter.ExportProcessorType = type;
            exporter.Protocol            = protocol;
            exporter.TimeoutMilliseconds = timeout;
            exporter.Headers             = headers;
        }
        public void ConfigureExporter( BatchExportProcessorOptions<Activity> processor ) => exporter.BatchExportProcessorOptions = processor;
        public void ConfigureExporter( Func<HttpClient>                      factory )   => exporter.HttpClientFactory = factory;
    }



    extension( WebApplication self )
    {
        public WebApplication UseDefaults() => self.UseDefaults("/_metrics");
        public WebApplication UseDefaults( OneOf<string, Func<HttpContext, bool>, (MeterProvider meterProvider, Func<HttpContext, bool> predicate, Action<IApplicationBuilder> configureBranchedPipeline, string optionsName, string path)> telemetry )
        {
            self.UseStaticFiles();
            self.UseRouting();
            self.UseHttpMetrics();
            self.UseAuthentication();
            self.UseAuthorization();
            telemetry.Switch(path => self.UseTelemetry(path), predicate => self.UseTelemetry(predicate), t => self.UseTelemetry(t.meterProvider, t.predicate, t.configureBranchedPipeline, t.optionsName, t.path));
            return self;
        }
        public WebApplication UseTelemetry( string path = "/metrics" )
        {
            self.UseOpenTelemetryPrometheusScrapingEndpoint(path);
            return self;
        }
        public WebApplication UseTelemetry( Func<HttpContext, bool> predicate )
        {
            self.UseOpenTelemetryPrometheusScrapingEndpoint(predicate);
            return self;
        }
        public WebApplication UseTelemetry( MeterProvider meterProvider, Func<HttpContext, bool> predicate, Action<IApplicationBuilder> configureBranchedPipeline, string optionsName, string path = "/metrics" )
        {
            self.UseOpenTelemetryPrometheusScrapingEndpoint(meterProvider, predicate, path, configureBranchedPipeline, optionsName);
            return self;
        }
    }



    extension( WebApplicationBuilder self )
    {
        public WebApplicationBuilder AddSerilog( AppLoggerOptions options, TelemetrySource source, SeqConfig? seqConfig, out Logger logger )
        {
            LoggerConfiguration config = new();
            config.MinimumLevel.Verbose();
            config.MinimumLevel.Override(nameof(Microsoft), LogEventLevel.Warning);
            config.Enrich.FromLogContext();

            OpenTelemetryActivityEnricher enricher = new(options, source);
            config.Enrich.With(enricher);


            config.WriteTo.Console();

            seqConfig?.Configure(config.WriteTo);

            logger     = config.CreateLogger();
            Log.Logger = logger;
            return self;
        }
        public WebApplicationBuilder AddOpenTelemetry<TApp>( Action<OtlpExporterOptions> tracerOtlpExporter, Action<OtlpExporterOptions> meterOtlpExporter, Action<LoggerProviderBuilder>? configureBuilder = null, Action<OpenTelemetryLoggerOptions>? configureOptions = null )
            where TApp : IAppID
        {
            KeyValuePair<string, object>[] attributes = [new(ATTRIBUTE_SERVICE_NAME, TApp.AppName), new(ATTRIBUTE_SERVICE_VERSION, TApp.AppVersion.ToString()), new(ATTRIBUTE_SERVICE_INSTANCE, TApp.AppID.ToString()), new(ATTRIBUTE_SERVICE_NAMESPACE, typeof(TApp).Namespace ?? EMPTY)];

            ResourceBuilder resources = ResourceBuilder.CreateEmpty()
                                                       .AddAttributes(attributes)
                                                       .AddTelemetrySdk()
                                                       .AddEnvironmentVariableDetector()
                                                       .AddService(TApp.AppName, null, TApp.AppVersion.ToString())
                                                       .AddService(METER_NAME);


            self.Services.AddOpenTelemetry()
                .WithTracing(configureTracing)
                .WithMetrics(configureMetrics)
                .WithLogging(configureBuilder, configureOptions);

            self.Services.AddOpenApi();
            return self;

            void configureMetrics( MeterProviderBuilder meterProviderBuilder )
            {
                meterProviderBuilder.AddAspNetCoreInstrumentation()
                                    .AddRuntimeInstrumentation()
                                    .AddHttpClientInstrumentation()
                                    .AddNpgsqlInstrumentation() // PostgreSQL metrics for Dselfer
                                    .AddFusionCacheInstrumentation()
                                    .AddMeter(METER_NAME)
                                    .SetResourceBuilder(resources)
                                    .AddOtlpExporter(meterOtlpExporter)
                                    .AddConsoleExporter();
            }

            void configureTracing( TracerProviderBuilder tracerProviderBuilder )
            {
                tracerProviderBuilder.AddAspNetCoreInstrumentation()
                                     .AddHttpClientInstrumentation()
                                     .AddNpgsql() // PostgreSQL tracing for Dselfer
                                     .AddFusionCacheInstrumentation()
                                     .AddSource(METER_NAME)
                                     .SetResourceBuilder(resources)
                                     .AddOtlpExporter(tracerOtlpExporter)
                                     .AddConsoleExporter();
            }
        }
    }



    public static IMetricServer Server( int port, string url = "/metrics", CollectorRegistry? registry = null, X509Certificate2? certificate = null )
    {
        KestrelMetricServer server = new(port, url, registry, certificate);
        return server.Start();
    }
    public static IMetricServer Server( KestrelMetricServerOptions options )
    {
        KestrelMetricServer server = new(options);
        return server.Start();
    }


    public static ActivitySource CreateSource()                                           => CreateSource(GetAssembly());
    public static ActivitySource CreateSource( Assembly     assembly )                    => CreateSource(assembly.GetName());
    public static ActivitySource CreateSource( AssemblyName assembly )                    => CreateSource(assembly.Name ?? nameof(Database), assembly);
    public static ActivitySource CreateSource( string       name )                        => CreateSource(name,                              GetAssembly());
    public static ActivitySource CreateSource( string       name, Assembly     assembly ) => CreateSource(name,                              assembly.GetName());
    public static ActivitySource CreateSource( string       name, AssemblyName assembly ) => CreateSource(name,                              assembly.GetVersion());
    public static ActivitySource CreateSource( string       name, AppVersion   version )  => CreateSource(name,                              version.ToString());
    public static ActivitySource CreateSource( string       name, string       version )  => new(name, version);
    public static Meter          CreateMeter()                                                                                                                         => CreateMeter(GetAssembly());
    public static Meter          CreateMeter( Assembly     assembly )                                                                                                  => CreateMeter(assembly.GetName());
    public static Meter          CreateMeter( AssemblyName assembly )                                                                                                  => CreateMeter(assembly.Name ?? nameof(Database), assembly);
    public static Meter          CreateMeter( string       name )                                                                                                      => CreateMeter(name,                              GetAssembly());
    public static Meter          CreateMeter( string       name, Assembly     assembly )                                                                               => CreateMeter(name,                              assembly.GetName());
    public static Meter          CreateMeter( string       name, AssemblyName assembly )                                                                               => CreateMeter(name,                              assembly.GetVersion());
    public static Meter          CreateMeter( string       name, AppVersion   version, IEnumerable<KeyValuePair<string, object?>>? tags = null, object? scope = null ) => CreateMeter(name,                              version.ToString(), tags, scope);
    public static Meter          CreateMeter( string       name, string?      version, IEnumerable<KeyValuePair<string, object?>>? tags = null, object? scope = null ) => new(name, version, tags, scope);
    public static Assembly       GetAssembly() => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
    public static AppVersion GetVersion( this Assembly assembly ) => assembly.GetName()
                                                                             .GetVersion();
    public static AppVersion GetVersion( this AssemblyName assembly ) => assembly.Version ?? DefaultVersion;


    public static Meter GetOrAddMeter( [CallerMemberName] string meterName = EMPTY ) => Meters.GetOrAdd(meterName, CreateMeter);


    public static Histogram<TValue> GetOrAdd<TValue>( string unit, string description, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(description, out Instrument? value) && value is Histogram<TValue> instrument ) { return instrument; }

        Instruments[description] = instrument = GetOrAddMeter(meterName)
                                      .CreateHistogram<TValue>(meterName, unit, description, tags ?? Pairs);

        return instrument;
    }


    public static ObservableGauge<TValue> GetOrAddGauge<TValue>( string name, Func<Measurement<TValue>> observeValue, string? unit, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(name, out Instrument? value) && value is ObservableGauge<TValue> instrument ) { return instrument; }

        Instruments[name] = instrument = GetOrAddMeter(meterName)
                               .CreateObservableGauge(name, observeValue, unit, description, tags ?? Pairs);

        return instrument;
    }
    public static ObservableGauge<TValue> GetOrAddGauge<TValue>( string name, Func<IEnumerable<Measurement<TValue>>> observeValue, string? unit, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(name, out Instrument? value) && value is ObservableGauge<TValue> instrument ) { return instrument; }

        Instruments[name] = instrument = GetOrAddMeter(meterName)
                               .CreateObservableGauge(name, observeValue, unit, description, tags ?? Pairs);

        return instrument;
    }


    public static Counter<TValue> GetOrAddCounter<TValue>( string name, string? unit, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(name, out Instrument? value) && value is Counter<TValue> instrument ) { return instrument; }

        Instruments[name] = instrument = GetOrAddMeter(meterName)
                               .CreateCounter<TValue>(name, unit, description, tags ?? Pairs);

        return instrument;
    }
    public static ObservableCounter<TValue> GetOrAddCounter<TValue>( string name, Func<Measurement<TValue>> observeValue, string? unit, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(name, out Instrument? value) && value is ObservableCounter<TValue> instrument ) { return instrument; }

        Instruments[name] = instrument = GetOrAddMeter(meterName)
                               .CreateObservableCounter(name, observeValue, unit, description, tags ?? Pairs);

        return instrument;
    }
    public static ObservableCounter<TValue> GetOrAddCounter<TValue>( string name, Func<IEnumerable<Measurement<TValue>>> observeValue, string? unit, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(name, out Instrument? value) && value is ObservableCounter<TValue> instrument ) { return instrument; }

        Instruments[name] = instrument = GetOrAddMeter(meterName)
                               .CreateObservableCounter(name, observeValue, unit, description, tags ?? Pairs);

        return instrument;
    }


    public static UpDownCounter<TValue> GetOrAddUpDownCounter<TValue>( string name, string? unit, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(name, out Instrument? value) && value is UpDownCounter<TValue> instrument ) { return instrument; }

        Instruments[name] = instrument = GetOrAddMeter(meterName)
                               .CreateUpDownCounter<TValue>(name, unit, description, tags ?? Pairs);

        return instrument;
    }
    public static ObservableUpDownCounter<TValue> GetOrAddUpDownCounter<TValue>( string name, Func<Measurement<TValue>> observeValue, string? unit, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(name, out Instrument? value) && value is ObservableUpDownCounter<TValue> instrument ) { return instrument; }

        Instruments[name] = instrument = GetOrAddMeter(meterName)
                               .CreateObservableUpDownCounter(name, observeValue, unit, description, tags ?? Pairs);

        return instrument;
    }
    public static ObservableUpDownCounter<TValue> GetOrAddUpDownCounter<TValue>( string name, Func<IEnumerable<Measurement<TValue>>> observeValue, string? unit, string? description = null, IEnumerable<KeyValuePair<string, object?>>? tags = null, [CallerMemberName] string meterName = EMPTY )
        where TValue : struct
    {
        if ( Instruments.TryGetValue(name, out Instrument? value) && value is ObservableUpDownCounter<TValue> instrument ) { return instrument; }

        Instruments[name] = instrument = GetOrAddMeter(meterName)
                               .CreateObservableUpDownCounter(name, observeValue, unit, description, tags ?? Pairs);

        return instrument;
    }



    extension( Activity self )
    {
        public void AddUserID( UserRecord           record )       => self.AddTag(nameof(IUserID.UserID),      record.ID);
        public void AddSessionID( UserRecord        record )       => self.AddTag(Tags.SessionID,              record.SessionID);
        public void AddRoleID( RoleRecord           record )       => self.AddTag(Tags.RoleID,                 record.ID);
        public void AddGroupID( GroupRecord         record )       => self.AddTag(Tags.GroupID,                record.ID);
        public void AddGroup( string?               value = null ) => self.AddTag(Tags.AddGroup,               value);
        public void AddGroup( object?               value = null ) => self.AddTag(Tags.AddGroup,               value);
        public void AddGroupRights( string?         value = null ) => self.AddTag(Tags.AddGroupRights,         value);
        public void AddGroupRights( object?         value = null ) => self.AddTag(Tags.AddGroupRights,         value);
        public void AddRole( string?                value = null ) => self.AddTag(Tags.AddRole,                value);
        public void AddRole( object?                value = null ) => self.AddTag(Tags.AddRole,                value);
        public void AddRoleRights( string?          value = null ) => self.AddTag(Tags.AddRoleRights,          value);
        public void AddRoleRights( object?          value = null ) => self.AddTag(Tags.AddRoleRights,          value);
        public void AddUser( string?                value = null ) => self.AddTag(Tags.AddUser,                value);
        public void AddUser( object?                value = null ) => self.AddTag(Tags.AddUser,                value);
        public void AddUserAddress( string?         value = null ) => self.AddTag(Tags.AddUserAddress,         value);
        public void AddUserAddress( object?         value = null ) => self.AddTag(Tags.AddUserAddress,         value);
        public void AddUserLoginInfo( string?       value = null ) => self.AddTag(Tags.AddUserLoginInfo,       value);
        public void AddUserLoginInfo( object?       value = null ) => self.AddTag(Tags.AddUserLoginInfo,       value);
        public void AddUserRecoveryCode( string?    value = null ) => self.AddTag(Tags.AddUserRecoveryCode,    value);
        public void AddUserRecoveryCode( object?    value = null ) => self.AddTag(Tags.AddUserRecoveryCode,    value);
        public void AddUserRights( string?          value = null ) => self.AddTag(Tags.AddUserRights,          value);
        public void AddUserRights( object?          value = null ) => self.AddTag(Tags.AddUserRights,          value);
        public void AddUserSubscription( string?    value = null ) => self.AddTag(Tags.AddUserSubscription,    value);
        public void AddUserSubscription( object?    value = null ) => self.AddTag(Tags.AddUserSubscription,    value);
        public void AddUserToGroup( string?         value = null ) => self.AddTag(Tags.AddUserToGroup,         value);
        public void AddUserToGroup( object?         value = null ) => self.AddTag(Tags.AddUserToGroup,         value);
        public void AddUserToRole( string?          value = null ) => self.AddTag(Tags.AddUserToRole,          value);
        public void AddUserToRole( object?          value = null ) => self.AddTag(Tags.AddUserToRole,          value);
        public void ConnectDatabase( string?        value = null ) => self.AddTag(Tags.ConnectDatabase,        value);
        public void ConnectDatabase( object?        value = null ) => self.AddTag(Tags.ConnectDatabase,        value);
        public void LoginUser( string?              value = null ) => self.AddTag(Tags.LoginUser,              value);
        public void LoginUser( object?              value = null ) => self.AddTag(Tags.LoginUser,              value);
        public void RemoveGroup( string?            value = null ) => self.AddTag(Tags.RemoveGroup,            value);
        public void RemoveGroup( object?            value = null ) => self.AddTag(Tags.RemoveGroup,            value);
        public void RemoveGroupRights( string?      value = null ) => self.AddTag(Tags.RemoveGroupRights,      value);
        public void RemoveGroupRights( object?      value = null ) => self.AddTag(Tags.RemoveGroupRights,      value);
        public void RemoveRole( string?             value = null ) => self.AddTag(Tags.RemoveRole,             value);
        public void RemoveRole( object?             value = null ) => self.AddTag(Tags.RemoveRole,             value);
        public void RemoveRoleRights( string?       value = null ) => self.AddTag(Tags.RemoveRoleRights,       value);
        public void RemoveRoleRights( object?       value = null ) => self.AddTag(Tags.RemoveRoleRights,       value);
        public void RemoveUser( string?             value = null ) => self.AddTag(Tags.RemoveUser,             value);
        public void RemoveUser( object?             value = null ) => self.AddTag(Tags.RemoveUser,             value);
        public void RemoveUserAddress( string?      value = null ) => self.AddTag(Tags.RemoveUserAddress,      value);
        public void RemoveUserAddress( object?      value = null ) => self.AddTag(Tags.RemoveUserAddress,      value);
        public void RemoveUserFromGroup( string?    value = null ) => self.AddTag(Tags.RemoveUserFromGroup,    value);
        public void RemoveUserFromGroup( object?    value = null ) => self.AddTag(Tags.RemoveUserFromGroup,    value);
        public void RemoveUserFromRole( string?     value = null ) => self.AddTag(Tags.RemoveUserFromRole,     value);
        public void RemoveUserFromRole( object?     value = null ) => self.AddTag(Tags.RemoveUserFromRole,     value);
        public void RemoveUserLoginInfo( string?    value = null ) => self.AddTag(Tags.RemoveUserLoginInfo,    value);
        public void RemoveUserLoginInfo( object?    value = null ) => self.AddTag(Tags.RemoveUserLoginInfo,    value);
        public void RemoveUserRecoveryCode( string? value = null ) => self.AddTag(Tags.RemoveUserRecoveryCode, value);
        public void RemoveUserRecoveryCode( object? value = null ) => self.AddTag(Tags.RemoveUserRecoveryCode, value);
        public void RemoveUserRights( string?       value = null ) => self.AddTag(Tags.RemoveUserRights,       value);
        public void RemoveUserRights( object?       value = null ) => self.AddTag(Tags.RemoveUserRights,       value);
        public void RemoveUserSubscription( string? value = null ) => self.AddTag(Tags.RemoveUserSubscription, value);
        public void RemoveUserSubscription( object? value = null ) => self.AddTag(Tags.RemoveUserSubscription, value);
        public void UpdateGroup( string?            value = null ) => self.AddTag(Tags.UpdateGroup,            value);
        public void UpdateGroup( object?            value = null ) => self.AddTag(Tags.UpdateGroup,            value);
        public void UpdateRole( string?             value = null ) => self.AddTag(Tags.UpdateRole,             value);
        public void UpdateRole( object?             value = null ) => self.AddTag(Tags.UpdateRole,             value);
        public void UpdateUser( string?             value = null ) => self.AddTag(Tags.UpdateUser,             value);
        public void UpdateUser( object?             value = null ) => self.AddTag(Tags.UpdateUser,             value);
        public void UpdateUserAddress( string?      value = null ) => self.AddTag(Tags.UpdateUserAddress,      value);
        public void UpdateUserAddress( object?      value = null ) => self.AddTag(Tags.UpdateUserAddress,      value);
        public void UpdateUserLoginInfo( string?    value = null ) => self.AddTag(Tags.UpdateUserLoginInfo,    value);
        public void UpdateUserLoginInfo( object?    value = null ) => self.AddTag(Tags.UpdateUserLoginInfo,    value);
        public void UpdateUserSubscription( string? value = null ) => self.AddTag(Tags.UpdateUserSubscription, value);
        public void UpdateUserSubscription( object? value = null ) => self.AddTag(Tags.UpdateUserSubscription, value);
        public void VerifyLogin( string?            value = null ) => self.AddTag(Tags.VerifyLogin,            value);
        public void VerifyLogin( object?            value = null ) => self.AddTag(Tags.VerifyLogin,            value);
    }



    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Tags
    {
        public static string AddGroup            { get; set; } = nameof(AddGroup);
        public static string AddGroupRights      { get; set; } = nameof(AddGroupRights);
        public static string AddRole             { get; set; } = nameof(AddRole);
        public static string AddRoleRights       { get; set; } = nameof(AddRoleRights);
        public static string AddUser             { get; set; } = nameof(AddUser);
        public static string AddUserAddress      { get; set; } = nameof(AddUserAddress);
        public static string AddUserLoginInfo    { get; set; } = nameof(AddUserLoginInfo);
        public static string AddUserRecoveryCode { get; set; } = nameof(AddUserRecoveryCode);
        public static string AddUserRights       { get; set; } = nameof(AddUserRights);
        public static string AddUserSubscription { get; set; } = nameof(AddUserSubscription);
        public static string AddUserToGroup      { get; set; } = nameof(AddUserToGroup);
        public static string AddUserToRole       { get; set; } = nameof(AddUserToRole);
        public static string ConnectDatabase     { get; set; } = nameof(ConnectDatabase);
        public static string GroupID             { get; set; } = nameof(GroupID);
        public static string LoginUser           { get; set; } = nameof(LoginUser);
        public static string? Prefix
        {
            get;
            set
            {
                field = value;
                SetPrefix(value);
            }
        }
        public static string RemoveGroup            { get; set; } = nameof(RemoveGroup);
        public static string RemoveGroupRights      { get; set; } = nameof(RemoveGroupRights);
        public static string RemoveRole             { get; set; } = nameof(RemoveRole);
        public static string RemoveRoleRights       { get; set; } = nameof(RemoveRoleRights);
        public static string RemoveUser             { get; set; } = nameof(RemoveUser);
        public static string RemoveUserAddress      { get; set; } = nameof(RemoveUserAddress);
        public static string RemoveUserFromGroup    { get; set; } = nameof(RemoveUserFromGroup);
        public static string RemoveUserFromRole     { get; set; } = nameof(RemoveUserFromRole);
        public static string RemoveUserLoginInfo    { get; set; } = nameof(RemoveUserLoginInfo);
        public static string RemoveUserRecoveryCode { get; set; } = nameof(RemoveUserRecoveryCode);
        public static string RemoveUserRights       { get; set; } = nameof(RemoveUserRights);
        public static string RemoveUserSubscription { get; set; } = nameof(RemoveUserSubscription);
        public static string RoleID                 { get; set; } = nameof(RoleID);
        public static string SessionID              { get; set; } = nameof(SessionID);
        public static string UpdateGroup            { get; set; } = nameof(UpdateGroup);
        public static string UpdateRole             { get; set; } = nameof(UpdateRole);
        public static string UpdateUser             { get; set; } = nameof(UpdateUser);
        public static string UpdateUserAddress      { get; set; } = nameof(UpdateUserAddress);
        public static string UpdateUserLoginInfo    { get; set; } = nameof(UpdateUserLoginInfo);
        public static string UpdateUserSubscription { get; set; } = nameof(UpdateUserSubscription);
        public static string VerifyLogin            { get; set; } = nameof(VerifyLogin);


        public static void Print( TextWriter writer )
        {
            ReadOnlySpan<PropertyInfo> properties = typeof(Tags).GetProperties(BindingFlags.Static | BindingFlags.Public)
                                                                .Where(static x => x.Name != nameof(Prefix))
                                                                .ToArray();

            foreach ( PropertyInfo property in properties ) { writer.WriteLine(getPrefixLine(property.Name)); }

            writer.WriteLine();
            writer.WriteLine();
            writer.WriteLine();

            foreach ( PropertyInfo property in properties ) { writer.WriteLine(getMethodLine(property.Name)); }

            return;
            static string getPrefixLine( string property ) => $"            {property} = GetPrefix( prefix, {property}, nameof({property}) );";

            static string getMethodLine( string property ) =>
                $"""
                 [ MethodImpl( MethodImplOptions.AggressiveInlining ) ] public static void {property}( this Activity activity,  string? value = default ) => activity.AddTag( Tags.{property}, value );
                 [ MethodImpl( MethodImplOptions.AggressiveInlining ) ] public static void {property}( this Activity activity,  object? value = default ) => activity.AddTag( Tags.{property}, value );
                 """;
        }


        private static void SetPrefix( in ReadOnlySpan<char> prefix )
        {
            ConnectDatabase        = GetPrefix(prefix, ConnectDatabase,        nameof(ConnectDatabase));
            AddUser                = GetPrefix(prefix, AddUser,                nameof(AddUser));
            UpdateUser             = GetPrefix(prefix, UpdateUser,             nameof(UpdateUser));
            RemoveUser             = GetPrefix(prefix, RemoveUser,             nameof(RemoveUser));
            AddUserLoginInfo       = GetPrefix(prefix, AddUserLoginInfo,       nameof(AddUserLoginInfo));
            UpdateUserLoginInfo    = GetPrefix(prefix, UpdateUserLoginInfo,    nameof(UpdateUserLoginInfo));
            RemoveUserLoginInfo    = GetPrefix(prefix, RemoveUserLoginInfo,    nameof(RemoveUserLoginInfo));
            AddUserAddress         = GetPrefix(prefix, AddUserAddress,         nameof(AddUserAddress));
            UpdateUserAddress      = GetPrefix(prefix, UpdateUserAddress,      nameof(UpdateUserAddress));
            RemoveUserAddress      = GetPrefix(prefix, RemoveUserAddress,      nameof(RemoveUserAddress));
            AddUserSubscription    = GetPrefix(prefix, AddUserSubscription,    nameof(AddUserSubscription));
            UpdateUserSubscription = GetPrefix(prefix, UpdateUserSubscription, nameof(UpdateUserSubscription));
            RemoveUserSubscription = GetPrefix(prefix, RemoveUserSubscription, nameof(RemoveUserSubscription));
            AddUserRecoveryCode    = GetPrefix(prefix, AddUserRecoveryCode,    nameof(AddUserRecoveryCode));
            RemoveUserRecoveryCode = GetPrefix(prefix, RemoveUserRecoveryCode, nameof(RemoveUserRecoveryCode));
            AddUserRights          = GetPrefix(prefix, AddUserRights,          nameof(AddUserRights));
            RemoveUserRights       = GetPrefix(prefix, RemoveUserRights,       nameof(RemoveUserRights));
            LoginUser              = GetPrefix(prefix, LoginUser,              nameof(LoginUser));
            VerifyLogin            = GetPrefix(prefix, VerifyLogin,            nameof(VerifyLogin));
            AddGroup               = GetPrefix(prefix, AddGroup,               nameof(AddGroup));
            RemoveGroup            = GetPrefix(prefix, RemoveGroup,            nameof(RemoveGroup));
            UpdateGroup            = GetPrefix(prefix, UpdateGroup,            nameof(UpdateGroup));
            AddGroupRights         = GetPrefix(prefix, AddGroupRights,         nameof(AddGroupRights));
            RemoveGroupRights      = GetPrefix(prefix, RemoveGroupRights,      nameof(RemoveGroupRights));
            AddUserToGroup         = GetPrefix(prefix, AddUserToGroup,         nameof(AddUserToGroup));
            RemoveUserFromGroup    = GetPrefix(prefix, RemoveUserFromGroup,    nameof(RemoveUserFromGroup));
            AddRole                = GetPrefix(prefix, AddRole,                nameof(AddRole));
            RemoveRole             = GetPrefix(prefix, RemoveRole,             nameof(RemoveRole));
            UpdateRole             = GetPrefix(prefix, UpdateRole,             nameof(UpdateRole));
            AddRoleRights          = GetPrefix(prefix, AddRoleRights,          nameof(AddRoleRights));
            RemoveRoleRights       = GetPrefix(prefix, RemoveRoleRights,       nameof(RemoveRoleRights));
            AddUserToRole          = GetPrefix(prefix, AddUserToRole,          nameof(AddUserToRole));
            RemoveUserFromRole     = GetPrefix(prefix, RemoveUserFromRole,     nameof(RemoveUserFromRole));
        }
        private static string GetPrefix( in ReadOnlySpan<char> prefix, in ReadOnlySpan<char> tag, in ReadOnlySpan<char> defaultTag )
        {
            if ( prefix.IsEmpty )
            {
                return tag.IsEmpty
                           ? defaultTag.ToString()
                           : tag.ToString();
            }

            if ( tag.IsEmpty ) { return getResult(prefix, tag); }

            return getResult(prefix, defaultTag);

            static string getResult( in ReadOnlySpan<char> prefix, in ReadOnlySpan<char> tag )
            {
                Span<char> result = stackalloc char[prefix.Length + tag.Length];
                prefix.CopyTo(result);
                result[prefix.Length] = '.';

                tag.CopyTo(result[( prefix.Length + 1 )..]);
                return result.ToString();
            }
        }
    }
}
