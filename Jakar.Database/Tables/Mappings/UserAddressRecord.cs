// Jakar.Extensions :: Jakar.Database
// 4/4/2024  10:22

namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
[SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
public sealed record UserAddressRecord : Mapping<UserAddressRecord, UserRecord, AddressRecord>, ICreateMapping<UserAddressRecord, UserRecord, AddressRecord>
{
    public const  string TABLE_NAME = "user_adreesses";
    public static string TableName => TABLE_NAME;


    [ColumnInfo(UserRecord.TABLE_NAME)]    public override RecordID<UserRecord>    KeyID   { get; init; }
    [ColumnInfo(AddressRecord.TABLE_NAME)] public override RecordID<AddressRecord> ValueID { get; init; }


    public UserAddressRecord( UserRecord            key, AddressRecord           value ) : base(key, value) { }
    public UserAddressRecord( RecordID<UserRecord>  key, RecordID<AddressRecord> value ) : base(key, value) { }
    private UserAddressRecord( RecordID<UserRecord> key, RecordID<AddressRecord> value, DateTimeOffset dateCreated ) : base(key, value, dateCreated) { }
    internal UserAddressRecord( NpgsqlDataReader    reader ) : base(reader) { }


    [Pure] public static UserAddressRecord Create( UserRecord           key, AddressRecord           value ) => new(key, value);
    [Pure] public static UserAddressRecord Create( RecordID<UserRecord> key, RecordID<AddressRecord> value ) => new(key, value);
    [Pure] public static ImmutableArray<UserAddressRecord> Create( UserRecord key, params ReadOnlySpan<AddressRecord> values )
    {
        UserAddressRecord[] records = new UserAddressRecord[values.Length];
        for ( int i = 0; i < values.Length; i++ ) { records[i] = Create(key, values[i]); }

        return records.AsImmutableArray();
    }
    [Pure] public static ImmutableArray<UserAddressRecord> Create( RecordID<UserRecord> key, params ReadOnlySpan<RecordID<AddressRecord>> values )
    {
        UserAddressRecord[] records = new UserAddressRecord[values.Length];
        for ( int i = 0; i < values.Length; i++ ) { records[i] = Create(key, values[i]); }

        return records.AsImmutableArray();
    }
    [Pure] public static IEnumerable<UserAddressRecord> Create( UserRecord key, IEnumerable<AddressRecord> values )
    {
        foreach ( AddressRecord value in values ) { yield return Create(key, value); }
    }
    [Pure] public static IEnumerable<UserAddressRecord> Create( RecordID<UserRecord> key, IEnumerable<RecordID<AddressRecord>> values )
    {
        foreach ( RecordID<AddressRecord> value in values ) { yield return Create(key, value); }
    }
    [Pure] public static UserAddressRecord Create( NpgsqlDataReader reader ) => new UserAddressRecord(reader).Validate();


    public static bool operator >( UserAddressRecord  left, UserAddressRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( UserAddressRecord left, UserAddressRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( UserAddressRecord  left, UserAddressRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( UserAddressRecord left, UserAddressRecord right ) => left.CompareTo(right) <= 0;
}
