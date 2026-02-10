// Jakar.Extensions :: Jakar.Database
// 10/22/2025  23:12

namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record UserRecoveryCodeRecord : Mapping<UserRecoveryCodeRecord, UserRecord, RecoveryCodeRecord>, ICreateMapping<UserRecoveryCodeRecord, UserRecord, RecoveryCodeRecord>
{
    public const  string TABLE_NAME = "UserRecoveryCodes";
    public static string TableName => TABLE_NAME;


    [ColumnMetaData(UserRecord.TABLE_NAME)]         public override RecordID<UserRecord>         KeyID   { get; init; }
    [ColumnMetaData(RecoveryCodeRecord.TABLE_NAME)] public override RecordID<RecoveryCodeRecord> ValueID { get; init; }


    public UserRecoveryCodeRecord( RecordID<UserRecord> key, RecordID<RecoveryCodeRecord> value ) : base(key, value) { }
    public UserRecoveryCodeRecord( RecordID<UserRecord> key, RecordID<RecoveryCodeRecord> value, DateTimeOffset dateCreated ) : base(key, value, dateCreated) { }
    internal UserRecoveryCodeRecord( NpgsqlDataReader   reader ) : base(reader) { }


    public static UserRecoveryCodeRecord Create( UserRecord           key, RecoveryCodeRecord           value ) => new(key, value);
    public static UserRecoveryCodeRecord Create( RecordID<UserRecord> key, RecordID<RecoveryCodeRecord> value ) => new(key, value);
    [Pure] public static ImmutableArray<UserRecoveryCodeRecord> Create( UserRecord key, params ReadOnlySpan<RecoveryCodeRecord> values )
    {
        UserRecoveryCodeRecord[] records = GC.AllocateUninitializedArray<UserRecoveryCodeRecord>(values.Length);
        for ( int i = 0; i < values.Length; i++ ) { records[i] = Create(key.ID, values[i].ID); }

        return records.AsImmutableArray();
    }
    [Pure] public static ImmutableArray<UserRecoveryCodeRecord> Create( RecordID<UserRecord> key, params ReadOnlySpan<RecordID<RecoveryCodeRecord>> values )
    {
        UserRecoveryCodeRecord[] records = GC.AllocateUninitializedArray<UserRecoveryCodeRecord>(values.Length);
        for ( int i = 0; i < values.Length; i++ ) { records[i] = Create(key, values[i]); }

        return records.AsImmutableArray();
    }
    [Pure] public static IEnumerable<UserRecoveryCodeRecord> Create( UserRecord key, IEnumerable<RecoveryCodeRecord> values )
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( RecordID<RecoveryCodeRecord> value in values ) { yield return Create(key, value); }
    }
    [Pure] public static IEnumerable<UserRecoveryCodeRecord> Create( RecordID<UserRecord> key, IEnumerable<RecordID<RecoveryCodeRecord>> values )
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( RecordID<RecoveryCodeRecord> value in values ) { yield return Create(key, value); }
    }


    public static UserRecoveryCodeRecord Create( NpgsqlDataReader reader ) => new UserRecoveryCodeRecord(reader).Validate();
    public static async IAsyncEnumerable<UserRecoveryCodeRecord> CreateAsync( NpgsqlDataReader reader, [EnumeratorCancellation] CancellationToken token = default )
    {
        while ( await reader.ReadAsync(token) ) { yield return Create(reader); }
    }


    public static bool operator >( UserRecoveryCodeRecord  left, UserRecoveryCodeRecord right ) => Comparer<UserRecoveryCodeRecord>.Default.Compare(left, right) > 0;
    public static bool operator >=( UserRecoveryCodeRecord left, UserRecoveryCodeRecord right ) => Comparer<UserRecoveryCodeRecord>.Default.Compare(left, right) >= 0;
    public static bool operator <( UserRecoveryCodeRecord  left, UserRecoveryCodeRecord right ) => Comparer<UserRecoveryCodeRecord>.Default.Compare(left, right) < 0;
    public static bool operator <=( UserRecoveryCodeRecord left, UserRecoveryCodeRecord right ) => Comparer<UserRecoveryCodeRecord>.Default.Compare(left, right) <= 0;
}
