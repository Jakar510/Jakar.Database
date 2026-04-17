// Jakar.Extensions :: Jakar.Database
// 01/30/2023  2:41 PM


namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record UserLoginProviderRecord : OwnedTableRecord<UserLoginProviderRecord>, ITableRecord<UserLoginProviderRecord>
{
    public const string TABLE_NAME = "user_login_providers";

    // ReSharper disable once ReplaceWithFieldKeyword
    private static readonly    SqlName __tableName = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __tableName;

    [Indexed<UserLoginProviderRecord>(nameof(LoginProvider))] public                                string               LoginProvider       { get; init; }
    public                                                                                          string?              ProviderDisplayName { get; init; }
    [Indexed<UserLoginProviderRecord>(nameof(ProviderKey))] [ProtectedPersonalData] public          string               ProviderKey         { get; init; }
    [ForeignKey<UserLoginProviderRecord, UserRecord>]                               public override RecordID<UserRecord> UserID              { get; init; }
    [Indexed<UserLoginProviderRecord>(nameof(Value))] [ProtectedPersonalData]       public          string?              Value               { get; init; }


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


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    protected override async ValueTask Import( NpgsqlBinaryImporter importer, string propertyName, NpgsqlDbType postgresDbType, CancellationToken token )
    {
        switch ( propertyName )
        {
            case nameof(ID):
                await importer.WriteAsync(ID.Value, postgresDbType, token);
                break;

            case nameof(DateCreated):
                await importer.WriteAsync(DateCreated, postgresDbType, token);
                break;

            case nameof(UserID):
                await importer.WriteAsync(UserID.Value, postgresDbType, token);
                break;

            case nameof(LoginProvider):
                await importer.WriteAsync(LoginProvider, postgresDbType, token);
                break;

            case nameof(ProviderDisplayName):
                await importer.WriteAsync(ProviderDisplayName, postgresDbType, token);
                break;

            case nameof(ProviderKey):
                await importer.WriteAsync(ProviderKey, postgresDbType, token);
                break;

            case nameof(Value):
                await importer.WriteAsync(Value, postgresDbType, token);
                break;

            case nameof(LastModified):
                if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            default:
                throw new InvalidOperationException($"Unknown column: {propertyName}");
        }
    }
    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(LoginProvider)].DataColumn]       = LoginProvider;
        row[MetaData[nameof(ProviderDisplayName)].DataColumn] = ProviderDisplayName;
        row[MetaData[nameof(ProviderKey)].DataColumn]         = ProviderKey;
        row[MetaData[nameof(Value)].DataColumn]               = Value;
        return base.Import(row, token);
    }
    public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(LoginProvider),       LoginProvider);
        parameters.Add(nameof(ProviderDisplayName), ProviderDisplayName);
        parameters.Add(nameof(ProviderKey),         ProviderKey);
        parameters.Add(nameof(Value),               Value);
        return parameters;
    }


    [Pure] public static UserLoginProviderRecord Create( DbDataReader reader ) => new UserLoginProviderRecord(reader).Validate();


    public static CommandParameters GetDynamicParameters( UserRecord user, string value )
    {
        CommandParameters parameters = CommandParameters.Create<UserRecord>();
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


    public override bool Equals( UserLoginProviderRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return base.Equals(other)                                                                                         &&
               string.Equals(LoginProvider,       other.LoginProvider,       StringComparison.InvariantCultureIgnoreCase) &&
               string.Equals(ProviderDisplayName, other.ProviderDisplayName, StringComparison.InvariantCultureIgnoreCase) &&
               string.Equals(ProviderKey,         other.ProviderKey,         StringComparison.InvariantCultureIgnoreCase) &&
               string.Equals(Value,               other.Value,               StringComparison.InvariantCultureIgnoreCase);
    }
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(LoginProvider,       StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(ProviderDisplayName, StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(ProviderKey,         StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(Value,               StringComparer.InvariantCultureIgnoreCase);
        return hashCode.ToHashCode();
    }
    public static bool operator >( UserLoginProviderRecord  left, UserLoginProviderRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( UserLoginProviderRecord left, UserLoginProviderRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( UserLoginProviderRecord  left, UserLoginProviderRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( UserLoginProviderRecord left, UserLoginProviderRecord right ) => left.CompareTo(right) <= 0;


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
