// Jakar.Extensions :: Jakar.Database
// 08/14/2022  8:38 PM

namespace Jakar.Database;


public interface IDateCreated : IUniqueID
{
    public DateTimeOffset DateCreated { get; }
}



public interface ILastModified : IDateCreated
{
    public DateTimeOffset? LastModified { get; }
}



public interface ICreatedBy : IDateCreated
{
    public RecordID<UserRecord>? CreatedBy { get; }
}



public interface IRecordPair<TSelf> : IDateCreated
    where TSelf : class, IRecordPair<TSelf>, ITableRecord<TSelf>
{
    Guid IUniqueID<Guid>.      ID => ID.Value;
    public new RecordID<TSelf> ID { get; }


    public        ValueTask          Import( NpgsqlBatchCommand   importer, CancellationToken token );
    public        ValueTask          Import( NpgsqlBinaryImporter importer, CancellationToken token );
    public        ValueTask          Export( NpgsqlBinaryExporter exporter, CancellationToken token );
    [Pure] public UInt128            GetHash();
    [Pure] public PostgresParameters ToDynamicParameters();
}



public interface ITableRecord<TSelf> : IRecordPair<TSelf>, IJsonModel<TSelf>
    where TSelf : class, ITableRecord<TSelf>
{
    public abstract static ReadOnlyMemory<PropertyInfo> ClassProperties  { [Pure] get; }
    public abstract static TableMetaData<TSelf>         PropertyMetaData { [Pure] get; }
    public abstract static string                       TableName        { [Pure] get; }


    [Pure] public abstract static MigrationRecord CreateTable( ulong migrationID );


    [Pure] public                 RecordPair<TSelf> ToPair();
    public                        TSelf             Modified();
    [Pure] public abstract static TSelf             Create( NpgsqlDataReader reader );
    public                        TSelf             NewID( RecordID<TSelf>   id );
}



[Serializable]
public abstract record TableRecord<TSelf> : BaseRecord<TSelf>, IRecordPair<TSelf>, ILastModified
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    protected internal static readonly PropertyInfo[]  Properties = typeof(TSelf).GetProperties(BindingFlags.Instance | BindingFlags.Public);
    protected                          DateTimeOffset? _lastModified;
    private                            RecordID<TSelf> __id;


    public static                                   TableMetaData<TSelf>         PropertyMetaData => TableMetaData<TSelf>.Instance;
    public static                                   ReadOnlyMemory<PropertyInfo> ClassProperties  { [Pure] get => Properties; }
    public                                          DateTimeOffset               DateCreated      { get;                  init; }
    [Key]                                    public RecordID<TSelf>              ID               { get => __id;          init => __id = value; }
    [ColumnMetaData(ColumnOptions.Nullable)] public DateTimeOffset?              LastModified     { get => _lastModified; init => _lastModified = value; }


    protected TableRecord( in RecordID<TSelf> id, in DateTimeOffset dateCreated, in DateTimeOffset? lastModified, JObject? additionalData = null )
    {
        DateCreated    = dateCreated;
        _lastModified  = lastModified;
        __id           = id;
        AdditionalData = additionalData;
    }
    protected internal TableRecord( NpgsqlDataReader reader ) : this(RecordID<TSelf>.ID(reader), reader.GetFieldValue<TSelf, DateTimeOffset>(nameof(DateCreated)), reader.GetFieldValue<TSelf, DateTimeOffset>(nameof(LastModified))) { }


    [Pure] public UInt128 GetHash()
    {
        ReadOnlySpan<char> json = this.ToJson();
        return json.Hash128();
    }
    public TSelf Modified()
    {
        _lastModified = DateTimeOffset.UtcNow;
        return (TSelf)this;
    }
    public TSelf NewID( RecordID<TSelf> id )
    {
        __id = id;
        return (TSelf)this;
    }
    [Pure] public RecordPair<TSelf> ToPair() => new(ID, DateCreated);


    public override TSelf WithAdditionalData( JObject? value )
    {
        if ( value is null || value.Count <= 0 ) { return (TSelf)this; }

        base.WithAdditionalData(value);
        return Modified();
    }


    public static PostgresParameters GetDynamicParameters( TSelf record ) => GetDynamicParameters(in record.__id);
    public static PostgresParameters GetDynamicParameters( in RecordID<TSelf> id )
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(ID), id.Value);
        return parameters;
    }


    public abstract ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token );
    public virtual  ValueTask Import( NpgsqlBatchCommand   batch,    CancellationToken token ) => default;
    public abstract ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token );
    [Pure] public virtual PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(ID),           ID.Value);
        parameters.Add(nameof(DateCreated),  DateCreated);
        parameters.Add(nameof(LastModified), LastModified);
        return parameters;
    }


    [Pure] protected static TValue TryGet<TValue>( NpgsqlDataReader reader, string key )
    {
        int index = reader.GetOrdinal(key);
        return (TValue)reader.GetValue(index);
    }


    public void Deconstruct( out RecordID<TSelf> id, out DateTimeOffset dateCreated, out DateTimeOffset? lastModified, out JObject? additionalData )
    {
        additionalData = AdditionalData;
        id             = ID;
        dateCreated    = DateCreated;
        lastModified   = LastModified;
    }



    [Serializable]
    public class RecordCollection( int capacity = DEFAULT_CAPACITY ) : RecordCollection<TSelf>(capacity)
    {
        public RecordCollection( params ReadOnlySpan<TSelf> values ) : this() => Add(values);
        public RecordCollection( IEnumerable<TSelf>         values ) : this() => Add(values);
    }
}



[Serializable]
public abstract record OwnedTableRecord<TSelf> : TableRecord<TSelf>, ICreatedBy
    where TSelf : OwnedTableRecord<TSelf>, ITableRecord<TSelf>
{
    [ColumnMetaData(ColumnOptions.ForeignKey | ColumnOptions.Nullable, UserRecord.TABLE_NAME)] public RecordID<UserRecord>? CreatedBy { get; set; }


    protected OwnedTableRecord( in RecordID<UserRecord>?  createdBy, in RecordID<TSelf> id, in DateTimeOffset dateCreated, in DateTimeOffset? lastModified, JObject? additionalData = null ) : base(in id, in dateCreated, in lastModified, additionalData) => CreatedBy = createdBy;
    protected internal OwnedTableRecord( NpgsqlDataReader reader ) : base(reader) => CreatedBy = RecordID<UserRecord>.CreatedBy(reader);


    public static PostgresParameters GetDynamicParameters( UserRecord user )
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(CreatedBy), user.ID.Value);
        return parameters;
    }
    protected static PostgresParameters GetDynamicParameters( OwnedTableRecord<TSelf> record )
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(CreatedBy), record.CreatedBy);
        return parameters;
    }


    public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(CreatedBy), CreatedBy);
        return parameters;
    }


    public async ValueTask<UserRecord?> GetUser( NpgsqlConnection           connection, NpgsqlTransaction? transaction, Database db, CancellationToken token ) => await db.Users.Get(connection, transaction, true,      GetDynamicParameters(this), token);
    public async ValueTask<UserRecord?> GetUserWhoCreated( NpgsqlConnection connection, NpgsqlTransaction? transaction, Database db, CancellationToken token ) => await db.Users.Get(connection, transaction, CreatedBy, token);


    public TSelf WithOwner( UserRecord  user )   => (TSelf)( this with { CreatedBy = user.ID } );
    public bool  Owns( UserRecord       record ) => CreatedBy == record.ID;
    public bool  DoesNotOwn( UserRecord record ) => CreatedBy != record.ID;


    public override int CompareTo( TSelf? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        return Nullable.Compare(CreatedBy, other.CreatedBy);
    }
    public void Deconstruct( out RecordID<UserRecord>? createdBy, out RecordID<TSelf> id, out DateTimeOffset dateCreated, out DateTimeOffset? lastModified, out JObject? additionalData )
    {
        createdBy      = CreatedBy;
        id             = ID;
        dateCreated    = DateCreated;
        lastModified   = LastModified;
        additionalData = AdditionalData;
    }
}
