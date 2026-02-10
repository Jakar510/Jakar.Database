// Jakar.Extensions :: Jakar.Database
// 08/14/2022  8:38 PM

using ZLinq.Linq;



namespace Jakar.Database;


public interface IDateCreated
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



public interface ITableRecord<TSelf>
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    public abstract static ValueEnumerable<FromArray<PropertyInfo>, PropertyInfo> ClassProperties  { [Pure] get; }
    public abstract static int                                                    PropertyCount    { get; }
    public abstract static TableMetaData<TSelf>                                   PropertyMetaData { [Pure] get; }
    public abstract static string                                                 TableName        { [Pure] get; }

    [Pure] public abstract static TSelf           Create( NpgsqlDataReader reader );
    [Pure] public abstract static MigrationRecord CreateTable( ulong       migrationID );
}



[Serializable]
public abstract record TableRecord<TSelf>( in DateTimeOffset DateCreated ) : IJsonModel<TSelf>, IDateCreated
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    protected internal static readonly PropertyInfo[]  Properties = typeof(TSelf).GetProperties(BindingFlags.Instance | BindingFlags.Public);
    protected                          DateTimeOffset? _lastModified;


    public static TableMetaData<TSelf>                                   PropertyMetaData => TableMetaData<TSelf>.Instance;
    public static ValueEnumerable<FromArray<PropertyInfo>, PropertyInfo> ClassProperties  { [Pure] get => Properties.AsValueEnumerable(); }
    public static int                                                    PropertyCount    => Properties.Length;


    protected internal TableRecord( NpgsqlDataReader reader ) : this(reader.GetFieldValue<TSelf, DateTimeOffset>(nameof(DateCreated))) { }


    [Pure] public UInt128 GetHash()
    {
        ReadOnlySpan<char> json = ToString();
        return json.Hash128();
    }
    public TSelf Modified()
    {
        _lastModified = DateTimeOffset.UtcNow;
        return (TSelf)this;
    }


    public abstract bool Equals( TSelf?    other );
    public abstract int  CompareTo( TSelf? other );
    public int CompareTo( object? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        return other is TSelf t
                   ? CompareTo(t)
                   : throw new ExpectedValueTypeException(nameof(other), other, typeof(TSelf));
    }


    public static bool TryFromJson( string? json, [NotNullWhen(true)] out TSelf? result )
    {
        try
        {
            if ( string.IsNullOrWhiteSpace(json) )
            {
                result = null;
                return false;
            }

            result = FromJson(json);
            return true;
        }
        catch ( Exception e ) { SelfLogger.WriteLine("{Exception}", e.ToString()); }

        result = null;
        return false;
    }
    public static TSelf FromJson( string json ) => json.FromJson<TSelf>();


    public virtual  ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public virtual  ValueTask Import( NpgsqlBatchCommand   batch,    CancellationToken token ) => default;
    public abstract ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token );
    [Pure] public virtual PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(DateCreated), DateCreated);
        return parameters;
    }
}



[Serializable]
public abstract record PairRecord<TSelf> : TableRecord<TSelf>, IUniqueID
    where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
{
    private   RecordID<TSelf> __id;
    protected JObject?        _additionalData;


    Guid IUniqueID<Guid>.                                           ID             => ID.Value;
    [ProtectedPersonalData]                  public JObject?        AdditionalData { get => _additionalData; set => _additionalData = value; }
    [Key]                                    public RecordID<TSelf> ID             { get => __id;            init => __id = value; }
    [ColumnMetaData(ColumnOptions.Nullable)] public DateTimeOffset? LastModified   { get => _lastModified;   init => _lastModified = value; }


    protected PairRecord( in RecordID<TSelf> id, in DateTimeOffset dateCreated, in DateTimeOffset? lastModified, JObject? additionalData = null ) : base(in dateCreated)
    {
        ID             = id;
        DateCreated    = dateCreated;
        LastModified   = lastModified;
        AdditionalData = additionalData;
    }
    protected internal PairRecord( NpgsqlDataReader reader ) : this(RecordID<TSelf>.ID(reader), reader.GetFieldValue<TSelf, DateTimeOffset>(nameof(DateCreated)), reader.GetFieldValue<TSelf, DateTimeOffset>(nameof(LastModified))) { }


    public TSelf NewID( RecordID<TSelf> id )
    {
        __id = id;
        return (TSelf)this;
    }
    [Pure] public RecordPair<TSelf> ToPair() => new(ID, DateCreated);


    public TSelf WithAdditionalData( IJsonModel value ) => WithAdditionalData(value.AdditionalData);
    public virtual TSelf WithAdditionalData( JObject? additionalData )
    {
        if ( additionalData is null || additionalData.Count <= 0 ) { return (TSelf)this; }

        JObject json = _additionalData ??= new JObject();
        foreach ( ( string key, JToken? jToken ) in additionalData ) { json[key] = jToken; }

        return Modified();
    }


    public static PostgresParameters GetDynamicParameters( TSelf record ) => GetDynamicParameters(in record.__id);
    public static PostgresParameters GetDynamicParameters( in RecordID<TSelf> id )
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(ID), id.Value);
        return parameters;
    }


    [Pure] public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(ID),           ID.Value);
        parameters.Add(nameof(LastModified), LastModified);
        return parameters;
    }



    [Serializable]
    public class RecordCollection( int capacity = DEFAULT_CAPACITY ) : RecordCollection<TSelf>(capacity)
    {
        public RecordCollection( params ReadOnlySpan<TSelf> values ) : this() => Add(values);
        public RecordCollection( IEnumerable<TSelf>         values ) : this() => Add(values);
    }
}



[Serializable]
public abstract record OwnedTableRecord<TSelf> : PairRecord<TSelf>, ICreatedBy
    where TSelf : OwnedTableRecord<TSelf>, ITableRecord<TSelf>
{
    [ColumnMetaData(UserRecord.TABLE_NAME)] public RecordID<UserRecord>? CreatedBy { get; set; }


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
