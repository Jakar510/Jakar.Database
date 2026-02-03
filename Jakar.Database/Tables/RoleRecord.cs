using Jakar.Extensions;

using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record RoleRecord : OwnedTableRecord<RoleRecord>, ITableRecord<RoleRecord>, IRoleModel<Guid>
{
    public const string TABLE_NAME = "roles";


    public static                                                     string     TableName        => TABLE_NAME;
    [ColumnMetaData(ColumnOptions.None,    RIGHTS)]            public UserRights Rights           { get; set; }
    [ColumnMetaData(ColumnOptions.Indexed, NAME)]              public string     NameOfRole       { get; init; }
    [ColumnMetaData(ColumnOptions.Indexed, NORMALIZED_NAME)]   public string     NormalizedName   { get; init; }
    [ColumnMetaData(ColumnOptions.None,    CONCURRENCY_STAMP)] public string     ConcurrencyStamp { get; init; }


    public RoleRecord( IdentityRole role, RecordID<UserRecord>? caller                               = null ) : this(role.Name ?? EMPTY, role.NormalizedName ?? EMPTY, role.ConcurrencyStamp ?? EMPTY, caller) { }
    public RoleRecord( IdentityRole role, string                rights, RecordID<UserRecord>? caller = null ) : this(role.Name ?? EMPTY, role.NormalizedName ?? EMPTY, role.ConcurrencyStamp ?? EMPTY, rights, caller) { }
    public RoleRecord( string       name, RecordID<UserRecord>? caller                                                                                                             = null ) : this(name, name, caller) { }
    public RoleRecord( string       name, string                normalizedName, RecordID<UserRecord>? caller                                                                       = null ) : this(name, normalizedName, name.GetHash(), EMPTY, RecordID<RoleRecord>.New(), caller, DateTimeOffset.UtcNow) { }
    public RoleRecord( string       name, string                normalizedName, string                concurrencyStamp, RecordID<UserRecord>? caller                               = null ) : this(name, normalizedName, concurrencyStamp, EMPTY, RecordID<RoleRecord>.New(), caller, DateTimeOffset.UtcNow) { }
    public RoleRecord( string       name, string                normalizedName, string                concurrencyStamp, string                rights, RecordID<UserRecord>? caller = null ) : this(name, normalizedName, concurrencyStamp, rights, RecordID<RoleRecord>.New(), caller, DateTimeOffset.UtcNow) { }
    public RoleRecord( string NameOfRole, string NormalizedName, string ConcurrencyStamp, UserRights Rights, RecordID<RoleRecord> ID, RecordID<UserRecord>? CreatedBy, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in CreatedBy, in ID, in DateCreated, in LastModified)
    {
        this.Rights           = Rights;
        this.NameOfRole       = NameOfRole;
        this.NormalizedName   = NormalizedName;
        this.ConcurrencyStamp = ConcurrencyStamp;
    }
    internal RoleRecord( NpgsqlDataReader reader ) : base(reader)
    {
        Rights           = reader.GetFieldValue<RoleRecord, string>(nameof(Rights));
        NameOfRole       = reader.GetFieldValue<RoleRecord, string>(nameof(NameOfRole));
        NormalizedName   = reader.GetFieldValue<RoleRecord, string>(nameof(NormalizedName));
        ConcurrencyStamp = reader.GetFieldValue<RoleRecord, string>(nameof(ConcurrencyStamp));
    }


    public RoleModel ToRoleModel() => new(this);
    public TRoleModel ToRoleModel<TRoleModel>()
        where TRoleModel : class, IRoleModel<TRoleModel, Guid> => TRoleModel.Create(this);


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

                case nameof(NameOfRole):
                    await importer.WriteAsync(NameOfRole, column.PostgresDbType, token);
                    break;

                case nameof(NormalizedName):
                    await importer.WriteAsync(NormalizedName, column.PostgresDbType, token);
                    break;

                case nameof(ConcurrencyStamp):
                    await importer.WriteAsync(ConcurrencyStamp, column.PostgresDbType, token);
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
        parameters.Add(nameof(NameOfRole),       NameOfRole);
        parameters.Add(nameof(NormalizedName),   NormalizedName);
        parameters.Add(nameof(ConcurrencyStamp), ConcurrencyStamp);
        parameters.Add(nameof(Rights),           Rights);
        return parameters;
    }


    [Pure] public static RoleRecord Create<TEnum>( string name, [HandlesResourceDisposal] Permissions<TEnum> rights, string? normalizedName = null, RecordID<UserRecord>? caller = null, string? concurrencyStamp = null )
        where TEnum : unmanaged, Enum => new(name, normalizedName ?? name, concurrencyStamp ?? name.GetHash(), rights.ToStringAndDispose(), caller);
    [Pure] public static RoleRecord      Create( NpgsqlDataReader reader )      => new RoleRecord(reader).Validate();
    public static        MigrationRecord CreateTable( ulong       migrationID ) => MigrationRecord.CreateTable<RoleRecord>(migrationID);


    [Pure] public IAsyncEnumerable<UserRecord> GetUsers( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, CancellationToken token ) => UserRoleRecord.Where(connection, transaction, db.Users, this, token);


    [Pure] public IdentityRole ToIdentityRole() => new()
                                                   {
                                                       Name             = NameOfRole,
                                                       NormalizedName   = NormalizedName,
                                                       ConcurrencyStamp = ConcurrencyStamp
                                                   };


    public override bool Equals( RoleRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return base.Equals(other) && string.Equals(NameOfRole, other.NameOfRole, StringComparison.InvariantCultureIgnoreCase) && string.Equals(NormalizedName, other.NormalizedName, StringComparison.InvariantCultureIgnoreCase);
    }
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), NameOfRole, NormalizedName, Rights);
    public override int CompareTo( RoleRecord? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        int nameComparison = string.Compare(NameOfRole, other.NameOfRole, StringComparison.Ordinal);
        if ( nameComparison != 0 ) { return nameComparison; }

        int normalizedNameComparison = string.Compare(NormalizedName, other.NormalizedName, StringComparison.Ordinal);
        if ( normalizedNameComparison != 0 ) { return normalizedNameComparison; }

        int concurrencyComparison = string.Compare(ConcurrencyStamp, other.ConcurrencyStamp, StringComparison.Ordinal);
        if ( concurrencyComparison != 0 ) { return concurrencyComparison; }

        return base.CompareTo(other);
    }


    public static bool operator >( RoleRecord  left, RoleRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( RoleRecord left, RoleRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( RoleRecord  left, RoleRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( RoleRecord left, RoleRecord right ) => left.CompareTo(right) <= 0;
}
