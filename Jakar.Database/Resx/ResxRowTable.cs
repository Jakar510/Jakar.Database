namespace Jakar.Database.Resx;


/// <see cref="ResxString"/>
[Serializable]
[Table(TABLE_NAME)]
public sealed record ResxRowRecord : TableRecord<ResxRowRecord>, ITableRecord<ResxRowRecord>, IResxString
{
    public const    string TABLE_NAME = "resx";
    public static   string TableName  => TABLE_NAME;
    public required long   KeyID      { get; init; }
    public required string Key        { get; init; }
    public required string Neutral    { get; init; }
    public          string Arabic     { get; init; } = EMPTY;
    public          string Chinese    { get; init; } = EMPTY;
    public          string Czech      { get; init; } = EMPTY;
    public          string Dutch      { get; init; } = EMPTY;
    public          string English    { get; init; } = EMPTY;
    public          string French     { get; init; } = EMPTY;
    public          string German     { get; init; } = EMPTY;
    public          string Japanese   { get; init; } = EMPTY;
    public          string Korean     { get; init; } = EMPTY;
    public          string Polish     { get; init; } = EMPTY;
    public          string Portuguese { get; init; } = EMPTY;
    public          string Spanish    { get; init; } = EMPTY;
    public          string Swedish    { get; init; } = EMPTY;
    public          string Thai       { get; init; } = EMPTY;
    long IUniqueID<long>.  ID         => KeyID;


    public ResxRowRecord( in RecordID<ResxRowRecord> id, in DateTimeOffset dateCreated, in DateTimeOffset? lastModified, JObject? additionalData = null ) : base(in id, in dateCreated, in lastModified, additionalData) { }
    [SetsRequiredMembers] public ResxRowRecord( string key, long keyID, string neutral = EMPTY ) : base(RecordID<ResxRowRecord>.New(), DateTimeOffset.UtcNow, null)
    {
        KeyID   = keyID;
        Key     = key;
        Neutral = neutral;
    }
    [SetsRequiredMembers] public ResxRowRecord( NpgsqlDataReader reader ) : base(reader)
    {
        KeyID      = reader.GetFieldValue<ResxRowRecord, long>(nameof(KeyID));
        Key        = reader.GetFieldValue<ResxRowRecord, string>(nameof(Key));
        Neutral    = reader.GetFieldValue<ResxRowRecord, string>(nameof(Neutral));
        Arabic     = reader.GetFieldValue<ResxRowRecord, string>(nameof(Arabic));
        Chinese    = reader.GetFieldValue<ResxRowRecord, string>(nameof(Chinese));
        Czech      = reader.GetFieldValue<ResxRowRecord, string>(nameof(Czech));
        Dutch      = reader.GetFieldValue<ResxRowRecord, string>(nameof(Dutch));
        English    = reader.GetFieldValue<ResxRowRecord, string>(nameof(English));
        French     = reader.GetFieldValue<ResxRowRecord, string>(nameof(French));
        German     = reader.GetFieldValue<ResxRowRecord, string>(nameof(German));
        Japanese   = reader.GetFieldValue<ResxRowRecord, string>(nameof(Japanese));
        Korean     = reader.GetFieldValue<ResxRowRecord, string>(nameof(Korean));
        Polish     = reader.GetFieldValue<ResxRowRecord, string>(nameof(Polish));
        Portuguese = reader.GetFieldValue<ResxRowRecord, string>(nameof(Portuguese));
        Spanish    = reader.GetFieldValue<ResxRowRecord, string>(nameof(Spanish));
        Swedish    = reader.GetFieldValue<ResxRowRecord, string>(nameof(Swedish));
        Thai       = reader.GetFieldValue<ResxRowRecord, string>(nameof(Thai));
    }


    public static        MigrationRecord CreateTable( ulong       migrationID ) => MigrationRecord.CreateTable<ResxRowRecord>(migrationID);
    [Pure] public static ResxRowRecord   Create( NpgsqlDataReader reader )      => new ResxRowRecord(reader).Validate();
    [Pure] public static async IAsyncEnumerable<ResxRowRecord> CreateAsync( NpgsqlDataReader reader, [EnumeratorCancellation] CancellationToken token = default )
    {
        while ( await reader.ReadAsync(token) ) { yield return Create(reader); }
    }


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        foreach ( ColumnMetaData column in PropertyMetaData.Values.OrderBy(static x => x.Index) )
        {
            switch ( column.PropertyName )
            {
                case nameof(ID):
                    await importer.WriteAsync(ID.Value, NpgsqlDbType.Uuid, token);
                    break;

                case nameof(DateCreated):
                    await importer.WriteAsync(DateCreated, NpgsqlDbType.TimestampTz, token);
                    break;

                case nameof(Key):
                    await importer.WriteAsync(Key, NpgsqlDbType.Text, token);
                    break;

                case nameof(KeyID):
                    await importer.WriteAsync(KeyID, NpgsqlDbType.Integer, token);
                    break;

                case nameof(Neutral):
                    await importer.WriteAsync(Neutral, NpgsqlDbType.Text, token);
                    break;

                case nameof(English):
                    await importer.WriteAsync(English, NpgsqlDbType.Text, token);
                    break;

                case nameof(Spanish):
                    await importer.WriteAsync(Spanish, NpgsqlDbType.Text, token);
                    break;

                case nameof(French):
                    await importer.WriteAsync(French, NpgsqlDbType.Text, token);
                    break;

                case nameof(Swedish):
                    await importer.WriteAsync(Swedish, NpgsqlDbType.Text, token);
                    break;

                case nameof(German):
                    await importer.WriteAsync(German, NpgsqlDbType.Text, token);
                    break;

                case nameof(Polish):
                    await importer.WriteAsync(Polish, NpgsqlDbType.Text, token);
                    break;

                case nameof(Thai):
                    await importer.WriteAsync(Thai, NpgsqlDbType.Text, token);
                    break;

                case nameof(Japanese):
                    await importer.WriteAsync(Japanese, NpgsqlDbType.Text, token);
                    break;

                case nameof(Czech):
                    await importer.WriteAsync(Czech, NpgsqlDbType.Text, token);
                    break;

                case nameof(Portuguese):
                    await importer.WriteAsync(Portuguese, NpgsqlDbType.Text, token);
                    break;

                case nameof(Dutch):
                    await importer.WriteAsync(Dutch, NpgsqlDbType.Text, token);
                    break;

                case nameof(Korean):
                    await importer.WriteAsync(Korean, NpgsqlDbType.Text, token);
                    break;

                case nameof(Arabic):
                    await importer.WriteAsync(Arabic, NpgsqlDbType.Text, token);
                    break;

                case nameof(LastModified):
                    if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, NpgsqlDbType.TimestampTz, token); }
                    else { await importer.WriteNullAsync(token); }

                    break;

                default:
                    throw new InvalidOperationException($"Unknown column: {column.PropertyName}");
            }
        }
    }
    public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(KeyID),      KeyID);
        parameters.Add(nameof(Key),        Key);
        parameters.Add(nameof(Neutral),    Neutral);
        parameters.Add(nameof(English),    English);
        parameters.Add(nameof(Spanish),    Spanish);
        parameters.Add(nameof(French),     French);
        parameters.Add(nameof(Swedish),    Swedish);
        parameters.Add(nameof(German),     German);
        parameters.Add(nameof(Chinese),    Chinese);
        parameters.Add(nameof(Polish),     Polish);
        parameters.Add(nameof(Thai),       Thai);
        parameters.Add(nameof(Japanese),   Japanese);
        parameters.Add(nameof(Czech),      Czech);
        parameters.Add(nameof(Portuguese), Portuguese);
        parameters.Add(nameof(Dutch),      Dutch);
        parameters.Add(nameof(Korean),     Korean);
        parameters.Add(nameof(Arabic),     Arabic);
        return parameters;
    }


    public override int CompareTo( ResxRowRecord? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        return string.Compare(Neutral, other.Neutral, StringComparison.Ordinal);
    }
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(Neutral);
        hashCode.Add(English);
        hashCode.Add(Spanish);
        hashCode.Add(French);
        hashCode.Add(Swedish);
        hashCode.Add(German);
        hashCode.Add(Chinese);
        hashCode.Add(Polish);
        hashCode.Add(Thai);
        hashCode.Add(Japanese);
        hashCode.Add(Czech);
        hashCode.Add(Portuguese);
        hashCode.Add(Dutch);
        hashCode.Add(Korean);
        hashCode.Add(Arabic);
        return hashCode.ToHashCode();
    }


    [Pure] public ResxString ToResxString() => new(this);
    [Pure] public ResxRowRecord With( IResxString resx ) => this with
                                                            {
                                                                English = resx.English,
                                                                Spanish = resx.Spanish,
                                                                French = resx.French,
                                                                Swedish = resx.Swedish,
                                                                German = resx.German,
                                                                Chinese = resx.Chinese,
                                                                Polish = resx.Polish,
                                                                Thai = resx.Thai,
                                                                Japanese = resx.Japanese,
                                                                Czech = resx.Czech,
                                                                Portuguese = resx.Portuguese,
                                                                Dutch = resx.Dutch,
                                                                Korean = resx.Korean,
                                                                Arabic = resx.Arabic,
                                                                LastModified = DateTimeOffset.UtcNow
                                                            };


    public string GetValue( in SupportedLanguage language ) => language switch
                                                               {
                                                                   SupportedLanguage.English     => English,
                                                                   SupportedLanguage.Spanish     => Spanish,
                                                                   SupportedLanguage.French      => French,
                                                                   SupportedLanguage.Swedish     => Swedish,
                                                                   SupportedLanguage.German      => German,
                                                                   SupportedLanguage.Chinese     => Chinese,
                                                                   SupportedLanguage.Polish      => Polish,
                                                                   SupportedLanguage.Thai        => Thai,
                                                                   SupportedLanguage.Japanese    => Japanese,
                                                                   SupportedLanguage.Czech       => Czech,
                                                                   SupportedLanguage.Portuguese  => Portuguese,
                                                                   SupportedLanguage.Dutch       => Dutch,
                                                                   SupportedLanguage.Korean      => Korean,
                                                                   SupportedLanguage.Arabic      => Arabic,
                                                                   SupportedLanguage.Unspecified => throw new OutOfRangeException(language),
                                                                   _                             => throw new OutOfRangeException(language)
                                                               } ??
                                                               Neutral;
    public override bool Equals( ResxRowRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return Neutral == other.Neutral || ID == other.ID;
    }


    public static bool operator >( ResxRowRecord  left, ResxRowRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( ResxRowRecord left, ResxRowRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( ResxRowRecord  left, ResxRowRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( ResxRowRecord left, ResxRowRecord right ) => left.CompareTo(right) <= 0;
}
