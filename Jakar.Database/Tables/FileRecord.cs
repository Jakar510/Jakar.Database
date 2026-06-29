// Jakar.Extensions :: Jakar.Database
// 4/2/2024  17:43


namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed partial record FileRecord : PairRecord<FileRecord>, ITableRecord<FileRecord>, IFileData<Guid>, IFileMetaData
{
    public const string TABLE_NAME = "files";

    // ReSharper disable once ReplaceWithFieldKeyword
    private static readonly    SqlName __tableName = TABLE_NAME;
    public static ref readonly SqlName TableName => ref __tableName;

    [Fixed(1024)] public string?   FileDescription { get; set; }
    [Fixed(256)]  public string?   FileName        { get; set; }
    public               long      FileSize        { get; set; }
    [Fixed(256)] [StringCompare(StringComparison.InvariantCultureIgnoreCase)] public string? FileType { get; set; }
    [Unique]     [StringCompare(StringComparison.OrdinalIgnoreCase)]          public string? FullPath { get; init; }
    public               string    Hash            { get; set; } = EMPTY;
    public               MimeType? MimeType        { get; set; }
    public               string    Payload         { get; set; } = EMPTY;


    public FileRecord( in RecordID<FileRecord>       id,   in DateTimeOffset dateCreated, in DateTimeOffset? lastModified = null, JObject? additionalData = null ) : base(in id, in dateCreated, additionalData, in lastModified) { }
    public FileRecord( IFileData<Guid, FileMetaData> data, LocalFile?        file = null ) : this(data, data.MetaData, file) { }
    private FileRecord( IFileData<Guid> data, IFileMetaData metaData, LocalFile? file = null ) : base(RecordID<FileRecord>.New(), DateTimeOffset.UtcNow)
    {
        FileName        = metaData.FileName;
        FileDescription = metaData.FileDescription;
        FileType        = metaData.FileType;
        FileSize        = data.FileSize;
        Hash            = data.Hash;
        MimeType        = metaData.MimeType;
        Payload         = data.Payload;
        FullPath        = file?.FullPath;
    }
    internal FileRecord( DbDataReader reader ) : base(reader)
    {
        FileName        = reader.GetFieldValue<FileRecord, string?>(nameof(FileName));
        FileDescription = reader.GetFieldValue<FileRecord, string?>(nameof(FileDescription));
        FileType        = reader.GetFieldValue<FileRecord, string?>(nameof(FileType));
        FileSize        = reader.GetFieldValue<FileRecord, long>(nameof(FileSize));
        Hash            = reader.GetFieldValue<FileRecord, string>(nameof(Hash));
        MimeType        = reader.GetEnumValue<FileRecord, MimeType>(nameof(MimeType), Extensions.MimeType.NotSet);
        Payload         = reader.GetFieldValue<FileRecord, string>(nameof(Payload));
        FullPath        = reader.GetFieldValue<FileRecord, string?>(nameof(FullPath));
    }
    [Pure] public static FileRecord Create( DbDataReader                  reader )                       => new FileRecord(reader).Validate();
    public static        FileRecord Create( IFileData<Guid, FileMetaData> data, LocalFile? file = null ) => new(data, file);
    public static FileRecord Create<TFileMetaData>( IFileData<Guid, TFileMetaData> data, LocalFile? file = null )
        where TFileMetaData : class, IFileMetaData<TFileMetaData> => new(data, data.MetaData, file);
    public TFileData ToFileData<TFileData, TFileMetaData>()
        where TFileData : class, IFileData<TFileData, Guid, TFileMetaData>
        where TFileMetaData : class, IFileMetaData<TFileMetaData> => TFileData.Create(this, TFileMetaData.Create(this));


    [Pure] public async ValueTask<OneOf<byte[], string, FileData>> Read( CancellationToken token = default )
    {
        using TelemetrySpan telemetrySpan = TelemetrySpan.Create();
        if ( string.IsNullOrWhiteSpace(FullPath) ) { return new FileData(this, FileMetaData.Create(this)); }

        LocalFile file = FullPath;
        if ( MimeType != file.Mime ) { throw new InvalidOperationException($"{nameof(MimeType)} mismatch. Got {file.Mime} but expected {MimeType}"); }

        return file.Mime.IsText()
                   ? await file.ReadAsync().AsString(token)
                   : await file.ReadAsync().AsBytes(token);
    }


    [Pure] public async ValueTask<ErrorOrResult<FileData>> ToFileData( CancellationToken token = default )
    {
        OneOf<byte[], string, FileData> data = await Read(token);
        if ( data.IsT2 ) { return data.AsT2; }

        if ( data.IsT0 )
        {
            byte[] content = data.AsT0;
            string hash    = content.GetHash();
            if ( FileSize != content.Length ) { return Error.Conflict($"{nameof(FileSize)} mismatch. Got {content.Length} but expected {FileSize}"); }

            if ( !string.Equals(Hash, hash, StringComparison.Ordinal) ) { return Error.Conflict($"{nameof(Hash)} mismatch: {Hash} != {hash}"); }

            return new FileData(FileSize, Hash, Convert.ToBase64String(content), FileMetaData.Create(this));
        }
        else
        {
            string content = data.AsT1;
            string hash    = content.GetHash();
            if ( FileSize != content.Length ) { return Error.Conflict($"{nameof(FileSize)} mismatch. Got {content.Length} but expected {FileSize}"); }

            if ( !string.Equals(Hash, hash, StringComparison.Ordinal) ) { return Error.Conflict($"{nameof(Hash)} mismatch: {Hash} != {hash}"); }

            return new FileData(FileSize, Hash, content, FileMetaData.Create(this));
        }
    }


    public async ValueTask<FileRecord> Update( LocalFile file, CancellationToken token = default )
    {
        if ( FullPath != file.FullPath ) { throw new InvalidOperationException($"{nameof(FullPath)} mismatch. Got {file.FullPath} but expected {FullPath}"); }

        FileData data = await FileData.Create(file, token);
        return With(data, file);
    }
    public FileRecord With( IFileData<Guid, FileMetaData> data, LocalFile? file = null ) => With(data, data.MetaData, file);
    public FileRecord With( IFileData<Guid> data, IFileMetaData metaData, LocalFile? file = null )
    {
        if ( FullPath != file?.FullPath ) { throw new InvalidOperationException($"{nameof(FullPath)} mismatch. Got {file?.FullPath} but expected {FullPath}"); }

        FileName        = metaData.FileName;
        FileDescription = metaData.FileDescription;
        FileType        = metaData.FileType;
        FileSize        = data.FileSize;
        Hash            = data.Hash;
        MimeType        = metaData.MimeType;
        Payload         = data.Payload;
        return Modified();
    }


}
