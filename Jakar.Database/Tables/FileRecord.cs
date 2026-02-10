// Jakar.Extensions :: Jakar.Database
// 4/2/2024  17:43


namespace Jakar.Database;


[Serializable]
[Table(TABLE_NAME)]
public sealed record FileRecord : PairRecord<FileRecord>, ITableRecord<FileRecord>, IFileData<Guid>, IFileMetaData
{
    public const string TABLE_NAME = "files";


    public static                                      string    TableName       => TABLE_NAME;
    [ColumnMetaData(ColumnOptions.Fixed, 256)]  public string?   FileName        { get; set; }
    [ColumnMetaData(ColumnOptions.Fixed, 1024)] public string?   FileDescription { get; set; }
    [ColumnMetaData(ColumnOptions.Fixed, 256)]  public string?   FileType        { get; set; }
    public                                             long      FileSize        { get; set; }
    public                                             string    Hash            { get; set; } = EMPTY;
    [ColumnMetaData(ColumnOptions.Indexed)] public     MimeType? MimeType        { get; set; }
    public                                             string    Payload         { get; set; } = EMPTY;
    [ColumnMetaData(ColumnOptions.Indexed)] public     string?   FullPath        { get; init; }


    public FileRecord( in RecordID<FileRecord>       id,   in DateTimeOffset dateCreated, in DateTimeOffset? lastModified = null, JObject? additionalData = null ) : base(in id, in dateCreated, in lastModified, additionalData) { }
    public FileRecord( IFileData<Guid, FileMetaData> data, LocalFile?        file = null ) : this(data, data.MetaData, file) { }
    private FileRecord( IFileData<Guid> data, IFileMetaData metaData, LocalFile? file = null ) : base(RecordID<FileRecord>.New(), DateTimeOffset.UtcNow, null)
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
    internal FileRecord( NpgsqlDataReader reader ) : base(reader)
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
    [Pure] public static FileRecord      Create( NpgsqlDataReader              reader )                       => new FileRecord(reader).Validate();
    public static        MigrationRecord CreateTable( ulong                    migrationID )                  => MigrationRecord.CreateTable<FileRecord>(migrationID);
    public static        FileRecord      Create( IFileData<Guid, FileMetaData> data, LocalFile? file = null ) => new(data, file);
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
                   ? await file.ReadAsync()
                               .AsString(token)
                   : await file.ReadAsync()
                               .AsBytes(token);
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


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        using PooledArray<ColumnMetaData> buffer = PropertyMetaData.SortedColumns;

        foreach ( ColumnMetaData column in buffer.Array )
        {
            switch ( column.PropertyName )
            {
                case nameof(ID):
                    await importer.WriteAsync(ID.Value, column.PostgresDbType, token);
                    break;

                case nameof(DateCreated):
                    await importer.WriteAsync(DateCreated, column.PostgresDbType, token);
                    break;

                case nameof(LastModified):
                    if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, column.PostgresDbType, token); }
                    else { await importer.WriteNullAsync(token); }

                    break;

                case nameof(FileDescription):
                    await importer.WriteAsync(FileDescription, column.PostgresDbType, token);
                    break;

                case nameof(FileName):
                    await importer.WriteAsync(FileName, column.PostgresDbType, token);
                    break;

                case nameof(FileType):
                    if ( string.IsNullOrWhiteSpace(FileType) ) { await importer.WriteAsync(FileType, column.PostgresDbType, token); }
                    else { await importer.WriteNullAsync(token); }

                    break;

                case nameof(FileSize):
                    await importer.WriteAsync(FileSize, column.PostgresDbType, token);
                    break;

                case nameof(Hash):
                    await importer.WriteAsync(Hash, column.PostgresDbType, token);
                    break;

                case nameof(MimeType):
                    await importer.WriteAsync(MimeType, column.PostgresDbType, token);
                    break;

                case nameof(Payload):
                    await importer.WriteAsync(Payload, column.PostgresDbType, token);
                    break;

                case nameof(FullPath):
                    if ( string.IsNullOrWhiteSpace(FullPath) ) { await importer.WriteAsync(FullPath, column.PostgresDbType, token); }
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
        parameters.Add(nameof(FileName),        FileName);
        parameters.Add(nameof(FileDescription), FileDescription);
        parameters.Add(nameof(FileType),        FileType);
        parameters.Add(nameof(FileSize),        FileSize);
        parameters.Add(nameof(Hash),            Hash);
        parameters.Add(nameof(MimeType),        MimeType);
        parameters.Add(nameof(Payload),         Payload);
        parameters.Add(nameof(FullPath),        FullPath);
        parameters.Add(nameof(ID),              ID);
        parameters.Add(nameof(DateCreated),     DateCreated);
        parameters.Add(nameof(LastModified),    LastModified);
        return parameters;
    }


    public override int CompareTo( FileRecord? other )
    {
        if ( ReferenceEquals(this, other) ) { return 0; }

        if ( other is null ) { return 1; }

        int fileTypeComparison = string.Compare(FileType, other.FileType, StringComparison.Ordinal);
        if ( fileTypeComparison != 0 ) { return fileTypeComparison; }

        int fileNameComparison = string.Compare(FileName, other.FileName, StringComparison.Ordinal);
        if ( fileNameComparison != 0 ) { return fileNameComparison; }

        int fileDescriptionComparison = string.Compare(FileDescription, other.FileDescription, StringComparison.Ordinal);
        if ( fileDescriptionComparison != 0 ) { return fileDescriptionComparison; }

        int fileSizeComparison = FileSize.CompareTo(other.FileSize);
        if ( fileSizeComparison != 0 ) { return fileSizeComparison; }

        int hashComparison = string.Compare(Hash, other.Hash, StringComparison.Ordinal);
        if ( hashComparison != 0 ) { return hashComparison; }

        int mimeTypeComparison = Nullable.Compare(MimeType, other.MimeType);
        if ( mimeTypeComparison != 0 ) { return mimeTypeComparison; }

        int payloadComparison = string.Compare(Payload, other.Payload, StringComparison.Ordinal);
        if ( payloadComparison != 0 ) { return payloadComparison; }

        return string.Compare(FullPath, other.FullPath, StringComparison.Ordinal);
    }
    public override bool Equals( FileRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return base.Equals(other) && Nullable.Equals(MimeType, other.MimeType) && string.Equals(FileType, other.FileType, StringComparison.InvariantCultureIgnoreCase) && string.Equals(Hash, other.Hash, StringComparison.Ordinal) && string.Equals(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
    }
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(base.GetHashCode());
        hashCode.Add(AdditionalData);
        hashCode.Add(FileName);
        hashCode.Add(FileDescription);
        hashCode.Add(FileType);
        hashCode.Add(FileSize);
        hashCode.Add(Hash);
        hashCode.Add(MimeType);
        hashCode.Add(Payload);
        hashCode.Add(FullPath);
        return hashCode.ToHashCode();
    }


    public static bool operator >( FileRecord  left, FileRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( FileRecord left, FileRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( FileRecord  left, FileRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( FileRecord left, FileRecord right ) => left.CompareTo(right) <= 0;
}
