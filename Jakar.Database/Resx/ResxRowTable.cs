namespace Jakar.Database;


/// <see cref="ResxString"/>
[Serializable]
[Table(TABLE_NAME)]
[JsonModel]
public sealed partial record ResxRowRecord : TableRecord<ResxRowRecord>, ITableRecord<ResxRowRecord>, IResxString
{
    public const               string  TABLE_NAME = "resx";
    private static readonly    SqlName __sql_Name = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __sql_Name;

    [JsonOverflow] public JObjectNode?                AdditionalData { get; set; }
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


    [JsonCreator] public ResxRowRecord( in DateTimeOffset dateCreated ) : base(in dateCreated) { }
    [SetsRequiredMembers] public ResxRowRecord( string key, string neutral = EMPTY ) : this(DateTimeOffset.UtcNow)
    {
        Key     = key;
        Neutral = neutral;
    }


    [Pure] public static async IAsyncEnumerable<ResxRowRecord> CreateAsync( DbDataReader reader, [EnumeratorCancellation] CancellationToken token = default )
    {
        while ( await reader.ReadAsync(token) ) { yield return Create(reader); }
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
