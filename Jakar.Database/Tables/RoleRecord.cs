namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed partial record RoleRecord : OwnedTableRecord<RoleRecord>, ITableRecord<RoleRecord>, IRoleModel<Guid>
{
    public const string TABLE_NAME = "roles";

    // ReSharper disable once ReplaceWithFieldKeyword
    private static readonly    SqlName __tableName = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __tableName;

    [Fixed(CONCURRENCY_STAMP)]                                                            public             string               ConcurrencyStamp { get; init; }
    [Fixed(NAME)] [Unique] [StringCompare(StringComparison.InvariantCultureIgnoreCase)]   public             string               NameOfRole       { get; init; }
    [Fixed(NORMALIZED_NAME)] [Unique] [StringCompare(StringComparison.InvariantCultureIgnoreCase)] public    string               NormalizedName   { get; init; }
    public                                               UserRights           Rights           { get; set; }
    [ForeignKey<RoleRecord, UserRecord>] public override RecordID<UserRecord> UserID           { get; init; }


    public RoleRecord( IdentityRole role, RecordID<UserRecord> userID                              = default ) : this(role.Name ?? EMPTY, role.NormalizedName ?? EMPTY, role.ConcurrencyStamp ?? EMPTY, userID) { }
    public RoleRecord( IdentityRole role, string               rights, RecordID<UserRecord> userID = default ) : this(role.Name ?? EMPTY, role.NormalizedName ?? EMPTY, role.ConcurrencyStamp ?? EMPTY, rights, userID) { }
    public RoleRecord( string       name, RecordID<UserRecord> userID                                                                                                          = default ) : this(name, name, userID) { }
    public RoleRecord( string       name, string               normalizedName, RecordID<UserRecord> userID                                                                     = default ) : this(name, normalizedName, name.GetHash(), EMPTY, RecordID<RoleRecord>.New(), userID, DateTimeOffset.UtcNow) { }
    public RoleRecord( string       name, string               normalizedName, string               concurrencyStamp, RecordID<UserRecord> userID                              = default ) : this(name, normalizedName, concurrencyStamp, EMPTY, RecordID<RoleRecord>.New(), userID, DateTimeOffset.UtcNow) { }
    public RoleRecord( string       name, string               normalizedName, string               concurrencyStamp, string               rights, RecordID<UserRecord> userID = default ) : this(name, normalizedName, concurrencyStamp, rights, RecordID<RoleRecord>.New(), userID, DateTimeOffset.UtcNow) { }
    public RoleRecord( string NameOfRole, string NormalizedName, string ConcurrencyStamp, UserRights Rights, RecordID<RoleRecord> ID, RecordID<UserRecord> UserID, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in UserID, in ID, in DateCreated, in LastModified)
    {
        this.Rights           = Rights;
        this.NameOfRole       = NameOfRole;
        this.NormalizedName   = NormalizedName;
        this.ConcurrencyStamp = ConcurrencyStamp;
    }
    internal RoleRecord( DbDataReader reader ) : base(reader)
    {
        Rights           = reader.GetFieldValue<RoleRecord, string>(nameof(Rights));
        NameOfRole       = reader.GetFieldValue<RoleRecord, string>(nameof(NameOfRole));
        NormalizedName   = reader.GetFieldValue<RoleRecord, string>(nameof(NormalizedName));
        ConcurrencyStamp = reader.GetFieldValue<RoleRecord, string>(nameof(ConcurrencyStamp));
    }


    public RoleModel ToRoleModel() => new(this);
    public TRoleModel ToRoleModel<TRoleModel>()
        where TRoleModel : class, IRoleModel<TRoleModel, Guid> => TRoleModel.Create(this);


    [Pure] public static RoleRecord Create<TEnum>( string name, [HandlesResourceDisposal] Permissions<TEnum> rights, string? normalizedName = null, RecordID<UserRecord> userID = default, string? concurrencyStamp = null )
        where TEnum : unmanaged, Enum => new(name, normalizedName ?? name, concurrencyStamp ?? name.GetHash(), rights.ToStringAndDispose(), userID);


    [Pure] public IAsyncEnumerable<UserRecord> GetUsers( DbConnectionContext context, Database db, CancellationToken token ) => UserRoleRecord.Where(context, db.Users, this, token);


    [Pure] public IdentityRole ToIdentityRole() => new()
                                                   {
                                                       Name             = NameOfRole,
                                                       NormalizedName   = NormalizedName,
                                                       ConcurrencyStamp = ConcurrencyStamp
                                                   };


}
