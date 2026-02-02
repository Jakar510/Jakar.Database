// Jakar.Extensions :: Jakar.Database
// 10/22/2025  23:12

namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record UserRecoveryCodeRecord : Mapping<UserRecoveryCodeRecord, UserRecord, RecoveryCodeRecord>, ICreateMapping<UserRecoveryCodeRecord, UserRecord, RecoveryCodeRecord>
{
    public const  string TABLE_NAME = "UserRecoveryCodes";
    public static string TableName => TABLE_NAME;


    public UserRecoveryCodeRecord( RecordID<UserRecord> key, RecordID<RecoveryCodeRecord> value ) : base(key, value) { }
    public UserRecoveryCodeRecord( RecordID<UserRecord> key, RecordID<RecoveryCodeRecord> value, RecordID<UserRecoveryCodeRecord> id, DateTimeOffset dateCreated, DateTimeOffset? lastModified = null ) : base(key, value, id, dateCreated, lastModified) { }


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


    public static UserRecoveryCodeRecord Create( NpgsqlDataReader reader )
    {
        RecordID<UserRecord>             key          = new(reader.GetFieldValue<UserRecoveryCodeRecord, Guid>(nameof(KeyID)));
        RecordID<RecoveryCodeRecord>     value        = new(reader.GetFieldValue<UserRecoveryCodeRecord, Guid>(nameof(KeyID)));
        DateTimeOffset                   dateCreated  = reader.GetFieldValue<UserRecoveryCodeRecord, DateTimeOffset>(nameof(DateCreated));
        DateTimeOffset?                  lastModified = reader.GetFieldValue<UserRecoveryCodeRecord, DateTimeOffset?>(nameof(LastModified));
        RecordID<UserRecoveryCodeRecord> id           = RecordID<UserRecoveryCodeRecord>.ID(reader);
        return new UserRecoveryCodeRecord(key, value, id, dateCreated, lastModified);
    }
    public static async IAsyncEnumerable<UserRecoveryCodeRecord> CreateAsync( NpgsqlDataReader reader, [EnumeratorCancellation] CancellationToken token = default )
    {
        while ( await reader.ReadAsync(token) ) { yield return Create(reader); }
    }


    public static bool operator >( UserRecoveryCodeRecord  left, UserRecoveryCodeRecord right ) => Comparer<UserRecoveryCodeRecord>.Default.Compare(left, right) > 0;
    public static bool operator >=( UserRecoveryCodeRecord left, UserRecoveryCodeRecord right ) => Comparer<UserRecoveryCodeRecord>.Default.Compare(left, right) >= 0;
    public static bool operator <( UserRecoveryCodeRecord  left, UserRecoveryCodeRecord right ) => Comparer<UserRecoveryCodeRecord>.Default.Compare(left, right) < 0;
    public static bool operator <=( UserRecoveryCodeRecord left, UserRecoveryCodeRecord right ) => Comparer<UserRecoveryCodeRecord>.Default.Compare(left, right) <= 0;
}
