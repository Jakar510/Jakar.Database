namespace Jakar.Database;


/// <see cref="ResxString"/>
[Serializable]
[Table(TABLE_NAME)]
public sealed partial record ResxRowRecord : TableRecord<ResxRowRecord>, ITableRecord<ResxRowRecord>, IResxString
{
    public const               string  TABLE_NAME = "resx";
    private static readonly    SqlName __sql_Name = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __sql_Name;

    public JObject?                                   AdditionalData { get; set; }
    public string                                     Arabic         { get; init; } = EMPTY;
    public string                                     Chinese        { get; init; } = EMPTY;
    public string                                     Czech          { get; init; } = EMPTY;
    public string                                     Dutch          { get; init; } = EMPTY;
    public string                                     English        { get; init; } = EMPTY;
    public string                                     French         { get; init; } = EMPTY;
    public string                                     German         { get; init; } = EMPTY;
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
}
