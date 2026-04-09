namespace Jakar.Database;


/// <see cref="ResxString"/>
[Serializable]
[Table(TABLE_NAME)]
public sealed record ResxRowRecord : TableRecord<ResxRowRecord>, ITableRecord<ResxRowRecord>, IResxString
{
    public const  string                              TABLE_NAME = "resx";
    public static string                              TableName      => TABLE_NAME;
    public        JObject?                            AdditionalData { get; set; }
    public        string                              Arabic         { get; init; } = EMPTY;
    public        string                              Chinese        { get; init; } = EMPTY;
    public        string                              Czech          { get; init; } = EMPTY;
    public        string                              Dutch          { get; init; } = EMPTY;
    public        string                              English        { get; init; } = EMPTY;
    public        string                              French         { get; init; } = EMPTY;
    public        string                              German         { get; init; } = EMPTY;
    long IUniqueID<long>.                             ID             => KeyID.Value;
    public                string                      Japanese       { get; init; } = EMPTY;
    public required       string                      Key            { get; init; }
    [Key] public required AutoRecordID<ResxRowRecord> KeyID          { get; init; }
    public                string                      Korean         { get; init; } = EMPTY;
    public                DateTimeOffset?             LastModified   { get; set; }
    public required       string                      Neutral        { get; init; }
    public                string                      Polish         { get; init; } = EMPTY;
    public                string                      Portuguese     { get; init; } = EMPTY;
    public                string                      Spanish        { get; init; } = EMPTY;
    public                string                      Swedish        { get; init; } = EMPTY;
    public                string                      Thai           { get; init; } = EMPTY;


    public ResxRowRecord( in DateTimeOffset dateCreated ) : base(in dateCreated) { }
    [SetsRequiredMembers] public ResxRowRecord( string key, string neutral = EMPTY ) : this(DateTimeOffset.UtcNow)
    {
        Key     = key;
        Neutral = neutral;
    }
    [SetsRequiredMembers] public ResxRowRecord( DbDataReader reader ) : base(reader)
    {
        KeyID      = AutoRecordID<ResxRowRecord>.Create(reader, nameof(KeyID));
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


    [Pure] public static ResxRowRecord Create( DbDataReader reader ) => new ResxRowRecord(reader).Validate();
    [Pure] public static async IAsyncEnumerable<ResxRowRecord> CreateAsync( DbDataReader reader, [EnumeratorCancellation] CancellationToken token = default )
    {
        while ( await reader.ReadAsync(token) ) { yield return Create(reader); }
    }


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    protected override async ValueTask Import( NpgsqlBinaryImporter importer, string propertyName, NpgsqlDbType postgresDbType, CancellationToken token )
    {
        switch ( propertyName )
        {
            case nameof(DateCreated):
                await importer.WriteAsync(DateCreated, postgresDbType, token);
                return;

            case nameof(LastModified):
                if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                return;

            case nameof(Key):
                await importer.WriteAsync(Key, postgresDbType, token);
                return;

            case nameof(KeyID):
                await importer.WriteAsync(KeyID, postgresDbType, token);
                return;

            case nameof(Neutral):
                await importer.WriteAsync(Neutral, postgresDbType, token);
                return;

            case nameof(English):
                await importer.WriteAsync(English, postgresDbType, token);
                return;

            case nameof(Spanish):
                await importer.WriteAsync(Spanish, postgresDbType, token);
                return;

            case nameof(French):
                await importer.WriteAsync(French, postgresDbType, token);
                return;

            case nameof(Swedish):
                await importer.WriteAsync(Swedish, postgresDbType, token);
                return;

            case nameof(German):
                await importer.WriteAsync(German, postgresDbType, token);
                return;

            case nameof(Polish):
                await importer.WriteAsync(Polish, postgresDbType, token);
                return;

            case nameof(Thai):
                await importer.WriteAsync(Thai, postgresDbType, token);
                return;

            case nameof(Japanese):
                await importer.WriteAsync(Japanese, postgresDbType, token);
                return;

            case nameof(Czech):
                await importer.WriteAsync(Czech, postgresDbType, token);
                return;

            case nameof(Portuguese):
                await importer.WriteAsync(Portuguese, postgresDbType, token);
                return;

            case nameof(Dutch):
                await importer.WriteAsync(Dutch, postgresDbType, token);
                return;

            case nameof(Korean):
                await importer.WriteAsync(Korean, postgresDbType, token);
                return;

            case nameof(Arabic):
                await importer.WriteAsync(Arabic, postgresDbType, token);
                return;

            default:
                throw new InvalidOperationException($"Unknown column: {propertyName}");
        }
    }
    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(KeyID)].DataColumn]      = KeyID;
        row[MetaData[nameof(Key)].DataColumn]        = Key;
        row[MetaData[nameof(Neutral)].DataColumn]    = Neutral;
        row[MetaData[nameof(English)].DataColumn]    = English;
        row[MetaData[nameof(Spanish)].DataColumn]    = Spanish;
        row[MetaData[nameof(French)].DataColumn]     = French;
        row[MetaData[nameof(Swedish)].DataColumn]    = Swedish;
        row[MetaData[nameof(German)].DataColumn]     = German;
        row[MetaData[nameof(Polish)].DataColumn]     = Polish;
        row[MetaData[nameof(Thai)].DataColumn]       = Thai;
        row[MetaData[nameof(Japanese)].DataColumn]   = Japanese;
        row[MetaData[nameof(Czech)].DataColumn]      = Czech;
        row[MetaData[nameof(Portuguese)].DataColumn] = Portuguese;
        row[MetaData[nameof(Dutch)].DataColumn]      = Dutch;
        row[MetaData[nameof(Korean)].DataColumn]     = Korean;
        row[MetaData[nameof(Arabic)].DataColumn]     = Arabic;
        return base.Import(row, token);
    }
    public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
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

        return Neutral == other.Neutral || KeyID == other.KeyID;
    }


    public static bool operator >( ResxRowRecord  left, ResxRowRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( ResxRowRecord left, ResxRowRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( ResxRowRecord  left, ResxRowRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( ResxRowRecord left, ResxRowRecord right ) => left.CompareTo(right) <= 0;
}
