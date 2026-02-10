// Jakar.Database ::  Jakar.Database 
// 02/17/2023  2:40 PM

namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record UserRoleRecord : Mapping<UserRoleRecord, UserRecord, RoleRecord>, ICreateMapping<UserRoleRecord, UserRecord, RoleRecord>
{
    public const  string TABLE_NAME = "user_roles";
    public static string TableName => TABLE_NAME;


    [ColumnMetaData(UserRecord.TABLE_NAME)] public override RecordID<UserRecord> KeyID   { get; init; }
    [ColumnMetaData(RoleRecord.TABLE_NAME)] public override RecordID<RoleRecord> ValueID { get; init; }


    public UserRoleRecord( RecordID<UserRecord>  key, RecordID<RoleRecord> value ) : base(key, value) { }
    private UserRoleRecord( RecordID<UserRecord> key, RecordID<RoleRecord> value, DateTimeOffset dateCreated ) : base(key, value, dateCreated) { }
    internal UserRoleRecord( NpgsqlDataReader    reader ) : base(reader) { }


    [Pure] public static UserRoleRecord Create( NpgsqlDataReader     reader )                          => new UserRoleRecord(reader).Validate();
    [Pure] public static UserRoleRecord Create( UserRecord           key, RoleRecord           value ) => new(key, value);
    [Pure] public static UserRoleRecord Create( RecordID<UserRecord> key, RecordID<RoleRecord> value ) => new(key, value);
    [Pure] public static ImmutableArray<UserRoleRecord> Create( UserRecord key, params ReadOnlySpan<RoleRecord> values )
    {
        UserRoleRecord[] records = new UserRoleRecord[values.Length];
        for ( int i = 0; i < values.Length; i++ ) { records[i] = Create(key, values[i]); }

        return records.AsImmutableArray();
    }
    [Pure] public static ImmutableArray<UserRoleRecord> Create( RecordID<UserRecord> key, params ReadOnlySpan<RecordID<RoleRecord>> values )
    {
        UserRoleRecord[] records = new UserRoleRecord[values.Length];
        for ( int i = 0; i < values.Length; i++ ) { records[i] = Create(key, values[i]); }

        return records.AsImmutableArray();
    }
    [Pure] public static IEnumerable<UserRoleRecord> Create( UserRecord key, IEnumerable<RoleRecord> values )
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( RecordID<RoleRecord> value in values ) { yield return Create(key, value); }
    }
    [Pure] public static IEnumerable<UserRoleRecord> Create( RecordID<UserRecord> key, IEnumerable<RecordID<RoleRecord>> values )
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( RecordID<RoleRecord> value in values ) { yield return Create(key, value); }
    }


    public static bool operator >( UserRoleRecord  left, UserRoleRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( UserRoleRecord left, UserRoleRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( UserRoleRecord  left, UserRoleRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( UserRoleRecord left, UserRoleRecord right ) => left.CompareTo(right) <= 0;
}
