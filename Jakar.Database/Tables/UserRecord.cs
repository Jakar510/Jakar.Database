using System.Data;
using Jakar.Extensions;
using Microsoft.AspNetCore.Identity;
using static Jakar.Database.Telemetry;



namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed record UserRecord : OwnedTableRecord<UserRecord>, ITableRecord<UserRecord>, IUserID, IUserModel, IUserSecurity<UserRecord>
{
    public const           string   TABLE_NAME         = "users";
    public static readonly TimeSpan DefaultLockoutTime = TimeSpan.FromHours(6);


    public static TableMetaData<UserRecord> PropertyMetaData { get; } = SqlTable<UserRecord>.Default.With_AdditionalData()
                                                                                            .With_CreatedBy()
                                                                                            .WithColumn<string>(nameof(UserName),                     ColumnOptions.Indexed, USER_NAME)
                                                                                            .WithColumn<string>(nameof(FirstName),                    ColumnOptions.Indexed, FIRST_NAME)
                                                                                            .WithColumn<string>(nameof(LastName),                     ColumnOptions.Indexed, LAST_NAME)
                                                                                            .WithColumn<string>(nameof(FullName),                     ColumnOptions.Indexed, FULL_NAME)
                                                                                            .WithColumn<string>(nameof(Gender),                       ColumnOptions.None,    GENDER)
                                                                                            .WithColumn<string>(nameof(Description),                  ColumnOptions.None,    DESCRIPTION)
                                                                                            .WithColumn<string>(nameof(Company),                      ColumnOptions.None,    COMPANY)
                                                                                            .WithColumn<string>(nameof(Department),                   ColumnOptions.None,    DEPARTMENT)
                                                                                            .WithColumn<string>(nameof(Title),                        ColumnOptions.None,    TITLE)
                                                                                            .WithColumn<SupportedLanguage>(nameof(PreferredLanguage), false)
                                                                                            .WithColumn<string>(nameof(Email),                        ColumnOptions.None, EMAIL)
                                                                                            .WithColumn<string>(nameof(PhoneNumber),                  ColumnOptions.None, PHONE)
                                                                                            .WithColumn<string>(nameof(Ext),                          ColumnOptions.None, PHONE_EXT)
                                                                                            .WithColumn<Guid>(nameof(SubscriptionID),                 ColumnOptions.Nullable)
                                                                                            .WithColumn<DateTimeOffset>(nameof(SubscriptionExpires),  ColumnOptions.Nullable)
                                                                                            .WithColumn<string>(nameof(Rights),                       ColumnOptions.None, RIGHTS)
                                                                                            .WithColumn<UserRecord>(nameof(EscalateTo))
                                                                                            .WithColumn<FileRecord>(nameof(ImageID))
                                                                                            .Build();


    [StringLength(RIGHTS)] public            UserRights Rights           { get; set; } = new();
    public static                            string     TableName        => TABLE_NAME;
    [StringLength(AUTHENTICATOR_KEY)] public string     AuthenticatorKey { get; set; } = EMPTY;
    public                                   int?       BadLogins        { get; set; }


    /// <summary> A random value that must change whenever a user is persisted to the store </summary>
    [StringLength(CONCURRENCY_STAMP)] public string ConcurrencyStamp { get; set; } = EMPTY;

    public                                                           bool            IsActive               { get;                    set; }
    public                                                           bool            IsDisabled             { get;                    set; }
    public                                                           bool            IsEmailConfirmed       { get;                    set; }
    public                                                           bool            IsLocked               { get;                    set; }
    public                                                           bool            IsPhoneNumberConfirmed { get;                    set; }
    public                                                           bool            IsTwoFactorEnabled     { get;                    set; }
    public                                                           DateTimeOffset? LastBadAttempt         { get;                    set; }
    public                                                           DateTimeOffset? LastLogin              { get;                    set; }
    public                                                           DateTimeOffset? LockDate               { get;                    set; }
    public                                                           DateTimeOffset? LockoutEnd             { get;                    set; }
    [StringLength(ENCRYPTED_MAX_PASSWORD_SIZE)] public               string          PasswordHash           { get;                    set; } = EMPTY;
    [StringLength(REFRESH_TOKEN)]               public               UInt128         RefreshTokenHash       { get;                    set; }
    public                                                           DateTimeOffset? RefreshTokenExpiryTime { get;                    set; }
    [StringLength(SECURITY_STAMP)] public                            string          SecurityStamp          { get;                    set; } = EMPTY;
    public                                                           Guid?           SessionID              { get;                    set; }
    [ProtectedPersonalData] [StringLength(MAX_SIZE)] public override JObject?        AdditionalData         { get => _additionalData; set => _additionalData = value; }
    [ProtectedPersonalData] [StringLength(COMPANY)]  public          string          Company                { get;                    set; } = EMPTY;
    Guid? ICreatedByUser<Guid>.                                                      CreatedBy              => CreatedBy?.Value;
    [ProtectedPersonalData] [StringLength(DEPARTMENT)]  public string                Department             { get; set; } = EMPTY;
    [ProtectedPersonalData] [StringLength(DESCRIPTION)] public string                Description            { get; set; } = EMPTY;
    [ProtectedPersonalData] [StringLength(EMAIL)]       public string                Email                  { get; set; } = EMPTY;
    public                                                     RecordID<UserRecord>? EscalateTo             { get; set; }
    Guid? IEscalateToUser<Guid>.                                                     EscalateTo             => EscalateTo?.Value;
    [ProtectedPersonalData] [StringLength(PHONE_EXT)]  public string                 Ext                    { get; set; } = EMPTY;
    [ProtectedPersonalData] [StringLength(FIRST_NAME)] public string                 FirstName              { get; set; } = EMPTY;
    [ProtectedPersonalData] [StringLength(FULL_NAME)]  public string                 FullName               { get; set; } = EMPTY;
    [ProtectedPersonalData] [StringLength(GENDER)]     public string                 Gender                 { get; set; } = EMPTY;
    Guid? IImageID<Guid>.                                                            ImageID                => ImageID?.Value;
    public                                                 RecordID<FileRecord>?     ImageID                { get; set; }
    public                                                 bool                      IsValid                => !string.IsNullOrWhiteSpace(UserName) && ID.IsValid();
    [ProtectedPersonalData] [StringLength(2000)]  public   string                    LastName               { get; set; } = EMPTY;
    [ProtectedPersonalData] [StringLength(PHONE)] public   string                    PhoneNumber            { get; set; } = EMPTY;
    public                                                 SupportedLanguage         PreferredLanguage      { get; set; }
    public                                                 DateTimeOffset?           SubscriptionExpires    { get; set; }
    public                                                 Guid?                     SubscriptionID         { get; set; }
    [ProtectedPersonalData] [StringLength(TITLE)] public   string                    Title                  { get; set; } = EMPTY;
    public                                                 Guid                      UserID                 => ID.Value;
    public                                                 string                    UserName               { get; init; } = EMPTY;
    [ProtectedPersonalData] [StringLength(WEBSITE)] public string                    Website                { get; set; }  = EMPTY;


    public UserRecord( in RecordID<UserRecord> ID ) : this(in ID, null, DateTimeOffset.UtcNow) { }
    public UserRecord( in RecordID<UserRecord> ID, UserRecord?              CreatedBy, in DateTimeOffset DateCreated, in DateTimeOffset? LastModified = null ) : this(in ID, CreatedBy?.ID, in DateCreated, in LastModified) { }
    public UserRecord( in RecordID<UserRecord> ID, in RecordID<UserRecord>? CreatedBy, in DateTimeOffset DateCreated, in DateTimeOffset? LastModified = null ) : base(in CreatedBy, in ID, in DateCreated, in LastModified) { }
    internal UserRecord( NpgsqlDataReader reader ) : this(RecordID<UserRecord>.ID(reader), RecordID<UserRecord>.CreatedBy(reader), reader.GetFieldValue<UserRecord, DateTimeOffset>(nameof(DateCreated)), reader.GetFieldValue<UserRecord, DateTimeOffset>(nameof(LastModified)))
    {
        UserName               = reader.GetFieldValue<UserRecord, string>(nameof(UserName));
        FirstName              = reader.GetFieldValue<UserRecord, string>(nameof(FirstName));
        LastName               = reader.GetFieldValue<UserRecord, string>(nameof(LastName));
        FullName               = reader.GetFieldValue<UserRecord, string>(nameof(FullName));
        Rights                 = reader.GetFieldValue<UserRecord, string>(nameof(Rights));
        Gender                 = reader.GetFieldValue<UserRecord, string>(nameof(Gender));
        Company                = reader.GetFieldValue<UserRecord, string>(nameof(Company));
        Description            = reader.GetFieldValue<UserRecord, string>(nameof(Description));
        Department             = reader.GetFieldValue<UserRecord, string>(nameof(Department));
        Title                  = reader.GetFieldValue<UserRecord, string>(nameof(Title));
        Website                = reader.GetFieldValue<UserRecord, string>(nameof(Website));
        PreferredLanguage      = reader.GetEnumValue<UserRecord, SupportedLanguage>(nameof(PreferredLanguage), SupportedLanguage.Unspecified);
        Email                  = reader.GetFieldValue<UserRecord, string>(nameof(Email));
        PhoneNumber            = reader.GetFieldValue<UserRecord, string>(nameof(PhoneNumber));
        Ext                    = reader.GetFieldValue<UserRecord, string>(nameof(Ext));
        SubscriptionID         = reader.GetFieldValue<UserRecord, Guid?>(nameof(SubscriptionID));
        SubscriptionExpires    = reader.GetFieldValue<UserRecord, DateTimeOffset?>(nameof(SubscriptionExpires));
        EscalateTo             = RecordID<UserRecord>.TryCreate(reader, nameof(EscalateTo));
        AdditionalData         = reader.GetAdditionalData<UserRecord>();
        ImageID                = RecordID<FileRecord>.TryCreate(reader, nameof(ImageID));
        LastBadAttempt         = reader.GetFieldValue<UserRecord, DateTimeOffset?>(nameof(LastBadAttempt));
        LastLogin              = reader.GetFieldValue<UserRecord, DateTimeOffset?>(nameof(LastLogin));
        BadLogins              = reader.GetFieldValue<UserRecord, int>(nameof(BadLogins));
        LockDate               = reader.GetFieldValue<UserRecord, DateTimeOffset?>(nameof(LockDate));
        LockoutEnd             = reader.GetFieldValue<UserRecord, DateTimeOffset?>(nameof(LockoutEnd));
        PasswordHash           = reader.GetFieldValue<UserRecord, string>(nameof(PasswordHash));
        AuthenticatorKey       = reader.GetFieldValue<UserRecord, string>(nameof(AuthenticatorKey));
        RefreshTokenHash       = reader.GetFieldValue<UserRecord, UInt128>(nameof(RefreshTokenHash), UInt128.Zero);
        RefreshTokenExpiryTime = reader.GetFieldValue<UserRecord, DateTimeOffset?>(nameof(RefreshTokenExpiryTime));
        SessionID              = reader.GetFieldValue<UserRecord, Guid?>(nameof(SessionID));
        SecurityStamp          = reader.GetFieldValue<UserRecord, string>(nameof(SecurityStamp));
        ConcurrencyStamp       = reader.GetFieldValue<UserRecord, string>(nameof(ConcurrencyStamp));
        IsEmailConfirmed       = reader.GetFieldValue<UserRecord, bool>(nameof(IsEmailConfirmed));
        IsPhoneNumberConfirmed = reader.GetFieldValue<UserRecord, bool>(nameof(IsPhoneNumberConfirmed));
        IsTwoFactorEnabled     = reader.GetFieldValue<UserRecord, bool>(nameof(IsTwoFactorEnabled));
        IsLocked               = reader.GetFieldValue<UserRecord, bool>(nameof(IsLocked));
        IsActive               = reader.GetFieldValue<UserRecord, bool>(nameof(IsActive));
        IsDisabled             = reader.GetFieldValue<UserRecord, bool>(nameof(IsDisabled));
    }

    public static UserRecord Create( NpgsqlDataReader reader ) => new UserRecord(reader).Validate();
    public static UserRecord Create<TUser, TEnum>( ILoginRequest<TUser> request, UserRecord? caller = null )
        where TUser : class, IUserData<Guid>
        where TEnum : unmanaged, Enum => Create(request, request.Data.Rights, caller);
    public static UserRecord Create<TUser, TEnum>( ILoginRequest<TUser> request, scoped in Permissions<TEnum> rights, UserRecord? caller = null )
        where TUser : class, IUserData<Guid>
        where TEnum : unmanaged, Enum => Create(request, rights.ToString(), caller);
    public static UserRecord Create<TUser>( ILoginRequest<TUser> request, UserRights rights, UserRecord? caller = null )
        where TUser : class, IUserData<Guid>
    {
        ArgumentNullException.ThrowIfNull(request.Data);
        UserRecord user = Create(request.UserLogin, rights, request.Data, caller);

        return user.WithPassword(request.UserPassword)
                   .Enable();
    }

    public static UserRecord Create<TUser>( string userName, UserRights rights, TUser data, UserRecord? caller = null )
        where TUser : class, IUserData<Guid>
    {
        RecordID<UserRecord> id = RecordID<UserRecord>.New();

        UserRecord user = new(id, caller?.ID, DateTimeOffset.UtcNow)
                          {
                              UserName          = userName,
                              FirstName         = data.FirstName,
                              LastName          = data.LastName,
                              FullName          = data.FullName,
                              Gender            = data.Gender,
                              Description       = data.Description,
                              Company           = data.Company,
                              Department        = data.Department,
                              Title             = data.Title,
                              Website           = data.Website,
                              PreferredLanguage = data.PreferredLanguage,
                              Email             = data.Email,
                              PhoneNumber       = data.PhoneNumber,
                              Ext               = data.Ext,
                              Rights            = rights,
                              EscalateTo        = RecordID<UserRecord>.TryCreate(data.EscalateTo),
                              AdditionalData    = data.AdditionalData,
                              ImageID           = RecordID<FileRecord>.TryCreate(data.ImageID)
                          };

        return user;
    }

    public static UserRecord Create<TEnum>( string userName, string password, scoped in Permissions<TEnum> rights, UserRecord? caller = null )
        where TEnum : unmanaged, Enum => Create(userName, password, rights.ToString(), caller);
    public static UserRecord Create( string userName, string password, UserRights rights, UserRecord? caller = null ) => new(RecordID<UserRecord>.New(), caller?.ID, DateTimeOffset.UtcNow)
                                                                                                                         {
                                                                                                                             UserName = userName,
                                                                                                                             Rights   = rights
                                                                                                                         };


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        await base.Import(importer, token);
        await importer.WriteAsync(UserName,          NpgsqlDbType.Text,   token);
        await importer.WriteAsync(FirstName,         NpgsqlDbType.Text,   token);
        await importer.WriteAsync(LastName,          NpgsqlDbType.Text,   token);
        await importer.WriteAsync(FullName,          NpgsqlDbType.Text,   token);
        await importer.WriteAsync(Rights,            NpgsqlDbType.Text,   token);
        await importer.WriteAsync(Gender,            NpgsqlDbType.Text,   token);
        await importer.WriteAsync(Company,           NpgsqlDbType.Text,   token);
        await importer.WriteAsync(Department,        NpgsqlDbType.Text,   token);
        await importer.WriteAsync(Description,       NpgsqlDbType.Text,   token);
        await importer.WriteAsync(Title,             NpgsqlDbType.Text,   token);
        await importer.WriteAsync(Website,           NpgsqlDbType.Text,   token);
        await importer.WriteAsync(PreferredLanguage, NpgsqlDbType.Bigint, token);
        await importer.WriteAsync(Email,             NpgsqlDbType.Text,   token);

        if ( EscalateTo.HasValue ) { await importer.WriteAsync(EscalateTo.Value, NpgsqlDbType.Uuid, token); }
        else { await importer.WriteNullAsync(token); }

        if ( LastBadAttempt.HasValue ) { await importer.WriteAsync(LastBadAttempt.Value, NpgsqlDbType.TimestampTz, token); }
        else { await importer.WriteNullAsync(token); }

        if ( LastLogin.HasValue ) { await importer.WriteAsync(LastLogin.Value, NpgsqlDbType.TimestampTz, token); }
        else { await importer.WriteNullAsync(token); }

        if ( BadLogins.HasValue ) { await importer.WriteAsync(BadLogins.Value, NpgsqlDbType.Integer, token); }
        else { await importer.WriteNullAsync(token); }

        // await importer.WriteAsync(IsLocked, NpgsqlDbType.Boolean, token);

        if ( LockDate.HasValue ) { await importer.WriteAsync(LockDate.Value, NpgsqlDbType.TimestampTz, token); }
        else { await importer.WriteNullAsync(token); }

        if ( LockoutEnd.HasValue ) { await importer.WriteAsync(LockoutEnd.Value, NpgsqlDbType.TimestampTz, token); }
        else { await importer.WriteNullAsync(token); }

        await importer.WriteAsync(PasswordHash,     NpgsqlDbType.Text, token);
        await importer.WriteAsync(RefreshTokenHash, NpgsqlDbType.Text, token);

        if ( RefreshTokenExpiryTime.HasValue ) { await importer.WriteAsync(RefreshTokenExpiryTime.Value, NpgsqlDbType.TimestampTz, token); }
        else { await importer.WriteNullAsync(token); }

        if ( SessionID.HasValue ) { await importer.WriteAsync(SessionID.Value, NpgsqlDbType.Uuid, token); }
        else { await importer.WriteNullAsync(token); }

        await importer.WriteAsync(IsActive,         NpgsqlDbType.Boolean, token);
        await importer.WriteAsync(IsDisabled,       NpgsqlDbType.Boolean, token);
        await importer.WriteAsync(SecurityStamp,    NpgsqlDbType.Text,    token);
        await importer.WriteAsync(ConcurrencyStamp, NpgsqlDbType.Text,    token);

        await importer.WriteAsync(AuthenticatorKey, NpgsqlDbType.Text, token);
        await importer.WriteAsync(AdditionalData,   NpgsqlDbType.Json, token);
    }
    public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(UserName),               UserName);
        parameters.Add(nameof(FirstName),              FirstName);
        parameters.Add(nameof(LastName),               LastName);
        parameters.Add(nameof(FullName),               FullName);
        parameters.Add(nameof(Rights),                 Rights);
        parameters.Add(nameof(Gender),                 Gender);
        parameters.Add(nameof(Company),                Company);
        parameters.Add(nameof(Department),             Department);
        parameters.Add(nameof(Description),            Description);
        parameters.Add(nameof(Title),                  Title);
        parameters.Add(nameof(Website),                Website);
        parameters.Add(nameof(PreferredLanguage),      PreferredLanguage);
        parameters.Add(nameof(Email),                  Email);
        parameters.Add(nameof(PhoneNumber),            PhoneNumber);
        parameters.Add(nameof(Ext),                    Ext);
        parameters.Add(nameof(EscalateTo),             EscalateTo?.Value);
        parameters.Add(nameof(LastBadAttempt),         LastBadAttempt);
        parameters.Add(nameof(LastLogin),              LastLogin);
        parameters.Add(nameof(BadLogins),              BadLogins);
        parameters.Add(nameof(LockDate),               LockDate);
        parameters.Add(nameof(LockoutEnd),             LockoutEnd);
        parameters.Add(nameof(PasswordHash),           PasswordHash);
        parameters.Add(nameof(RefreshTokenHash),       RefreshTokenHash);
        parameters.Add(nameof(RefreshTokenExpiryTime), RefreshTokenExpiryTime);
        parameters.Add(nameof(SessionID),              SessionID);
        parameters.Add(nameof(SecurityStamp),          SecurityStamp);
        parameters.Add(nameof(ConcurrencyStamp),       ConcurrencyStamp);
        parameters.Add(nameof(AuthenticatorKey),       AuthenticatorKey);
        parameters.Add(nameof(IsEmailConfirmed),       IsEmailConfirmed);
        parameters.Add(nameof(IsPhoneNumberConfirmed), IsPhoneNumberConfirmed);
        parameters.Add(nameof(IsLocked),               IsLocked);
        parameters.Add(nameof(IsTwoFactorEnabled),     IsTwoFactorEnabled);
        parameters.Add(nameof(IsActive),               IsActive);
        parameters.Add(nameof(IsDisabled),             IsDisabled);
        parameters.Add(nameof(AdditionalData),         AdditionalData);
        return parameters;
    }


    public static PostgresParameters GetDynamicParameters( IUserData data )
    {
        PostgresParameters parameters = PostgresParameters.Create<UserRecord>();
        parameters.Add(nameof(Email),     data.Email);
        parameters.Add(nameof(FirstName), data.FirstName);
        parameters.Add(nameof(LastName),  data.LastName);
        parameters.Add(nameof(FullName),  data.FullName);
        return parameters;
    }
    public static PostgresParameters GetDynamicParameters( ILoginRequest request )
    {
        PostgresParameters parameters = PostgresParameters.Create<UserRecord>();
        parameters.Add(nameof(UserName), request.UserLogin);
        return parameters;
    }
    public static PostgresParameters GetDynamicParameters( string userName )
    {
        PostgresParameters parameters = PostgresParameters.Create<UserRecord>();
        parameters.Add(nameof(UserName), userName);
        return parameters;
    }
    public static PostgresParameters GetDynamicParameters( RecordID<UserRecord> userID )
    {
        PostgresParameters parameters = PostgresParameters.Create<UserRecord>();
        parameters.Add(nameof(ID), userID);
        return parameters;
    }


    public string        GetDescription()              => IUserData.GetDescription(this);
    void IUserData<Guid>.With( IUserData<Guid> value ) => With(value);
    public UserRecord With( IUserData<Guid> value )
    {
        FirstName         = value.FirstName;
        LastName          = value.LastName;
        FullName          = value.FullName;
        Description       = value.Description;
        Website           = value.Website;
        Email             = value.Email;
        PhoneNumber       = value.PhoneNumber;
        Ext               = value.Ext;
        Title             = value.Title;
        Department        = value.Department;
        Company           = value.Company;
        PreferredLanguage = value.PreferredLanguage;
        EscalateTo        = RecordID<UserRecord>.TryCreate(value.EscalateTo);
        ImageID           = RecordID<FileRecord>.TryCreate(value.ImageID);
        return WithAdditionalData(value);
    }


    public UserRecord WithRights( scoped in UserRights rights )
    {
        Rights.Value = rights.Value;
        return this;
    }
    public UserRecord WithRights<TEnum>( scoped in Permissions<TEnum> rights )
        where TEnum : unmanaged, Enum
    {
        Rights.Value = rights.ToString();
        return this;
    }
   

    public override bool Equals( UserRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return base.Equals(other) && string.Equals(UserName, other.UserName, StringComparison.InvariantCultureIgnoreCase) && string.Equals(FullName, other.FullName, StringComparison.InvariantCultureIgnoreCase) && string.Equals(FirstName, other.FirstName, StringComparison.InvariantCultureIgnoreCase) && string.Equals(LastName, other.LastName, StringComparison.InvariantCultureIgnoreCase);
    }
    public override int CompareTo( UserRecord? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }


        int userNameComparison = string.Compare(UserName, other.UserName, StringComparison.Ordinal);
        if ( userNameComparison != 0 ) { return userNameComparison; }

        int firstNameComparison = string.Compare(FirstName, other.FirstName, StringComparison.Ordinal);
        if ( firstNameComparison != 0 ) { return firstNameComparison; }

        int lastNameComparison = string.Compare(LastName, other.LastName, StringComparison.Ordinal);
        if ( lastNameComparison != 0 ) { return lastNameComparison; }

        int fullNameComparison = string.Compare(FullName, other.FullName, StringComparison.Ordinal);
        if ( fullNameComparison != 0 ) { return fullNameComparison; }

        int descriptionComparison = string.Compare(Description, other.Description, StringComparison.Ordinal);
        if ( descriptionComparison != 0 ) { return descriptionComparison; }

        int websiteComparison = string.Compare(Website, other.Website, StringComparison.Ordinal);
        if ( websiteComparison != 0 ) { return websiteComparison; }

        int emailComparison = string.Compare(Email, other.Email, StringComparison.Ordinal);
        if ( emailComparison != 0 ) { return emailComparison; }

        int phoneNumberComparison = string.Compare(PhoneNumber, other.PhoneNumber, StringComparison.Ordinal);
        if ( phoneNumberComparison != 0 ) { return phoneNumberComparison; }

        int extComparison = string.Compare(Ext, other.Ext, StringComparison.Ordinal);
        if ( extComparison != 0 ) { return extComparison; }

        int titleComparison = string.Compare(Title, other.Title, StringComparison.Ordinal);
        if ( titleComparison != 0 ) { return titleComparison; }

        int departmentComparison = string.Compare(Department, other.Department, StringComparison.Ordinal);
        if ( departmentComparison != 0 ) { return departmentComparison; }

        return string.Compare(Company, other.Company, StringComparison.Ordinal);
    }
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), UserName, FullName, FirstName, LastName);


    public static bool operator >( UserRecord  left, UserRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( UserRecord left, UserRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( UserRecord  left, UserRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( UserRecord left, UserRecord right ) => left.CompareTo(right) <= 0;


    public UserModel ToUserModel() => ToUserModel<UserModel>();
    public TSelf ToUserModel<TSelf>()
        where TSelf : UserModel<TSelf, Guid, UserAddress, GroupModel, RoleModel>, ICreateUserModel<TSelf, Guid, UserAddress, GroupModel, RoleModel>, IJsonModel<TSelf>, new() => ToUserModel<TSelf, UserAddress, GroupModel, RoleModel>();
    public TSelf ToUserModel<TSelf, TAddress, TGroupModel, TRoleModel>()
        where TSelf : class, IUserData<Guid, TAddress, TGroupModel, TRoleModel>, ICreateUserModel<TSelf, Guid, TAddress, TGroupModel, TRoleModel>, new()
        where TGroupModel : class, IGroupModel<TGroupModel, Guid>, IEquatable<TGroupModel>
        where TRoleModel : class, IRoleModel<TRoleModel, Guid>, IEquatable<TRoleModel>
        where TAddress : class, IAddress<TAddress, Guid>, IEquatable<TAddress> => TSelf.Create(this);


    public ValueTask<UserModel> ToUserModel( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, CancellationToken token ) => ToUserModel<UserModel>(connection, transaction, db, token);
    public ValueTask<TSelf> ToUserModel<TSelf>( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, CancellationToken token )
        where TSelf : UserModel<TSelf, Guid, UserAddress, GroupModel, RoleModel>, ICreateUserModel<TSelf, Guid, UserAddress, GroupModel, RoleModel>, IJsonModel<TSelf>, new() => ToUserModel<TSelf, UserAddress, GroupModel, RoleModel>(connection, transaction, db, token);
    public async ValueTask<TSelf> ToUserModel<TSelf, TAddress, TGroupModel, TRoleModel>( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, CancellationToken token )
        where TSelf : class, IUserData<Guid, TAddress, TGroupModel, TRoleModel>, ICreateUserModel<TSelf, Guid, TAddress, TGroupModel, TRoleModel>, new()
        where TGroupModel : class, IGroupModel<TGroupModel, Guid>, IEquatable<TGroupModel>
        where TRoleModel : class, IRoleModel<TRoleModel, Guid>, IEquatable<TRoleModel>
        where TAddress : class, IAddress<TAddress, Guid>, IEquatable<TAddress>
    {
        TSelf model = TSelf.Create(this);

        await foreach ( AddressRecord record in GetAddresses(connection, transaction, db, token) ) { model.Addresses.Add(record.ToAddressModel<TAddress>()); }

        await foreach ( GroupRecord record in GetGroups(connection, transaction, db, token) ) { model.Groups.Add(record.ToGroupModel<TGroupModel>()); }

        await foreach ( RoleRecord record in GetRoles(connection, transaction, db, token) ) { model.Roles.Add(record.ToRoleModel<TRoleModel>()); }

        return model;
    }


    public static MigrationRecord CreateTable( ulong migrationID ) => MigrationRecord.CreateTable<UserRecord>(migrationID);



    #region Owners

    public async ValueTask<ErrorOrResult<UserRecord>> GetBoss( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, CancellationToken token ) =>
        EscalateTo.HasValue
            ? await db.Users.Get(connection, transaction, EscalateTo.Value, token)
            : Error.Gone();


    public bool DoesNotOwn<TSelf>( TSelf record )
        where TSelf : class, ICreatedBy, ITableRecord<TSelf> => record.CreatedBy != ID;
    public bool Owns<TSelf>( TSelf record )
        where TSelf : class, ICreatedBy, ITableRecord<TSelf> => record.CreatedBy == ID;

    #endregion



    public ValueTask<bool> RedeemCode( Database db, string code, CancellationToken token ) => db.TryCall(RedeemCode, db, code, token);
    public async ValueTask<bool> RedeemCode( NpgsqlConnection connection, NpgsqlTransaction transaction, Database db, string code, CancellationToken token )
    {
        await foreach ( UserRecoveryCodeRecord mapping in UserRecoveryCodeRecord.Where(connection, transaction, db.UserRecoveryCodes, this, token) )
        {
            RecoveryCodeRecord? record = await mapping.Get(connection, transaction, db.RecoveryCodes, token);

            if ( record is null ) { await db.UserRecoveryCodes.Delete(connection, transaction, mapping, token); }
            else if ( RecoveryCodeRecord.IsValid(code, ref record) )
            {
                await db.RecoveryCodes.Delete(connection, transaction, record, token);
                await db.UserRecoveryCodes.Delete(connection, transaction, mapping, token);
                return true;
            }
        }

        return false;
    }
    public ValueTask<string[]> ReplaceCodes( Database db, int count = 10, CancellationToken token = default ) => db.TryCall(ReplaceCodes, db, count, token);
    public async ValueTask<string[]> ReplaceCodes( NpgsqlConnection connection, NpgsqlTransaction transaction, Database db, int count = 10, CancellationToken token = default )
    {
        IAsyncEnumerable<RecoveryCodeRecord>            old        = Codes(connection, transaction, db, token);
        IReadOnlyDictionary<string, RecoveryCodeRecord> dictionary = RecoveryCodeRecord.Create(this, count);
        string[]                                        codes      = dictionary.Keys.ToArray();


        await db.RecoveryCodes.Delete(connection, transaction, old, token);
        await UserRecoveryCodeRecord.Replace(connection, transaction, db.UserRecoveryCodes, this, RecordID<RecoveryCodeRecord>.Create(dictionary.Values), token);
        return codes;
    }
    public ValueTask<string[]> ReplaceCodes( Database db, IEnumerable<string> recoveryCodes, CancellationToken token = default ) => db.TryCall(ReplaceCodes, db, recoveryCodes, token);
    public async ValueTask<string[]> ReplaceCodes( NpgsqlConnection connection, NpgsqlTransaction transaction, Database db, IEnumerable<string> recoveryCodes, CancellationToken token = default )
    {
        IAsyncEnumerable<RecoveryCodeRecord> old        = Codes(connection, transaction, db, token);
        RecoveryCodeRecord.Codes             dictionary = RecoveryCodeRecord.Create(this, recoveryCodes);
        string[]                             codes      = [.. dictionary.Keys];


        await db.RecoveryCodes.Delete(connection, transaction, old, token);
        await UserRecoveryCodeRecord.Replace(connection, transaction, db.UserRecoveryCodes, this, RecordID<RecoveryCodeRecord>.Create(dictionary.Values), token);
        return codes;
    }
    public       IAsyncEnumerable<RecoveryCodeRecord> Codes( Database                 db,         CancellationToken  token )                                                                                               => db.TryCall(Codes, db, token);
    public       IAsyncEnumerable<RecoveryCodeRecord> Codes( NpgsqlConnection         connection, NpgsqlTransaction  transaction, Database db, CancellationToken                          token )                          => UserRecoveryCodeRecord.Where(connection, transaction, db.RecoveryCodes, this, token);
    public async ValueTask<bool>                      TryAdd( NpgsqlConnection        connection, NpgsqlTransaction  transaction, Database db, AddressRecord                              value, CancellationToken token ) => await UserAddressRecord.TryAdd(connection, transaction, db.UserAddresses, ID, value, token);
    public       IAsyncEnumerable<AddressRecord>      GetAddresses( NpgsqlConnection  connection, NpgsqlTransaction? transaction, Database db, [EnumeratorCancellation] CancellationToken token = default )                => UserAddressRecord.Where(connection, transaction, db.Addresses, ID, token);
    public async ValueTask<bool>                      HasAddress( NpgsqlConnection    connection, NpgsqlTransaction  transaction, Database db, AddressRecord                              value, CancellationToken token ) => await UserAddressRecord.Exists(connection, transaction, db.UserAddresses, ID, value, token);
    public async ValueTask                            Remove( NpgsqlConnection        connection, NpgsqlTransaction  transaction, Database db, AddressRecord                              value, CancellationToken token ) => await UserAddressRecord.Delete(connection, transaction, db.UserAddresses, ID, value, token);
    public async ValueTask<bool>                      TryAdd( NpgsqlConnection        connection, NpgsqlTransaction  transaction, Database db, RoleRecord                                 value, CancellationToken token ) => await UserRoleRecord.TryAdd(connection, transaction, db.UserRoles, ID, value, token);
    public       IAsyncEnumerable<RoleRecord>         GetRoles( NpgsqlConnection      connection, NpgsqlTransaction? transaction, Database db, CancellationToken                          token = default )                => UserRoleRecord.Where(connection, transaction, db.Roles, ID, token);
    public async ValueTask<bool>                      HasRole( NpgsqlConnection       connection, NpgsqlTransaction  transaction, Database db, RoleRecord                                 value, CancellationToken token ) => await UserRoleRecord.Exists(connection, transaction, db.UserRoles, ID, value, token);
    public async ValueTask                            Remove( NpgsqlConnection        connection, NpgsqlTransaction  transaction, Database db, RoleRecord                                 value, CancellationToken token ) => await UserRoleRecord.Delete(connection, transaction, db.UserRoles, ID, value, token);
    public async ValueTask<bool>                      TryAdd( NpgsqlConnection        connection, NpgsqlTransaction  transaction, Database db, GroupRecord                                value, CancellationToken token ) => await UserGroupRecord.TryAdd(connection, transaction, db.UserGroups, ID, value, token);
    public       IAsyncEnumerable<GroupRecord>        GetGroups( NpgsqlConnection     connection, NpgsqlTransaction? transaction, Database db, CancellationToken                          token = default )                => UserGroupRecord.Where(connection, transaction, db.Groups, ID, token);
    public async ValueTask<bool>                      IsPartOfGroup( NpgsqlConnection connection, NpgsqlTransaction  transaction, Database db, GroupRecord                                value, CancellationToken token ) => await UserGroupRecord.Exists(connection, transaction, db.UserGroups, ID, value, token);
    public async ValueTask                            Remove( NpgsqlConnection        connection, NpgsqlTransaction  transaction, Database db, GroupRecord                                value, CancellationToken token ) => await UserGroupRecord.Delete(connection, transaction, db.UserGroups, ID, value, token);



    #region Claims

    public async ValueTask<Claim[]> GetUserClaims( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, ClaimType types, CancellationToken token )
    {
        UserModel model = await ToUserModel(connection, transaction, db, token);
        return model.GetClaims(types);
    }
    public static ValueTask<ErrorOrResult<UserRecord>> TryFromClaims( NpgsqlConnection connection, NpgsqlTransaction transaction, Database db, ClaimsPrincipal principal, ClaimType types, CancellationToken token )
    {
        Claim[] array = principal.Claims.ToArray();
        return TryFromClaims(connection, transaction, db, array.AsValueEnumerable(), types, token);
    }
    public static ValueTask<ErrorOrResult<UserRecord>> TryFromClaims<TEnumerator>( NpgsqlConnection connection, NpgsqlTransaction transaction, Database db, ValueEnumerable<TEnumerator, Claim> claims, in ClaimType types, CancellationToken token )
        where TEnumerator : struct, IValueEnumerator<Claim>
    {
        PostgresParameters parameters = PostgresParameters.Create<UserRecord>();

        parameters.Add(nameof(ID),
                       Guid.Parse(claims.Single(static x => x.IsUserID())
                                        .Value));

        if ( types.HasFlag(ClaimType.UserName) )
        {
            parameters.Add(nameof(UserName),
                           claims.Single(static x => x.IsUserName())
                                 .Value);
        }

        if ( types.HasFlag(ClaimType.FirstName) )
        {
            parameters.Add(nameof(FirstName),
                           claims.Single(static x => x.IsFirstName())
                                 .Value);
        }

        if ( types.HasFlag(ClaimType.LastName) )
        {
            parameters.Add(nameof(LastName),
                           claims.Single(static x => x.IsLastName())
                                 .Value);
        }

        if ( types.HasFlag(ClaimType.FullName) )
        {
            parameters.Add(nameof(FullName),
                           claims.Single(static x => x.IsFullName())
                                 .Value);
        }

        if ( types.HasFlag(ClaimType.Email) )
        {
            parameters.Add(nameof(Email),
                           claims.Single(static x => x.IsEmail())
                                 .Value);
        }

        if ( types.HasFlag(ClaimType.MobilePhone) )
        {
            parameters.Add(nameof(PhoneNumber),
                           claims.Single(static x => x.IsMobilePhone())
                                 .Value);
        }

        if ( types.HasFlag(ClaimType.WebSite) )
        {
            parameters.Add(nameof(Website),
                           claims.Single(static x => x.IsWebSite())
                                 .Value);
        }

        return db.Users.Get(connection, transaction, true, parameters, token);
    }
    public static async IAsyncEnumerable<UserRecord> TryFromClaims( NpgsqlConnection connection, NpgsqlTransaction transaction, Database db, Claim claim, [EnumeratorCancellation] CancellationToken token = default )
    {
        PostgresParameters parameters = PostgresParameters.Create<UserRecord>();

        switch ( claim.Type )
        {
            case ClaimTypes.NameIdentifier:
                parameters.Add(nameof(UserName), claim.Value);
                break;

            case ClaimTypes.Sid:
                parameters.Add(nameof(ID), Guid.Parse(claim.Value));
                break;

            case ClaimTypes.GivenName:
                parameters.Add(nameof(FirstName), claim.Value);
                break;

            case ClaimTypes.Surname:
                parameters.Add(nameof(LastName), claim.Value);
                break;

            case ClaimTypes.Name:
                parameters.Add(nameof(FullName), claim.Value);
                break;

            case ClaimTypes.Email:
                parameters.Add(nameof(Email), claim.Value);
                break;

            case ClaimTypes.MobilePhone:
                parameters.Add(nameof(PhoneNumber), claim.Value);
                break;

            case ClaimTypes.Webpage:
                parameters.Add(nameof(Website), claim.Value);
                break;
        }

        await foreach ( UserRecord record in db.Users.Where(connection, transaction, true, parameters, token) ) { yield return record; }
    }

    #endregion
}
