namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed record GroupRecord : OwnedTableRecord<GroupRecord>, ITableRecord<GroupRecord>, IGroupModel<Guid>
{
    public const int    MAX_SIZE   = 1024;
    public const string TABLE_NAME = "groups";


    public static string                                                                      TableName      => TABLE_NAME;
    Guid? ICreatedByUser<Guid>.                                                               CreatedBy      => CreatedBy?.Value;
    [ColumnMetaData(ColumnOptions.Indexed | ColumnOptions.Fixed, MAX_SIZE)] public string?    NormalizedName { get; set; }
    Guid? IGroupModel<Guid>.                                                                  OwnerID        => CreatedBy?.Value;
    public                                                                         UserRights Rights         { get; set; }
    [ColumnMetaData(ColumnOptions.Indexed | ColumnOptions.Fixed, MAX_SIZE)] public string     NameOfGroup    { get; init; }


    public GroupRecord( string nameOfGroup, UserRights rights, RecordID<UserRecord>? owner = null, string? normalizedName = null ) : this(nameOfGroup, normalizedName, EMPTY, RecordID<GroupRecord>.New(), owner, DateTimeOffset.UtcNow) { }
    public GroupRecord( string NameOfGroup, string? NormalizedName, UserRights Rights, RecordID<GroupRecord> ID, RecordID<UserRecord>? __CreatedBy, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in __CreatedBy, in ID, in DateCreated, in LastModified)
    {
        this.NormalizedName = NormalizedName;
        this.Rights         = Rights;
        this.NameOfGroup    = NameOfGroup;
    }


    [Pure] public static GroupRecord Create( NpgsqlDataReader reader )
    {
        string                normalizedName = reader.GetFieldValue<GroupRecord, string>(nameof(NormalizedName));
        string                nameOfGroup    = reader.GetFieldValue<GroupRecord, string>(nameof(NameOfGroup));
        UserRights            rights         = reader.GetFieldValue<GroupRecord, string>(nameof(Rights));
        DateTimeOffset        dateCreated    = reader.GetFieldValue<GroupRecord, DateTimeOffset>(nameof(DateCreated));
        DateTimeOffset?       lastModified   = reader.GetFieldValue<GroupRecord, DateTimeOffset?>(nameof(LastModified));
        RecordID<UserRecord>? ownerUserID    = RecordID<UserRecord>.CreatedBy(reader);
        RecordID<GroupRecord> id             = RecordID<GroupRecord>.ID(reader);
        GroupRecord           record         = new(nameOfGroup, normalizedName, rights, id, ownerUserID, dateCreated, lastModified);
        return record.Validate();
    }
    [Pure] public static GroupRecord Create<TEnum>( string name, [HandlesResourceDisposal] Permissions<TEnum> rights, string? normalizedName = null, RecordID<UserRecord>? caller = null )
        where TEnum : unmanaged, Enum => new(name, normalizedName, rights.ToStringAndDispose(), RecordID<GroupRecord>.New(), caller, DateTimeOffset.UtcNow);


    public GroupModel ToGroupModel() => new(this);
    public TGroupModel ToGroupModel<TGroupModel>()
        where TGroupModel : class, IGroupModel<TGroupModel, Guid> => TGroupModel.Create(this);


    public static MigrationRecord CreateTable( ulong migrationID ) => MigrationRecord.CreateTable<RoleRecord>(migrationID);


    public override int CompareTo( GroupRecord? other )
    {
        if ( ReferenceEquals(this, other) ) { return 0; }

        if ( other is null ) { return 1; }

        int nameOfGroupComparison = string.Compare(NameOfGroup, other.NameOfGroup, StringComparison.Ordinal);
        if ( nameOfGroupComparison != 0 ) { return nameOfGroupComparison; }

        int normalizedNameComparison = string.Compare(NormalizedName, other.NormalizedName, StringComparison.Ordinal);
        if ( normalizedNameComparison != 0 ) { return normalizedNameComparison; }

        int lastModifiedComparison = Nullable.Compare(LastModified, other.LastModified);
        if ( lastModifiedComparison != 0 ) { return lastModifiedComparison; }

        return DateCreated.CompareTo(other.DateCreated);
    }
    public override bool Equals( GroupRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return base.Equals(other) && NameOfGroup == other.NameOfGroup && Rights == other.Rights && NormalizedName == other.NormalizedName;
    }
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), NameOfGroup, Rights, NormalizedName);


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        foreach ( ColumnMetaData column in PropertyMetaData.Values.OrderBy(static x => x.Index) )
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

                case nameof(Rights):
                    await importer.WriteAsync(Rights.Value, column.PostgresDbType, token);
                    break;

                case nameof(NameOfGroup):
                    await importer.WriteAsync(NameOfGroup, column.PostgresDbType, token);
                    break;

                case nameof(NormalizedName):
                    await importer.WriteAsync(NormalizedName, column.PostgresDbType, token);
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
    [Pure] public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(NormalizedName), NormalizedName);
        parameters.Add(nameof(NameOfGroup),    NameOfGroup);
        parameters.Add(nameof(CreatedBy),      CreatedBy);
        parameters.Add(nameof(Rights),         Rights);
        return parameters;
    }


    [Pure] public async ValueTask<ErrorOrResult<UserRecord>> GetOwner( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, CancellationToken token ) => await db.Users.Get(connection, transaction, CreatedBy, token);
    [Pure] public       IAsyncEnumerable<UserRecord>         GetUsers( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, CancellationToken token ) => UserGroupRecord.Where(connection, transaction, db.Users, ID, token);


    public static bool operator >( GroupRecord  left, GroupRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( GroupRecord left, GroupRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( GroupRecord  left, GroupRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( GroupRecord left, GroupRecord right ) => left.CompareTo(right) <= 0;
}
