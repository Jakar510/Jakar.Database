// Jakar.Database ::  Jakar.Database 
// 02/17/2023  2:39 PM

namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record UserGroupRecord : Mapping<UserGroupRecord, UserRecord, GroupRecord>, ICreateMapping<UserGroupRecord, UserRecord, GroupRecord>
{
    public const  string TABLE_NAME = "user_groups";
    public static string TableName => TABLE_NAME;


    [ColumnMetaData(UserRecord.TABLE_NAME)]  public override RecordID<UserRecord>  KeyID   { get; init; }
    [ColumnMetaData(GroupRecord.TABLE_NAME)] public override RecordID<GroupRecord> ValueID { get; init; }


    public UserGroupRecord( RecordID<UserRecord>  key, RecordID<GroupRecord> value ) : base(key, value) { }
    private UserGroupRecord( RecordID<UserRecord> key, RecordID<GroupRecord> value, DateTimeOffset dateCreated ) : base(key, value, dateCreated) { }
    internal UserGroupRecord( NpgsqlDataReader    reader ) : base(reader) { }


    [Pure] public static UserGroupRecord Create( UserRecord           key, GroupRecord           value ) => new(key, value);
    public static        UserGroupRecord Create( RecordID<UserRecord> key, RecordID<GroupRecord> value ) => new(key, value);
    [Pure] public static ImmutableArray<UserGroupRecord> Create( RecordID<UserRecord> key, params ReadOnlySpan<RecordID<GroupRecord>> values )
    {
        UserGroupRecord[] records = new UserGroupRecord[values.Length];
        for ( int i = 0; i < values.Length; i++ ) { records[i] = Create(key, values[i]); }

        return records.AsImmutableArray();
    }
    [Pure] public static ImmutableArray<UserGroupRecord> Create( UserRecord key, params ReadOnlySpan<GroupRecord> values )
    {
        UserGroupRecord[] records = new UserGroupRecord[values.Length];
        for ( int i = 0; i < values.Length; i++ ) { records[i] = Create(key, values[i]); }

        return records.AsImmutableArray();
    }
    [Pure] public static IEnumerable<UserGroupRecord> Create( RecordID<UserRecord> key, IEnumerable<RecordID<GroupRecord>> values )
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( RecordID<GroupRecord> value in values ) { yield return Create(key, value); }
    }
    [Pure] public static IEnumerable<UserGroupRecord> Create( UserRecord key, IEnumerable<GroupRecord> values )
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( RecordID<GroupRecord> value in values ) { yield return Create(key, value); }
    }
    [Pure] public static UserGroupRecord Create( NpgsqlDataReader reader ) => new UserGroupRecord(reader).Validate();


    public static bool operator >( UserGroupRecord  left, UserGroupRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( UserGroupRecord left, UserGroupRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( UserGroupRecord  left, UserGroupRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( UserGroupRecord left, UserGroupRecord right ) => left.CompareTo(right) <= 0;
}
