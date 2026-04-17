namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed record UserRecord : PairRecord<UserRecord>, ITableRecord<UserRecord>, IUserModel, IUserSecurity, IUserDetails
{
    public const               string  TABLE_NAME = "users";
    private static readonly    SqlName __sql_Name = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __sql_Name;

    public        RecordID<UserRecord>?                               EscalateTo          { get; set; }
    Guid? IEscalateToUser<Guid>.                                      EscalateTo          => EscalateTo?.Value;
    Guid? IImageID<Guid>.                                             ImageID             => ImageID?.Value;
    [ForeignKey<UserRecord, FileRecord>] public RecordID<FileRecord>? ImageID             { get; set; }
    public                                      bool                  IsValid             => !string.IsNullOrWhiteSpace(UserName) && ID.IsValid();
    public                                      SupportedLanguage     PreferredLanguage   { get; set; }
    public                                      DateTimeOffset?       SubscriptionExpires { get; set; }
    public                                      Guid?                 SubscriptionID      { get; set; }
    public                                      Guid                  UserID              => ID.Value;
    RecordID<UserRecord> IUserRecordID.                               UserID              => ID;
    [Fixed(USER_NAME)] [ProtectedPersonalData] public string          UserName            { get; init; } = EMPTY;
    [DbIgnore]                                 public bool            HasPassword         { [MemberNotNullWhen(true, nameof(PasswordHash))] get => !string.IsNullOrWhiteSpace(PasswordHash); }


    public UserRecord( in RecordID<UserRecord> ID ) : this(in ID, null, DateTimeOffset.UtcNow) { }
    public UserRecord( in RecordID<UserRecord> ID, UserRecord?             user,   in DateTimeOffset DateCreated, in DateTimeOffset? LastModified = null ) : this(in ID, user?.ID ?? RecordID<UserRecord>.Empty, in DateCreated, in LastModified) { }
    public UserRecord( in RecordID<UserRecord> ID, in RecordID<UserRecord> userID, in DateTimeOffset DateCreated, in DateTimeOffset? LastModified = null ) : base(in ID, in DateCreated, null, in LastModified) => EscalateTo = userID;
    internal UserRecord( DbDataReader reader ) : base(reader)
    {
        UserName               = reader.GetFieldValue<UserRecord, string>(nameof(UserName));
        FirstName              = reader.GetFieldValue<UserRecord, string?>(nameof(FirstName));
        LastName               = reader.GetFieldValue<UserRecord, string?>(nameof(LastName));
        FullName               = reader.GetFieldValue<UserRecord, string?>(nameof(FullName));
        Rights                 = reader.GetFieldValue<UserRecord, string>(nameof(Rights));
        Gender                 = reader.GetFieldValue<UserRecord, string?>(nameof(Gender));
        Company                = reader.GetFieldValue<UserRecord, string?>(nameof(Company));
        Description            = reader.GetFieldValue<UserRecord, string?>(nameof(Description));
        Department             = reader.GetFieldValue<UserRecord, string?>(nameof(Department));
        Title                  = reader.GetFieldValue<UserRecord, string?>(nameof(Title));
        Website                = reader.GetFieldValue<UserRecord, string?>(nameof(Website));
        PreferredLanguage      = reader.GetEnumValue<UserRecord, SupportedLanguage>(nameof(PreferredLanguage), SupportedLanguage.Unspecified);
        Email                  = reader.GetFieldValue<UserRecord, string?>(nameof(Email));
        PhoneNumber            = reader.GetFieldValue<UserRecord, string?>(nameof(PhoneNumber));
        Ext                    = reader.GetFieldValue<UserRecord, string?>(nameof(Ext));
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
        PasswordHash           = reader.GetFieldValue<UserRecord, string?>(nameof(PasswordHash));
        AuthenticatorKey       = reader.GetFieldValue<UserRecord, string?>(nameof(AuthenticatorKey));
        RefreshTokenHash       = reader.GetFieldValue<UserRecord, string?>(nameof(RefreshTokenHash));
        RefreshTokenExpiryTime = reader.GetFieldValue<UserRecord, DateTimeOffset?>(nameof(RefreshTokenExpiryTime));
        SessionID              = reader.GetFieldValue<UserRecord, Guid?>(nameof(SessionID));
        SecurityStamp          = reader.GetFieldValue<UserRecord, string?>(nameof(SecurityStamp));
        ConcurrencyStamp       = reader.GetFieldValue<UserRecord, string?>(nameof(ConcurrencyStamp));
        IsEmailConfirmed       = reader.GetFieldValue<UserRecord, bool>(nameof(IsEmailConfirmed));
        IsPhoneNumberConfirmed = reader.GetFieldValue<UserRecord, bool>(nameof(IsPhoneNumberConfirmed));
        IsTwoFactorEnabled     = reader.GetFieldValue<UserRecord, bool>(nameof(IsTwoFactorEnabled));
        IsLocked               = reader.GetFieldValue<UserRecord, bool>(nameof(IsLocked));
        IsActive               = reader.GetFieldValue<UserRecord, bool>(nameof(IsActive));
        IsDisabled             = reader.GetFieldValue<UserRecord, bool>(nameof(IsDisabled));
    }


    public UserRecord( IUserData<Guid> value ) : base(RecordID<UserRecord>.Create(value.ID), DateTimeOffset.UtcNow, value.AdditionalData) => With(value);
    public static UserRecord Create<TValue>( TValue value )
        where TValue : IUserData<Guid>, IUserDetails
    {
        UserRecord self = new(value);
        return self.With(value);
    }
    public UserRecord With<TValue>( TValue value )
        where TValue : IUserData<Guid>, IUserDetails
    {
        FirstName   = value.FirstName;
        LastName    = value.LastName;
        FullName    = value.FullName;
        Description = value.Description;
        Website     = value.Website;
        Email       = value.Email;
        PhoneNumber = value.PhoneNumber;
        Ext         = value.Ext;
        Title       = value.Title;
        Department  = value.Department;
        Company     = value.Company;
        return With((IUserData<Guid>)value);
    }
    public UserRecord With( IUserData<Guid> value )
    {
        if ( UserName != value.UserName ) { throw new InvalidOperationException($"Username mismatch: '{UserName}'"); }

        Rights            = value.Rights;
        ImageID           = RecordID<FileRecord>.Create(value.ImageID);
        EscalateTo        = RecordID<UserRecord>.Create(value.CreatedBy);
        EscalateTo        = RecordID<UserRecord>.Create(value.EscalateTo);
        PreferredLanguage = value.PreferredLanguage;
        Rights            = value.Rights;
        return WithAdditionalData(value.AdditionalData);
    }
    public string GetDescription() => Description ??= IUserDetails.GetDescription(this);


    public static UserRecord Create( DbDataReader reader ) => new UserRecord(reader).Validate();
    public static UserRecord Create<TUser>( ILoginRequest<TUser> request, UserRecord? caller = null )
        where TUser : class, IUserData<Guid>, IUserDetails => Create(request, request.Data.Rights, caller);
    public static UserRecord Create<TUser, TEnum>( ILoginRequest<TUser> request, scoped in Permissions<TEnum> rights, UserRecord? caller = null )
        where TUser : class, IUserData<Guid>, IUserDetails
        where TEnum : unmanaged, Enum => Create(request, rights.ToString(), caller);
    public static UserRecord Create<TUser>( ILoginRequest<TUser> request, UserRights rights, UserRecord? caller = null )
        where TUser : class, IUserData<Guid>, IUserDetails
    {
        ArgumentNullException.ThrowIfNull(request.Data);
        UserRecord user = Create(request.UserLogin, rights, request.Data, caller);

        return user.WithPassword(request.UserPassword).Enable();
    }

    public static UserRecord Create<TUser>( string userName, UserRights rights, TUser data, UserRecord? caller = null )
        where TUser : class, IUserData<Guid>, IUserDetails
    {
        RecordID<UserRecord> id = RecordID<UserRecord>.New();

        UserRecord user = new(id, caller, DateTimeOffset.UtcNow)
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
    public static UserRecord Create( string userName, string password, UserRights rights, UserRecord? caller = null ) => new UserRecord(RecordID<UserRecord>.New(), caller, DateTimeOffset.UtcNow)
                                                                                                                         {
                                                                                                                             UserName = userName,
                                                                                                                             Rights   = rights
                                                                                                                         }.WithPassword(password);


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

            case nameof(Rights):
                await importer.WriteAsync(Rights.Value, postgresDbType, token);
                break;

            case nameof(UserName):
                await importer.WriteAsync(UserName, postgresDbType, token);
                break;

            case nameof(FirstName):
                await importer.WriteAsync(FirstName, postgresDbType, token);
                break;

            case nameof(LastName):
                await importer.WriteAsync(LastName, postgresDbType, token);
                break;

            case nameof(FullName):
                await importer.WriteAsync(FullName, postgresDbType, token);
                break;

            case nameof(Gender):
                await importer.WriteAsync(Gender, postgresDbType, token);
                break;

            case nameof(Company):
                await importer.WriteAsync(Company, postgresDbType, token);
                break;

            case nameof(Department):
                await importer.WriteAsync(Department, postgresDbType, token);
                break;

            case nameof(Title):
                await importer.WriteAsync(Title, postgresDbType, token);
                break;

            case nameof(Website):
                await importer.WriteAsync(Website, postgresDbType, token);
                break;

            case nameof(PreferredLanguage):
                await importer.WriteAsync(PreferredLanguage, postgresDbType, token);
                break;

            case nameof(Email):
                await importer.WriteAsync(Email, postgresDbType, token);
                break;

            case nameof(LastModified):
                if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            case nameof(LastBadAttempt):
                if ( LastBadAttempt.HasValue ) { await importer.WriteAsync(LastBadAttempt.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            case nameof(LastLogin):
                if ( LastLogin.HasValue ) { await importer.WriteAsync(LastLogin.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            case nameof(BadLogins):
                if ( BadLogins.HasValue ) { await importer.WriteAsync(BadLogins.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            case nameof(LockDate):
                if ( LockDate.HasValue ) { await importer.WriteAsync(LockDate.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            case nameof(LockoutEnd):
                if ( LockoutEnd.HasValue ) { await importer.WriteAsync(LockoutEnd.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            case nameof(PasswordHash):
                await importer.WriteAsync(PasswordHash, postgresDbType, token);
                break;

            case nameof(RefreshTokenHash):
                await importer.WriteAsync(RefreshTokenHash, postgresDbType, token);
                break;

            case nameof(RefreshTokenExpiryTime):

                if ( RefreshTokenExpiryTime.HasValue ) { await importer.WriteAsync(RefreshTokenExpiryTime.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            case nameof(SessionID):
                if ( SessionID.HasValue ) { await importer.WriteAsync(SessionID.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            case nameof(IsActive):
                await importer.WriteAsync(IsActive, postgresDbType, token);
                break;

            case nameof(IsDisabled):
                await importer.WriteAsync(IsDisabled, postgresDbType, token);
                break;

            case nameof(SecurityStamp):
                await importer.WriteAsync(SecurityStamp, postgresDbType, token);
                break;

            case nameof(ConcurrencyStamp):
                await importer.WriteAsync(ConcurrencyStamp, postgresDbType, token);
                break;

            case nameof(AuthenticatorKey):
                await importer.WriteAsync(AuthenticatorKey, postgresDbType, token);
                break;

            case nameof(AdditionalData):
                await importer.WriteAsync(AdditionalData, postgresDbType, token);
                break;

            case nameof(EscalateTo):
                if ( EscalateTo.HasValue ) { await importer.WriteAsync(EscalateTo.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                break;

            default:
                throw new InvalidOperationException($"Unknown column: {propertyName}");
        }
    }
    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(Rights)].DataColumn]                 = Rights;
        row[MetaData[nameof(UserName)].DataColumn]               = UserName;
        row[MetaData[nameof(FirstName)].DataColumn]              = FirstName;
        row[MetaData[nameof(LastName)].DataColumn]               = LastName;
        row[MetaData[nameof(FullName)].DataColumn]               = FullName;
        row[MetaData[nameof(Gender)].DataColumn]                 = Gender;
        row[MetaData[nameof(Company)].DataColumn]                = Company;
        row[MetaData[nameof(Department)].DataColumn]             = Department;
        row[MetaData[nameof(Title)].DataColumn]                  = Title;
        row[MetaData[nameof(Website)].DataColumn]                = Website;
        row[MetaData[nameof(PreferredLanguage)].DataColumn]      = PreferredLanguage;
        row[MetaData[nameof(Email)].DataColumn]                  = Email;
        row[MetaData[nameof(LastBadAttempt)].DataColumn]         = LastBadAttempt;
        row[MetaData[nameof(LastLogin)].DataColumn]              = LastLogin;
        row[MetaData[nameof(BadLogins)].DataColumn]              = BadLogins;
        row[MetaData[nameof(LockDate)].DataColumn]               = LockDate;
        row[MetaData[nameof(LockoutEnd)].DataColumn]             = LockoutEnd;
        row[MetaData[nameof(PasswordHash)].DataColumn]           = PasswordHash;
        row[MetaData[nameof(RefreshTokenHash)].DataColumn]       = RefreshTokenHash;
        row[MetaData[nameof(RefreshTokenExpiryTime)].DataColumn] = RefreshTokenExpiryTime;
        row[MetaData[nameof(SessionID)].DataColumn]              = SessionID;
        row[MetaData[nameof(IsActive)].DataColumn]               = IsActive;
        row[MetaData[nameof(IsDisabled)].DataColumn]             = IsDisabled;
        row[MetaData[nameof(SecurityStamp)].DataColumn]          = SecurityStamp;
        row[MetaData[nameof(ConcurrencyStamp)].DataColumn]       = ConcurrencyStamp;
        row[MetaData[nameof(AuthenticatorKey)].DataColumn]       = AuthenticatorKey;
        row[MetaData[nameof(AdditionalData)].DataColumn]         = AdditionalData;
        row[MetaData[nameof(EscalateTo)].DataColumn]             = EscalateTo;
        return base.Import(row, token);
    }
    public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
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
        parameters.Add(nameof(EscalateTo),             EscalateTo);
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


    public static CommandParameters GetDynamicParameters( IUserDetails data )
    {
        CommandParameters parameters = CommandParameters.Create<UserRecord>();
        parameters.Add(nameof(Email),     data.Email);
        parameters.Add(nameof(FirstName), data.FirstName);
        parameters.Add(nameof(LastName),  data.LastName);
        parameters.Add(nameof(FullName),  data.FullName);
        return parameters;
    }
    public static CommandParameters GetDynamicParameters( ILoginRequest request )
    {
        CommandParameters parameters = CommandParameters.Create<UserRecord>();
        parameters.Add(nameof(UserName), request.UserLogin);
        return parameters;
    }
    public static CommandParameters GetDynamicParameters( string userName )
    {
        CommandParameters parameters = CommandParameters.Create<UserRecord>();
        parameters.Add(nameof(UserName), userName);
        return parameters;
    }
    public static CommandParameters GetDynamicParameters( RecordID<UserRecord> userID )
    {
        CommandParameters parameters = CommandParameters.Create<UserRecord>();
        parameters.Add(nameof(ID), userID);
        return parameters;
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
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), UserName);


    public static bool operator >( UserRecord  left, UserRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( UserRecord left, UserRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( UserRecord  left, UserRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( UserRecord left, UserRecord right ) => left.CompareTo(right) <= 0;


    public event PropertyChangedEventHandler? PropertyChanged;
    private void                              OnPropertyChanged( [CallerMemberName] string? propertyName = null ) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
    private bool SetField<T>( ref T field, T value, [CallerMemberName] string? propertyName = null )
    {
        if ( EqualityComparer<T>.Default.Equals(field, value) ) { return false; }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }



    #region Security

    public                            UserRights Rights           { get; set; } = new();
    [Fixed(AUTHENTICATOR_KEY)] public string?    AuthenticatorKey { get; set; } = EMPTY;
    public                            int?       BadLogins        { get; set; }

    /// <summary> A random value that must change whenever a user is persisted to the store </summary>
    [Fixed(CONCURRENCY_STAMP)] public string? ConcurrencyStamp { get; set; } = EMPTY;

    public                                      bool            IsActive               { get; set; }
    public                                      bool            IsDisabled             { get; set; }
    public                                      bool            IsEmailConfirmed       { get; set; }
    public                                      bool            IsLocked               { get; set; }
    public                                      bool            IsPhoneNumberConfirmed { get; set; }
    public                                      bool            IsTwoFactorEnabled     { get; set; }
    public                                      DateTimeOffset? LastBadAttempt         { get; set; }
    public                                      DateTimeOffset? LastLogin              { get; set; }
    public                                      DateTimeOffset? LockDate               { get; set; }
    public                                      DateTimeOffset? LockoutEnd             { get; set; }
    [Fixed(ENCRYPTED_MAX_PASSWORD_SIZE)] public string?         PasswordHash           { get; set; }
    [Fixed(REFRESH_TOKEN)]               public string?         RefreshTokenHash       { get; set; }
    public                                      DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    [Fixed(SECURITY_STAMP)] public              string?         SecurityStamp          { get; set; }
    public                                      Guid?           SessionID              { get; set; }

    #endregion Security



    #region Details

    [Fixed(COMPANY)] [ProtectedPersonalData] public string?                                              Company     { get; set; } = EMPTY;
    Guid? ICreatedByUser<Guid>.                                                                          CreatedBy   => EscalateTo?.Value;
    [Fixed(DEPARTMENT)]                                                                   public string? Department  { get; set; } = EMPTY;
    [Fixed(DESCRIPTION)]                                                                  public string? Description { get; set; }
    [Fixed(EMAIL)] [Indexed<UserRecord>(nameof(Email))] [ProtectedPersonalData]           public string? Email       { get; set; } = EMPTY;
    [Fixed(WEBSITE)] [ProtectedPersonalData]                                              public string? Website     { get; set; } = EMPTY;
    [Fixed(LAST_NAME)] [Indexed<UserRecord>(nameof(LastName))] [ProtectedPersonalData]    public string? LastName    { get; set; } = EMPTY;
    [Fixed(PHONE)] [Indexed<UserRecord>(    nameof(PhoneNumber))] [ProtectedPersonalData] public string? PhoneNumber { get; set; } = EMPTY;
    [Fixed(PHONE_EXT)] [ProtectedPersonalData]                                            public string? Ext         { get; set; } = EMPTY;
    [Fixed(TITLE)]                                                                        public string? Title       { get; set; } = EMPTY;
    [Fixed(FIRST_NAME)] [Indexed<UserRecord>(nameof(FirstName))] [ProtectedPersonalData]  public string? FirstName   { get; set; } = EMPTY;
    [Fixed(FULL_NAME)] [Indexed<UserRecord>( nameof(FullName))] [ProtectedPersonalData]   public string? FullName    { get; set; } = EMPTY;
    [Fixed(GENDER)] [ProtectedPersonalData]                                               public string? Gender      { get; set; } = EMPTY;

    #endregion Details



    #region Helpers

    public UserModel ToUserModel() => ToUserModel<UserModel>();
    public TSelf ToUserModel<TSelf>()
        where TSelf : UserModel<TSelf, Guid, UserAddress, GroupModel, RoleModel>, ICreateUserModel<TSelf, Guid, UserAddress, GroupModel, RoleModel>, IJsonModel<TSelf>, new() => ToUserModel<TSelf, UserAddress, GroupModel, RoleModel>();
    public TSelf ToUserModel<TSelf, TAddress, TGroupModel, TRoleModel>()
        where TSelf : class, IUserData<Guid, TAddress, TGroupModel, TRoleModel>, ICreateUserModel<TSelf, Guid, TAddress, TGroupModel, TRoleModel>, new()
        where TGroupModel : class, IGroupModel<TGroupModel, Guid>, IEquatable<TGroupModel>
        where TRoleModel : class, IRoleModel<TRoleModel, Guid>, IEquatable<TRoleModel>
        where TAddress : class, IAddress<TAddress, Guid>, IEquatable<TAddress> => TSelf.Create(this);


    public ValueTask<UserModel> ToUserModel( DbConnectionContext context, Database db, CancellationToken token ) => ToUserModel<UserModel>(context, db, token);
    public ValueTask<TSelf> ToUserModel<TSelf>( DbConnectionContext context, Database db, CancellationToken token )
        where TSelf : UserModel<TSelf, Guid, UserAddress, GroupModel, RoleModel>, ICreateUserModel<TSelf, Guid, UserAddress, GroupModel, RoleModel>, IJsonModel<TSelf>, new() => ToUserModel<TSelf, UserAddress, GroupModel, RoleModel>(context, db, token);
    public async ValueTask<TSelf> ToUserModel<TSelf, TAddress, TGroupModel, TRoleModel>( DbConnectionContext context, Database db, CancellationToken token )
        where TSelf : class, IUserData<Guid, TAddress, TGroupModel, TRoleModel>, ICreateUserModel<TSelf, Guid, TAddress, TGroupModel, TRoleModel>, new()
        where TGroupModel : class, IGroupModel<TGroupModel, Guid>, IEquatable<TGroupModel>
        where TRoleModel : class, IRoleModel<TRoleModel, Guid>, IEquatable<TRoleModel>
        where TAddress : class, IAddress<TAddress, Guid>, IEquatable<TAddress>
    {
        TSelf model = TSelf.Create(this);

        await model.Addresses.AddAsync(GetAddresses(context, db, token).Select(static x => x.ToAddressModel<TAddress>()), token);

        await model.Groups.AddAsync(GetGroups(context, db, token).Select(static x => x.ToGroupModel<TGroupModel>()), token);

        await model.Roles.AddAsync(GetRoles(context, db, token).Select(static x => x.ToRoleModel<TRoleModel>()), token);

        return model;
    }


    public ValueTask<bool> RedeemCode( Database db, string code, CancellationToken token ) => db.TryCall(RedeemCode, db, code, token);
    public async ValueTask<bool> RedeemCode( DbConnectionContext context, Database db, string code, CancellationToken token )
    {
        await foreach ( RecoveryCodeRecord record in UserRecoveryCodeRecord.Where(context, db.RecoveryCodes, ID, token) )
        {
            if ( RecoveryCodeRecord.IsValid(code, record) )
            {
                await db.RecoveryCodes.Delete(context, record, token);
                await UserRecoveryCodeRecord.Delete(context, ID, record.ID, token);
                return true;
            }
        }

        return false;
    }
    public ValueTask<ImmutableArray<string>> ReplaceCodes( Database db, int count = 10, CancellationToken token = default ) => db.TryCall(ReplaceCodes, db, count, token);
    public async ValueTask<ImmutableArray<string>> ReplaceCodes( DbConnectionContext context, Database db, int count = 10, CancellationToken token = default )
    {
        IAsyncEnumerable<RecoveryCodeRecord> old        = Codes(context, db, token);
        RecoveryCodeRecord.Codes             dictionary = RecoveryCodeRecord.Create(this, count);
        ImmutableArray<string>               codes      = [..dictionary.Keys];


        await db.RecoveryCodes.Delete(context, old, token);
        await UserRecoveryCodeRecord.Replace(context, this, RecordID<RecoveryCodeRecord>.Create(dictionary.Values), token);
        return codes;
    }
    public ValueTask<ImmutableArray<string>> ReplaceCodes( Database db, IEnumerable<string> recoveryCodes, CancellationToken token = default ) => db.TryCall(ReplaceCodes, db, recoveryCodes, token);
    public async ValueTask<ImmutableArray<string>> ReplaceCodes( DbConnectionContext context, Database db, IEnumerable<string> recoveryCodes, CancellationToken token = default )
    {
        IAsyncEnumerable<RecoveryCodeRecord> old        = Codes(context, db, token);
        RecoveryCodeRecord.Codes             dictionary = RecoveryCodeRecord.Create(this, recoveryCodes);
        ImmutableArray<string>               codes      = [.. dictionary.Keys];


        await db.RecoveryCodes.Delete(context, old, token);
        await UserRecoveryCodeRecord.Replace(context, this, RecordID<RecoveryCodeRecord>.Create(dictionary.Values), token);
        return codes;
    }


    public       IAsyncEnumerable<RecoveryCodeRecord> Codes( Database                    db,      CancellationToken token )                                                             => db.TryCall(Codes, db, token);
    public       IAsyncEnumerable<RecoveryCodeRecord> Codes( DbConnectionContext         context, Database          db,    CancellationToken                          token )           => UserRecoveryCodeRecord.Where(context, db.RecoveryCodes, this, token);
    public async ValueTask<bool>                      TryAdd( DbConnectionContext        context, AddressRecord     value, CancellationToken                          token )           => await UserAddressRecord.TryAdd(context, ID, value, token);
    public       IAsyncEnumerable<AddressRecord>      GetAddresses( DbConnectionContext  context, Database          db,    [EnumeratorCancellation] CancellationToken token = default ) => UserAddressRecord.Where(context, db.Addresses, ID, token);
    public async ValueTask<bool>                      HasAddress( DbConnectionContext    context, AddressRecord     value, CancellationToken                          token )           => await UserAddressRecord.Exists(context, ID, value, token);
    public async ValueTask                            Remove( DbConnectionContext        context, AddressRecord     value, CancellationToken                          token )           => await UserAddressRecord.Delete(context, ID, value, token);
    public async ValueTask<bool>                      TryAdd( DbConnectionContext        context, RoleRecord        value, CancellationToken                          token )           => await UserRoleRecord.TryAdd(context, ID, value, token);
    public       IAsyncEnumerable<RoleRecord>         GetRoles( DbConnectionContext      context, Database          db,    CancellationToken                          token = default ) => UserRoleRecord.Where(context, db.Roles, ID, token);
    public async ValueTask<bool>                      HasRole( DbConnectionContext       context, RoleRecord        value, CancellationToken                          token )           => await UserRoleRecord.Exists(context, ID, value, token);
    public async ValueTask                            Remove( DbConnectionContext        context, RoleRecord        value, CancellationToken                          token )           => await UserRoleRecord.Delete(context, ID, value, token);
    public async ValueTask<bool>                      TryAdd( DbConnectionContext        context, GroupRecord       value, CancellationToken                          token )           => await UserGroupRecord.TryAdd(context, ID, value, token);
    public       IAsyncEnumerable<GroupRecord>        GetGroups( DbConnectionContext     context, Database          db,    CancellationToken                          token = default ) => UserGroupRecord.Where(context, db.Groups, ID, token);
    public async ValueTask<bool>                      IsPartOfGroup( DbConnectionContext context, GroupRecord       value, CancellationToken                          token )           => await UserGroupRecord.Exists(context, ID, value, token);
    public async ValueTask                            Remove( DbConnectionContext        context, GroupRecord       value, CancellationToken                          token )           => await UserGroupRecord.Delete(context, ID, value, token);

    #endregion Helpers



    #region Owners

    public async ValueTask<ErrorOrResult<UserRecord>> GetBoss( DbConnectionContext context, Database db, CancellationToken token ) =>
        EscalateTo.HasValue
            ? await db.Users.Get(context, EscalateTo.Value, token)
            : Error.Gone();


    public bool DoesNotOwn<TSelf>( TSelf record )
        where TSelf : TableRecord<TSelf>, IUserRecordID, ITableRecord<TSelf> => record.UserID != ID;
    public bool Owns<TSelf>( TSelf record )
        where TSelf : TableRecord<TSelf>, IUserRecordID, ITableRecord<TSelf> => record.UserID == ID;

    #endregion Owners



    #region Claims

    public async ValueTask<Claim[]> GetUserClaims( DbConnectionContext context, Database db, ClaimType types, CancellationToken token )
    {
        UserModel model = await ToUserModel(context, db, token);
        return model.GetClaims(types);
    }
    public static ValueTask<ErrorOrResult<UserRecord>> TryFromClaims( DbConnectionContext context, Database db, ClaimsPrincipal principal, ClaimType types, CancellationToken token )
    {
        Claim[] array = principal.Claims.ToArray();
        return TryFromClaims(context, db, array.AsValueEnumerable(), types, token);
    }
    public static ValueTask<ErrorOrResult<UserRecord>> TryFromClaims<TEnumerator>( DbConnectionContext context, Database db, ValueEnumerable<TEnumerator, Claim> claims, in ClaimType types, CancellationToken token )
        where TEnumerator : struct, IValueEnumerator<Claim>
    {
        CommandParameters parameters = CommandParameters.Create<UserRecord>();

        parameters.Add(nameof(ID), Guid.Parse(claims.Single(static x => x.IsUserID()).Value));

        if ( types.HasFlag(ClaimType.UserName) ) { parameters.Add(nameof(UserName), claims.Single(static x => x.IsUserName()).Value); }

        if ( types.HasFlag(ClaimType.FirstName) ) { parameters.Add(nameof(FirstName), claims.Single(static x => x.IsFirstName()).Value); }

        if ( types.HasFlag(ClaimType.LastName) ) { parameters.Add(nameof(LastName), claims.Single(static x => x.IsLastName()).Value); }

        if ( types.HasFlag(ClaimType.FullName) ) { parameters.Add(nameof(FullName), claims.Single(static x => x.IsFullName()).Value); }

        if ( types.HasFlag(ClaimType.Email) ) { parameters.Add(nameof(Email), claims.Single(static x => x.IsEmail()).Value); }

        if ( types.HasFlag(ClaimType.MobilePhone) ) { parameters.Add(nameof(PhoneNumber), claims.Single(static x => x.IsMobilePhone()).Value); }

        if ( types.HasFlag(ClaimType.WebSite) ) { parameters.Add(nameof(Website), claims.Single(static x => x.IsWebSite()).Value); }

        return db.Users.Get(context, parameters, token);
    }
    public static async IAsyncEnumerable<UserRecord> TryFromClaims( DbConnectionContext context, Database db, Claim claim, [EnumeratorCancellation] CancellationToken token = default )
    {
        CommandParameters parameters = CommandParameters.Create<UserRecord>();

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

        await foreach ( UserRecord record in db.Users.Where(context, parameters, token) ) { yield return record; }
    }

    #endregion Claims
}
