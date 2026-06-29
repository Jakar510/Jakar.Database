namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial record GroupRecord : OwnedTableRecord<GroupRecord>, ITableRecord<GroupRecord>, IGroupModel<Guid>
{
    public const int    MAX_SIZE   = 1024;
    public const string TABLE_NAME = "groups";

    // ReSharper disable once ReplaceWithFieldKeyword
    private static readonly    SqlName __tableName = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __tableName;

    Guid? ICreatedByUser<Guid>.                                                CreatedBy      => UserID.Value;
    [Unique] [Fixed(MAX_SIZE)] public string                                   NameOfGroup    { get; init; }
    [Unique] [Fixed(MAX_SIZE)] public string?                                  NormalizedName { get; set; }
    Guid? IGroupModel<Guid>.                                                   OwnerID        => UserID.Value;
    public                                                UserRights           Rights         { get; set; }
    [ForeignKey<GroupRecord, UserRecord>] public override RecordID<UserRecord> UserID         { get; init; }


    public GroupRecord( string nameOfGroup, UserRights rights, RecordID<UserRecord> userID = default, string? normalizedName = null ) : this(nameOfGroup, normalizedName, EMPTY, RecordID<GroupRecord>.New(), userID, DateTimeOffset.UtcNow) { }
    public GroupRecord( string NameOfGroup, string? NormalizedName, UserRights Rights, RecordID<GroupRecord> ID, RecordID<UserRecord> userID, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in userID, in ID, in DateCreated, in LastModified)
    {
        this.NormalizedName = NormalizedName;
        this.Rights         = Rights;
        this.NameOfGroup    = NameOfGroup;
    }


    [Pure] public static GroupRecord Create( DbDataReader reader )
    {
        string                normalizedName = reader.GetFieldValue<GroupRecord, string>(nameof(NormalizedName));
        string                nameOfGroup    = reader.GetFieldValue<GroupRecord, string>(nameof(NameOfGroup));
        UserRights            rights         = reader.GetFieldValue<GroupRecord, string>(nameof(Rights));
        DateTimeOffset        dateCreated    = reader.GetFieldValue<GroupRecord, DateTimeOffset>(nameof(DateCreated));
        DateTimeOffset?       lastModified   = reader.GetFieldValue<GroupRecord, DateTimeOffset?>(nameof(LastModified));
        RecordID<UserRecord>  ownerUserID    = RecordID<UserRecord>.UserID(reader);
        RecordID<GroupRecord> id             = RecordID<GroupRecord>.ID(reader);
        GroupRecord           record         = new(nameOfGroup, normalizedName, rights, id, ownerUserID, dateCreated, lastModified);
        return record.Validate();
    }
    [Pure] public static GroupRecord Create<TEnum>( string name, [HandlesResourceDisposal] Permissions<TEnum> rights, string? normalizedName = null, RecordID<UserRecord> userID = default )
        where TEnum : unmanaged, Enum => new(name, normalizedName, rights.ToStringAndDispose(), RecordID<GroupRecord>.New(), userID, DateTimeOffset.UtcNow);


    public GroupModel ToGroupModel() => new(this);
    public TGroupModel ToGroupModel<TGroupModel>()
        where TGroupModel : class, IGroupModel<TGroupModel, Guid> => TGroupModel.Create(this);


    [Pure] public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(NormalizedName), NormalizedName);
        parameters.Add(nameof(NameOfGroup),    NameOfGroup);
        parameters.Add(nameof(UserID),         UserID);
        parameters.Add(nameof(Rights),         Rights);
        return parameters;
    }


    [Pure] public async ValueTask<ErrorOrResult<UserRecord>> GetOwner( DbConnectionContext context, Database db, CancellationToken token ) => await db.Users.Get(context, UserID, token);
    [Pure] public       IAsyncEnumerable<UserRecord>         GetUsers( DbConnectionContext context, Database db, CancellationToken token ) => UserGroupRecord.Where(context, db.Users, ID, token);
}
