// Jakar.Extensions :: Jakar.Database
// 08/14/2022  8:38 PM

namespace Jakar.Database;


public interface IDateCreated
{
    public DateTimeOffset DateCreated { get; }
}



public interface ILastModified : IDateCreated
{
    public DateTimeOffset? LastModified { get; }
}



public interface IRecordID : IUniqueID, ISpanFormattable;



public interface ITableRecord<TSelf>
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    public abstract static ref readonly ImmutableArray<PropertyInfo> ClassProperties { [Pure] get; }
    public abstract static              TableMetaData<TSelf>         MetaData        { [Pure] get; }
    public abstract static              int                          PropertyCount   { get; }
    public abstract static              string                       TableName       { [Pure] get; }


    // [Pure] public abstract static TSelf Create( SqlDataReader    reader );
    [Pure] public abstract static TSelf Create( DbDataReader reader );
}



[Serializable]
public abstract record TableRecord<TSelf>( in DateTimeOffset DateCreated ) : IJsonModel<TSelf>, IDateCreated
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    protected internal static readonly ImmutableArray<PropertyInfo> Properties = typeof(TSelf).GetProperties(ITableMetaData.ATTRIBUTES).AsValueEnumerable().Where(static x => !x.HasAttribute<DbIgnoreAttribute>()).ToImmutableArray();

    protected                  DateTimeOffset?              _lastModified;
    public static ref readonly ImmutableArray<PropertyInfo> ClassProperties { [Pure] get => ref Properties; }


    public static TableMetaData<TSelf> MetaData      => TableMetaData<TSelf>.Instance;
    public static int                  PropertyCount => Properties.Length;


    protected internal TableRecord( DbDataReader reader ) : this(reader.DateCreated<TSelf>()) { }


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


    // IDataReader
    public virtual ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public virtual ValueTask Import( NpgsqlBatchCommand   batch,    CancellationToken token ) => default;
    public async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        await importer.StartRowAsync(token);
        using ArrayBuffer<ColumnMetaData> buffer = MetaData.SortedColumns;

        foreach ( ColumnMetaData column in buffer.Array ) { await Import(importer, column.PropertyName, column.PostgresDbType, token); }
    }
    protected abstract ValueTask Import( NpgsqlBinaryImporter importer, string propertyName, NpgsqlDbType postgresDbType, CancellationToken token );
    public virtual ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(DateCreated)].DataColumn] = DateCreated;

        return token.IsCancellationRequested
                   ? ValueTask.FromCanceled(token)
                   : ValueTask.CompletedTask;
    }
    [Pure] public virtual CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = CommandParameters.Create<TSelf>();
        parameters.Add(nameof(DateCreated), DateCreated);
        return parameters;
    }
}



[Serializable]
public abstract record LastModifiedRecord<TSelf> : TableRecord<TSelf>, ILastModified
    where TSelf : LastModifiedRecord<TSelf>, ITableRecord<TSelf>
{
    public DateTimeOffset? LastModified { get => _lastModified; init => _lastModified = value; }


    protected LastModifiedRecord( in DateTimeOffset dateCreated, in DateTimeOffset? lastModified ) : base(in dateCreated)
    {
        DateCreated  = dateCreated;
        LastModified = lastModified;
    }
    protected internal LastModifiedRecord( DbDataReader reader ) : base(reader) => LastModified = reader.LastModified<TSelf>();

    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(LastModified)].DataColumn] = LastModified;
        return base.Import(row, token);
    }
    [Pure] public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(LastModified), LastModified);
        return parameters;
    }
}



[Serializable]
public abstract record PairRecord<TSelf> : LastModifiedRecord<TSelf>, IUniqueID
    where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
{
    protected                      JObject?        _additionalData;
    private                        RecordID<TSelf> __id;
    [ProtectedPersonalData] public JObject?        AdditionalData { get => _additionalData; set => _additionalData = value; }


    Guid IUniqueID<Guid>.        ID => ID.Value;
    [Key] public RecordID<TSelf> ID { get => __id; init => __id = value; }


    protected PairRecord( in RecordID<TSelf> id, in DateTimeOffset dateCreated, JObject? additionalData = null, in DateTimeOffset? lastModified = null ) : base(in dateCreated, in lastModified)
    {
        ID             = id;
        AdditionalData = additionalData;
    }
    protected internal PairRecord( DbDataReader reader ) : base(reader)
    {
        ID             = RecordID<TSelf>.ID(reader);
        AdditionalData = reader.GetAdditionalData<TSelf>();
    }


    public static implicit operator RecordID<TSelf>( PairRecord<TSelf>? record ) => record?.ID ?? RecordID<TSelf>.Empty;


    public TSelf With( RecordID<TSelf> id )
    {
        __id = id;
        return (TSelf)this;
    }
    [Pure] public   RecordPair<TSelf> ToPair()      => new(ID, DateCreated);
    public override int               GetHashCode() => ID.GetHashCode();


    public TSelf WithAdditionalData( IJsonModel value ) => WithAdditionalData(value.AdditionalData);
    public virtual TSelf WithAdditionalData( JObject? additionalData )
    {
        if ( additionalData is null || additionalData.Count <= 0 ) { return (TSelf)this; }

        JObject json = _additionalData ??= new JObject();
        foreach ( ( string key, JToken? jToken ) in additionalData ) { json[key] = jToken; }

        return Modified();
    }


    public static CommandParameters GetDynamicParameters( TSelf record ) => GetDynamicParameters(in record.__id);
    public static CommandParameters GetDynamicParameters( in RecordID<TSelf> id )
    {
        CommandParameters parameters = CommandParameters.Create<TSelf>();
        parameters.Add(nameof(ID), id.Value);
        return parameters;
    }


    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(ID)].DataColumn] = ID;
        return base.Import(row, token);
    }
    [Pure] public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(ID),             ID.Value);
        parameters.Add(nameof(AdditionalData), AdditionalData);
        return parameters;
    }



    [Serializable]
    public class RecordCollection( int capacity = DEFAULT_CAPACITY ) : RecordCollection<TSelf>(capacity)
    {
        public RecordCollection( params ReadOnlySpan<TSelf> values ) : this() => Add(values);
        public RecordCollection( IEnumerable<TSelf>         values ) : this() => Add(values);
    }
}



public interface IUserRecordID : IDateCreated, IUniqueID, IUserID
{
    public new RecordID<UserRecord> UserID { get; }
}



[Serializable]
public abstract record OwnedTableRecord<TSelf> : PairRecord<TSelf>, IUserRecordID
    where TSelf : OwnedTableRecord<TSelf>, ITableRecord<TSelf>
{
    public abstract RecordID<UserRecord> UserID { get; init; }
    Guid IUserID.                        UserID => UserID.Value;


    protected OwnedTableRecord( in RecordID<UserRecord> userID, in RecordID<TSelf> id, in DateTimeOffset dateCreated, in DateTimeOffset? lastModified, JObject? additionalData = null ) : base(in id, in dateCreated, additionalData, in lastModified) => UserID = userID;
    protected internal OwnedTableRecord( DbDataReader   reader ) : base(reader) => UserID = RecordID<UserRecord>.UserID(reader);


    public static implicit operator RecordID<UserRecord>( OwnedTableRecord<TSelf>? record ) => record?.UserID ?? RecordID<UserRecord>.Empty;


    public static CommandParameters GetDynamicParameters( UserRecord user )
    {
        CommandParameters parameters = CommandParameters.Create<TSelf>();
        parameters.Add(nameof(UserID), user.ID.Value);
        return parameters;
    }
    protected static CommandParameters GetDynamicParameters( OwnedTableRecord<TSelf> record )
    {
        CommandParameters parameters = CommandParameters.Create<TSelf>();
        parameters.Add(nameof(UserID), record.UserID);
        return parameters;
    }


    public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(UserID), UserID);
        return parameters;
    }


    public async ValueTask<UserRecord?> GetUser( DbConnectionContext           context, Database db, CancellationToken token ) => await db.Users.Get(context, GetDynamicParameters(this), token);
    public async ValueTask<UserRecord?> GetUserWhoCreated( DbConnectionContext context, Database db, CancellationToken token ) => await db.Users.Get(context, UserID,                     token);


    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(UserID)].DataColumn] = UserID;
        return base.Import(row, token);
    }
    public TSelf WithOwner( UserRecord  user )   => (TSelf)( this with { UserID = user.ID } );
    public bool  Owns( UserRecord       record ) => UserID == record.ID;
    public bool  DoesNotOwn( UserRecord record ) => UserID != record.ID;


    public override int CompareTo( TSelf? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        return UserID.CompareTo(other.UserID);
    }
}
