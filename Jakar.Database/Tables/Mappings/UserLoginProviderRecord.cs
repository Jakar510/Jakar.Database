// Jakar.Extensions :: Jakar.Database
// 01/30/2023  2:41 PM


namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed partial record UserLoginProviderRecord : OwnedTableRecord<UserLoginProviderRecord>, ITableRecord<UserLoginProviderRecord>
{
    public const string TABLE_NAME = "user_login_providers";

    // ReSharper disable once ReplaceWithFieldKeyword
    private static readonly    SqlName __tableName = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __tableName;

    [Indexed<UserLoginProviderRecord>(nameof(LoginProvider))] [StringCompare(StringComparison.InvariantCultureIgnoreCase)] public                                string               LoginProvider       { get; init; }
    [StringCompare(StringComparison.InvariantCultureIgnoreCase)]                                                           public                                string?              ProviderDisplayName { get; init; }
    [Indexed<UserLoginProviderRecord>(nameof(ProviderKey))] [ProtectedPersonalData] [StringCompare(StringComparison.InvariantCultureIgnoreCase)] public          string               ProviderKey         { get; init; }
    [ForeignKey<UserLoginProviderRecord, UserRecord>]                               public override RecordID<UserRecord> UserID              { get; init; }
    [Indexed<UserLoginProviderRecord>(nameof(Value))] [ProtectedPersonalData] [StringCompare(StringComparison.InvariantCultureIgnoreCase)]       public          string?              Value               { get; init; }


    public UserLoginProviderRecord( UserRecord user, UserLoginInfo info ) : this(user, info.LoginProvider, info.ProviderKey, info.ProviderDisplayName) { }
    public UserLoginProviderRecord( UserRecord user, string        loginProvider, string providerKey, string? providerDisplayName ) : this(loginProvider, providerDisplayName, providerKey, EMPTY, RecordID<UserLoginProviderRecord>.New(), user.ID, DateTimeOffset.UtcNow) { }
    public UserLoginProviderRecord( string LoginProvider, string? ProviderDisplayName, string ProviderKey, string? Value, RecordID<UserLoginProviderRecord> ID, RecordID<UserRecord> UserID, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in UserID, in ID, in DateCreated, in LastModified)
    {
        this.LoginProvider       = LoginProvider;
        this.ProviderDisplayName = ProviderDisplayName;
        this.ProviderKey         = ProviderKey;
        this.Value               = Value;
    }
    internal UserLoginProviderRecord( DbDataReader reader ) : base(reader)
    {
        LoginProvider       = reader.GetFieldValue<UserLoginProviderRecord, string>(nameof(LoginProvider));
        ProviderDisplayName = reader.GetFieldValue<UserLoginProviderRecord, string>(nameof(ProviderDisplayName));
        ProviderKey         = reader.GetFieldValue<UserLoginProviderRecord, string>(nameof(ProviderKey));
        Value               = reader.GetFieldValue<UserLoginProviderRecord, string>(nameof(Value));
    }


    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(LoginProvider)].DataColumn]       = LoginProvider;
        row[MetaData[nameof(ProviderDisplayName)].DataColumn] = ProviderDisplayName;
        row[MetaData[nameof(ProviderKey)].DataColumn]         = ProviderKey;
        row[MetaData[nameof(Value)].DataColumn]               = Value;
        return base.Import(row, token);
    }
    public static CommandParameters GetDynamicParameters( UserRecord user, string value )
    {
        CommandParameters parameters = CommandParameters.Create<UserLoginProviderRecord>();
        parameters.Add(nameof(UserID), user.ID.Value);
        parameters.Add(nameof(Value),  value);
        return parameters;
    }
    [Pure] public static CommandParameters GetDynamicParameters( UserRecord user, UserLoginInfo info ) => GetDynamicParameters(user, info.LoginProvider, info.ProviderKey);
    [Pure] public static CommandParameters GetDynamicParameters( UserRecord user, string loginProvider, string providerKey )
    {
        CommandParameters parameters = GetDynamicParameters(user);
        parameters.Add(nameof(ProviderKey),   providerKey);
        parameters.Add(nameof(LoginProvider), loginProvider);
        return parameters;
    }

    [Pure] public UserLoginInfo ToUserLoginInfo() => new(LoginProvider, ProviderKey, ProviderDisplayName);


    public static implicit operator UserLoginInfo( UserLoginProviderRecord value ) => value.ToUserLoginInfo();
    public static implicit operator IdentityUserToken<string>( UserLoginProviderRecord value ) => new()
                                                                                                  {
                                                                                                      UserId        = value.UserID.ToString() ?? throw new NullReferenceException(nameof(value.UserID)),
                                                                                                      LoginProvider = value.LoginProvider,
                                                                                                      Name          = value.ProviderDisplayName ?? EMPTY,
                                                                                                      Value         = value.ProviderKey
                                                                                                  };
    public static implicit operator IdentityUserToken<Guid>( UserLoginProviderRecord value ) => new()
                                                                                                {
                                                                                                    UserId        = value.UserID.Value,
                                                                                                    LoginProvider = value.LoginProvider,
                                                                                                    Name          = value.ProviderDisplayName ?? EMPTY,
                                                                                                    Value         = value.ProviderKey
                                                                                                };
}
