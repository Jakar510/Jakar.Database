// Jakar.Extensions :: Jakar.Database
// 01/30/2023  2:41 PM


namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record UserLoginProviderRecord : OwnedTableRecord<UserLoginProviderRecord>, ITableRecord<UserLoginProviderRecord>
{
    public const string TABLE_NAME = "user_login_providers";


    public static                                                          string  TableName           => TABLE_NAME;
    [ColumnMetaData(ColumnOptions.Indexed)] public                         string  LoginProvider       { get; init; }
    public                                                                 string? ProviderDisplayName { get; init; }
    [ColumnMetaData(ColumnOptions.Indexed)] [ProtectedPersonalData] public string  ProviderKey         { get; init; }
    [ColumnMetaData(ColumnOptions.Indexed)] [ProtectedPersonalData] public string? Value               { get; init; }


    public UserLoginProviderRecord( UserRecord user, UserLoginInfo info ) : this(user, info.LoginProvider, info.ProviderKey, info.ProviderDisplayName) { }
    public UserLoginProviderRecord( UserRecord user, string        loginProvider, string providerKey, string? providerDisplayName ) : this(loginProvider, providerDisplayName, providerKey, EMPTY, RecordID<UserLoginProviderRecord>.New(), user.ID, DateTimeOffset.UtcNow) { }
    public UserLoginProviderRecord( string LoginProvider, string? ProviderDisplayName, string ProviderKey, string? Value, RecordID<UserLoginProviderRecord> ID, RecordID<UserRecord>? CreatedBy, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in CreatedBy, in ID, in DateCreated, in LastModified)
    {
        this.LoginProvider       = LoginProvider;
        this.ProviderDisplayName = ProviderDisplayName;
        this.ProviderKey         = ProviderKey;
        this.Value               = Value;
    }
    internal UserLoginProviderRecord( NpgsqlDataReader reader ) : base(reader)
    {
        LoginProvider       = reader.GetFieldValue<UserLoginProviderRecord, string>(nameof(LoginProvider));
        ProviderDisplayName = reader.GetFieldValue<UserLoginProviderRecord, string>(nameof(ProviderDisplayName));
        ProviderKey         = reader.GetFieldValue<UserLoginProviderRecord, string>(nameof(ProviderKey));
        Value               = reader.GetFieldValue<UserLoginProviderRecord, string>(nameof(Value));
    }


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        using PooledArray<ColumnMetaData> buffer = PropertyMetaData.SortedColumns;

        foreach ( ColumnMetaData column in buffer.Array )
        {
            switch ( column.PropertyName )
            {
                case nameof(ID):
                    await importer.WriteAsync(ID.Value, column.PostgresDbType, token);
                    break;

                case nameof(DateCreated):
                    await importer.WriteAsync(DateCreated, column.PostgresDbType, token);
                    break;

                case nameof(CreatedBy):
                    await importer.WriteAsync(CreatedBy?.Value, column.PostgresDbType, token);
                    break;

                case nameof(LoginProvider):
                    await importer.WriteAsync(LoginProvider, column.PostgresDbType, token);
                    break;

                case nameof(ProviderDisplayName):
                    await importer.WriteAsync(ProviderDisplayName, column.PostgresDbType, token);
                    break;

                case nameof(ProviderKey):
                    await importer.WriteAsync(ProviderKey, column.PostgresDbType, token);
                    break;

                case nameof(Value):
                    await importer.WriteAsync(Value, column.PostgresDbType, token);
                    break;

                case nameof(LastModified):
                    if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, column.PostgresDbType, token); }
                    else { await importer.WriteNullAsync(token); }

                    break;

                default:
                    throw new InvalidOperationException($"Unknown column: {column.PropertyName}");
            }
        }
    }
    public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(LoginProvider),       LoginProvider);
        parameters.Add(nameof(ProviderDisplayName), ProviderDisplayName);
        parameters.Add(nameof(ProviderKey),         ProviderKey);
        parameters.Add(nameof(Value),               Value);
        return parameters;
    }


    public static        MigrationRecord         CreateTable( ulong       migrationID ) => MigrationRecord.CreateTable<UserLoginProviderRecord>(migrationID);
    [Pure] public static UserLoginProviderRecord Create( NpgsqlDataReader reader )      => new UserLoginProviderRecord(reader).Validate();


    public static PostgresParameters GetDynamicParameters( UserRecord user, string value )
    {
        PostgresParameters parameters = PostgresParameters.Create<UserRecord>();
        parameters.Add(nameof(CreatedBy), user.ID.Value);
        parameters.Add(nameof(Value),     value);
        return parameters;
    }
    [Pure] public static PostgresParameters GetDynamicParameters( UserRecord user, UserLoginInfo info ) => GetDynamicParameters(user, info.LoginProvider, info.ProviderKey);
    [Pure] public static PostgresParameters GetDynamicParameters( UserRecord user, string loginProvider, string providerKey )
    {
        PostgresParameters parameters = GetDynamicParameters(user);
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
                                                                                                      UserId        = value.CreatedBy?.ToString() ?? throw new NullReferenceException(nameof(value.CreatedBy)),
                                                                                                      LoginProvider = value.LoginProvider,
                                                                                                      Name          = value.ProviderDisplayName ?? EMPTY,
                                                                                                      Value         = value.ProviderKey
                                                                                                  };
    public static implicit operator IdentityUserToken<Guid>( UserLoginProviderRecord value ) => new()
                                                                                                {
                                                                                                    UserId        = value.CreatedBy?.Value ?? Guid.Empty,
                                                                                                    LoginProvider = value.LoginProvider,
                                                                                                    Name          = value.ProviderDisplayName ?? EMPTY,
                                                                                                    Value         = value.ProviderKey
                                                                                                };
    public void Deconstruct( out string LoginProvider, out string? ProviderDisplayName, out string ProviderKey, out string? Value, out RecordID<UserLoginProviderRecord> ID, out RecordID<UserRecord>? CreatedBy, out DateTimeOffset DateCreated, out DateTimeOffset? LastModified )
    {
        LoginProvider       = this.LoginProvider;
        ProviderDisplayName = this.ProviderDisplayName;
        ProviderKey         = this.ProviderKey;
        Value               = this.Value;
        ID                  = this.ID;
        CreatedBy           = this.CreatedBy;
        DateCreated         = this.DateCreated;
        LastModified        = this.LastModified;
    }
}
