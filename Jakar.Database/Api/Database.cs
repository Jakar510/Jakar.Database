// Jakar.Extensions :: Jakar.Database
// 08/14/2022  8:39 PM

namespace Jakar.Database;


[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract partial class Database : Randoms, IConnectableDbRoot, IHealthCheck, IUserTwoFactorTokenProvider<UserRecord>
{
    public const       ClaimType                        DEFAULT_CLAIM_TYPES = ClaimType.UserID | ClaimType.UserName | ClaimType.Group | ClaimType.Role;
    protected readonly ConcurrentBag<IDbTable>          _tables             = [];
    public readonly    DbOptions                        Options;
    public readonly    DbTable<AddressRecord>           Addresses;
    public readonly    DbTable<FileRecord>              Files;
    public readonly    DbTable<GroupRecord>             Groups;
    public readonly    DbTable<RecoveryCodeRecord>      RecoveryCodes;
    public readonly    DbTable<RoleRecord>              Roles;
    public readonly    DbTable<UserLoginProviderRecord> UserLoginProviders;
    public readonly    DbTable<UserRecord>              Users;
    public readonly    IConfiguration                   Configuration;
    protected readonly IFusionCache                     _cache;
    public readonly    ThreadLocal<UserRecord?>         LoggedInUser = new();
    protected          ActivitySource?                  _activitySource;
    protected          Meter?                           _meter;
    protected          string?                          _className;
    public static      Database?                        Current                   { get; set; }
    public static      DataProtector                    DataProtector             { get; set; } = new(RSAEncryptionPadding.OaepSHA1);
    public             string                           ClassName                 => _className ??= GetType().GetFullName();
    protected internal SecuredString?                   ConnectionString          { get; set; }
    public             MigrationManager                 MigrationManager          { get; }
    ref readonly       DbOptions IConnectableDbRoot.    Options                   => ref Options;
    public virtual     PasswordValidator                PasswordValidator         => DbOptions.PasswordRequirements.GetValidator();
    public virtual     IsolationLevel                   TransactionIsolationLevel => IsolationLevel.RepeatableRead;
    public             AppVersion                       Version                   => Options.AppInformation.Version;


    static Database()
    {
        EnumSqlHandler<SupportedLanguage>.Register();
        EnumSqlHandler<MimeType>.Register();
        EnumSqlHandler<Status>.Register();
        EnumSqlHandler<AppVersionFormat>.Register();
        DateTimeOffsetHandler.Register();
        DateTimeHandler.Register();
        DateOnlyHandler.Register();
        TimeOnlyHandler.Register();
        AppVersionHandler.Register();
        RecordID<AddressRecord>.RegisterDapperTypeHandlers();
        RecordID<RecoveryCodeRecord>.RegisterDapperTypeHandlers();
        RecordID<GroupRecord>.RegisterDapperTypeHandlers();
        RecordID<RoleRecord>.RegisterDapperTypeHandlers();
        RecordID<UserRecord>.RegisterDapperTypeHandlers();
        RecordID<UserLoginProviderRecord>.RegisterDapperTypeHandlers();
    }
    protected Database( IConfiguration configuration, IOptions<DbOptions> options, IFusionCache cache ) : base()
    {
        _cache             = cache;
        Configuration      = configuration;
        Options            = options.Value;
        Users              = Create<UserRecord>();
        Roles              = Create<RoleRecord>();
        Groups             = Create<GroupRecord>();
        RecoveryCodes      = Create<RecoveryCodeRecord>();
        UserLoginProviders = Create<UserLoginProviderRecord>();
        Addresses          = Create<AddressRecord>();
        Files              = Create<FileRecord>();
        MigrationManager   = CreateMigrationManager();
        Current            = this;
        Task.Run(InitDataProtector);
    }
    public virtual async ValueTask DisposeAsync()
    {
        foreach ( IDbTable disposable in _tables ) { await disposable.DisposeAsync(); }

        _tables.Clear();
        ConnectionString?.Dispose();
        ConnectionString = null;
        NpgsqlConnection.ClearAllPools();
        GC.SuppressFinalize(this);
    }


    // Task IHostedService.StartAsync( CancellationToken cancellationToken ) => _tableCache.StartAsync( cancellationToken );
    // Task IHostedService.StopAsync( CancellationToken  cancellationToken ) => _tableCache.StopAsync( cancellationToken );


    public async ValueTask<bool> HasAccess<TRight>( CancellationToken token, params TRight[] rights )
        where TRight : unmanaged, Enum
    {
        await using DbConnectionContext context      = await ConnectAsync(token);
        UserRecord?                     loggedInUser = LoggedInUser.Value;
        if ( loggedInUser is null ) { return false; }

        bool result = await HasPermission(context, loggedInUser, token, rights);
        return result;
    }
    public async ValueTask<bool> HasPermission<TRight>( DbConnectionContext context, UserRecord user, CancellationToken token, params TRight[] rights )
        where TRight : unmanaged, Enum
    {
        HashSet<TRight> permissions = await CurrentPermissions<TRight>(context, user, token);

        foreach ( TRight right in rights.AsSpan() )
        {
            if ( permissions.Contains(right) ) { return false; }
        }

        return true;
    }
    public async ValueTask<HashSet<TRight>> CurrentPermissions<TRight>( DbConnectionContext context, UserRecord user, CancellationToken token )
        where TRight : unmanaged, Enum
    {
        HashSet<IUserRights> models = new(DEFAULT_CAPACITY) { user };
        HashSet<TRight>      rights = new(Permissions<TRight>.EnumValues.Length);

        await foreach ( GroupRecord record in user.GetGroups(context, this, token) ) { models.Add(record); }

        await foreach ( RoleRecord record in user.GetRoles(context, this, token) ) { models.Add(record); }

        using Permissions<TRight> results = Permissions<TRight>.Create(models);

        foreach ( ( TRight permission, bool value ) in results.Rights )
        {
            if ( value ) { rights.Add(permission); }
        }

        return rights;
    }


    protected async Task InitDataProtector()
    {
        if ( Options.DataProtectorKey.HasValue )
        {
            ( LocalFile pem, SecuredStringResolverOptions password ) = Options.DataProtectorKey.Value;
            await InitDataProtector(pem, password);
        }
    }
    protected async ValueTask InitDataProtector( LocalFile pem, SecuredStringResolverOptions password, CancellationToken token = default ) => DataProtector = await DataProtector.WithKeyAsync(pem, await password.GetSecuredStringAsync(Configuration, token), token);

    protected virtual MigrationManager CreateMigrationManager() => new(this);
    internal async Task<DbConnection> CreateConnection( CancellationToken token )
    {
        ConnectionString ??= await Options.GetConnectionStringAsync(Configuration, token);
        DbConnection connection = CreateConnection(ConnectionString);
        await connection.OpenAsync(token);
        return connection;
    }
    protected abstract                 DbConnection                   CreateConnection( in SecuredString secure );
    [MustDisposeResource] public async ValueTask<DbConnectionContext> ConnectAsync( CancellationToken    token, IsolationLevel? level = null ) => await DbConnectionContext.CreateAsync(this, token, level);


    protected virtual DbTable<TSelf> Create<TSelf>()
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        TableMetaData<TSelf> data  = TSelf.MetaData;
        DbTable<TSelf>       table = new(this, _cache);
        return AddDisposable(table);
    }
    protected TValue AddDisposable<TValue>( TValue value )
        where TValue : IDbTable
    {
        _tables.Add(value);
        return value;
    }


    public virtual async Task<HealthCheckResult> CheckHealthAsync( HealthCheckContext context, CancellationToken token = default )
    {
        try
        {
            await using DbConnectionContext connection = await ConnectAsync(token);

            return connection.State switch
                   {
                       ConnectionState.Broken     => HealthCheckResult.Unhealthy(),
                       ConnectionState.Closed     => HealthCheckResult.Degraded(),
                       ConnectionState.Open       => HealthCheckResult.Healthy(),
                       ConnectionState.Connecting => HealthCheckResult.Healthy(),
                       ConnectionState.Executing  => HealthCheckResult.Healthy(),
                       ConnectionState.Fetching   => HealthCheckResult.Healthy(),
                       _                          => throw new OutOfRangeException(connection.State)
                   };
        }
        catch ( Exception e ) { return HealthCheckResult.Unhealthy(e.Message, e); }
    }


    public ValueTask<ErrorOrResult<SessionToken>> Register<TRequest>( TRequest request, string rights, ClaimType types = default, CancellationToken token = default )
        where TRequest : ILoginRequest<UserModel> => this.TryCall(Register, request, rights, types, token);
    public virtual async ValueTask<ErrorOrResult<SessionToken>> Register<TRequest>( DbConnectionContext context, TRequest request, string rights, ClaimType types = default, CancellationToken token = default )
        where TRequest : ILoginRequest<UserModel>
    {
        UserRecord? record = await Users.Get(context, UserRecord.GetDynamicParameters(request), token);
        if ( record is not null ) { return Error.NotFound(request.UserLogin); }

        if ( !PasswordValidator.Validate(request.UserPassword, out PasswordValidator.Results results) ) { return Error.Unauthorized(in results); }

        record = UserRecord.Create(request, rights);
        record = await Users.Insert(context, record, token);
        return await GetToken(context, record, types, token);
    }


    public virtual async IAsyncEnumerable<TSelf> Where<TSelf>( DbConnectionContext context, string sql, CommandParameters parameters, [EnumeratorCancellation] CancellationToken token = default )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>, IDateCreated
    {
        SqlCommand               command = SqlCommand.Create(sql, parameters);
        await using DbCommand    cmd     = command.ToCommand(context);
        await using DbDataReader reader  = await cmd.ExecuteReaderAsync(token);
        await foreach ( TSelf record in reader.CreateAsync<TSelf>(token) ) { yield return record; }
    }
    public virtual async IAsyncEnumerable<TValue> Where<TSelf, TValue>( DbConnectionContext context, string sql, CommandParameters parameters, [EnumeratorCancellation] CancellationToken token = default )
        where TValue : struct
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        SqlCommand               command = SqlCommand.Create(sql, parameters);
        await using DbCommand    cmd     = command.ToCommand(context);
        await using DbDataReader reader  = await cmd.ExecuteReaderAsync(token);
        while ( await reader.ReadAsync(token) ) { yield return reader.GetFieldValue<TValue>(0); }
    }
}
