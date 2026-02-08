// Jakar.Extensions :: Jakar.Database
// 01/29/2023  1:26 PM

namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record RecoveryCodeRecord : OwnedTableRecord<RecoveryCodeRecord>, ITableRecord<RecoveryCodeRecord>
{
    public const            string                             TABLE_NAME = "recovery_codes";
    private static readonly PasswordHasher<RecoveryCodeRecord> __hasher   = new();


    public static                                  string TableName => TABLE_NAME;
    [ColumnMetaData(ColumnOptions.Indexed)] public string Code      { get; init; }


    public RecoveryCodeRecord( string code, UserRecord                   user ) : this(code, RecordID<RecoveryCodeRecord>.New(), user.ID, DateTimeOffset.UtcNow) { }
    public RecoveryCodeRecord( string code, RecordID<RecoveryCodeRecord> ID, RecordID<UserRecord>? CreatedBy, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in CreatedBy, in ID, in DateCreated, in LastModified) => Code = __hasher.HashPassword(this, code);


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

                case nameof(Code):
                    await importer.WriteAsync(Code, column.PostgresDbType, token);
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
        parameters.Add(nameof(Code), Code);
        return parameters;
    }
    [Pure] public static RecoveryCodeRecord Create( NpgsqlDataReader reader )
    {
        string                       code         = reader.GetFieldValue<RecoveryCodeRecord, string>(nameof(Code));
        DateTimeOffset               dateCreated  = reader.GetFieldValue<RecoveryCodeRecord, DateTimeOffset>(nameof(DateCreated));
        DateTimeOffset?              lastModified = reader.GetFieldValue<RecoveryCodeRecord, DateTimeOffset?>(nameof(LastModified));
        RecordID<UserRecord>?        ownerUserID  = RecordID<UserRecord>.CreatedBy(reader);
        RecordID<RecoveryCodeRecord> id           = RecordID<RecoveryCodeRecord>.ID(reader);
        RecoveryCodeRecord           record       = new(code, id, ownerUserID, dateCreated, lastModified);
        return record.Validate();
    }


    public static MigrationRecord CreateTable( ulong migrationID ) => MigrationRecord.CreateTable<UserRecord>(migrationID);


    [Pure] public static Codes Create( UserRecord user, IEnumerable<string> recoveryCodes )
    {
        Codes codes = new();

        foreach ( string recoveryCode in recoveryCodes )
        {
            ( string code, RecoveryCodeRecord record ) = Create(user, recoveryCode);
            codes[code]                                = record;
        }

        return codes;
    }
    [Pure] public static Codes Create( UserRecord user, Extensions.ObservableCollection<string> recoveryCodes ) => Create(user, recoveryCodes.AsSpan());
    [Pure] public static Codes Create( UserRecord user, List<string>                            recoveryCodes ) => Create(user, recoveryCodes.AsSpan());
    [Pure] public static Codes Create( UserRecord user, scoped in ReadOnlyMemory<string>        recoveryCodes ) => Create(user, recoveryCodes.Span);
    [Pure] public static Codes Create( UserRecord user, params ReadOnlySpan<string> recoveryCodes )
    {
        Codes codes = new();

        foreach ( string recoveryCode in recoveryCodes )
        {
            ( string code, RecoveryCodeRecord record ) = Create(user, recoveryCode);
            codes[code]                                = record;
        }

        return codes;
    }
    [Pure] public static async ValueTask<Codes> Create( UserRecord user, IAsyncEnumerable<string> recoveryCodes, CancellationToken token = default )
    {
        Codes codes = new();

        await foreach ( string recoveryCode in recoveryCodes.WithCancellation(token) )
        {
            ( string code, RecoveryCodeRecord record ) = Create(user, recoveryCode);
            codes[code]                                = record;
        }

        return codes;
    }
    [Pure] public static Codes Create( UserRecord user, int count )
    {
        Codes codes = new();

        for ( int i = 0; i < count; i++ )
        {
            ( string code, RecoveryCodeRecord record ) = Create(user);
            codes[code]                                = record;
        }

        return codes;
    }


    public static (string Code, RecoveryCodeRecord Record) Create( UserRecord user )              => Create(user, Guid.CreateVersion7());
    public static (string Code, RecoveryCodeRecord Record) Create( UserRecord user, Guid   code ) => Create(user, code.ToHex());
    public static (string Code, RecoveryCodeRecord Record) Create( UserRecord user, string code ) => ( code, new RecoveryCodeRecord(code, user) );


    [Pure] public static bool IsValid( string code, ref RecoveryCodeRecord record )
    {
        PasswordVerificationResult result = __hasher.VerifyHashedPassword(record, record.Code, code);

        switch ( result )
        {
            case PasswordVerificationResult.Failed:
                return false;

            case PasswordVerificationResult.Success:
                return true;

            case PasswordVerificationResult.SuccessRehashNeeded:
                record = record with { Code = __hasher.HashPassword(record, code) };

                return true;

            default:
                throw new OutOfRangeException(result);
        }
    }


    public override bool Equals( RecoveryCodeRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return base.Equals(other) && string.Equals(Code, other.Code, StringComparison.InvariantCultureIgnoreCase);
    }
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Code);


    public static bool operator >( RecoveryCodeRecord  left, RecoveryCodeRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( RecoveryCodeRecord left, RecoveryCodeRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( RecoveryCodeRecord  left, RecoveryCodeRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( RecoveryCodeRecord left, RecoveryCodeRecord right ) => left.CompareTo(right) <= 0;



    public sealed class Codes()
    {
        private readonly SortedDictionary<string, RecoveryCodeRecord> __codes = new(StringComparer.Ordinal);

        public int Count => __codes.Count;
        public RecoveryCodeRecord this[ string key ] { get => __codes[key]; internal set => __codes[key] = value; }
        public SortedDictionary<string, RecoveryCodeRecord>.KeyCollection   Keys   => __codes.Keys;
        public SortedDictionary<string, RecoveryCodeRecord>.ValueCollection Values => __codes.Values;

        public SortedDictionary<string, RecoveryCodeRecord>.Enumerator GetEnumerator() => __codes.GetEnumerator();

        public bool ContainsKey( string key )                                                      => __codes.ContainsKey(key);
        public bool TryGetValue( string key, [MaybeNullWhen(false)] out RecoveryCodeRecord value ) => __codes.TryGetValue(key, out value);
    }
}
