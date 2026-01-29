// Jakar.Extensions :: Jakar.Database
// 03/11/2023  11:20 PM

namespace Jakar.Database;


[Serializable]
[method: JsonConstructor]
public readonly struct RecordPair<TSelf>( RecordID<TSelf> id, DateTimeOffset dateCreated ) : IEqualComparable<RecordPair<TSelf>>, IRecordPair<TSelf>
    where TSelf : ITableRecord<TSelf>
{
    public readonly  RecordID<TSelf> ID          = id;
    public readonly  DateTimeOffset  DateCreated = dateCreated;
    private readonly int             __hash      = HashCode.Combine(id, dateCreated);


    public static ReadOnlyMemory<PropertyInfo> ClassProperties  { get; } = typeof(RecordPair<TSelf>).GetProperties();
    public static TableMetaData                PropertyMetaData => TSelf.PropertyMetaData;
    public static string                       TableName        => TSelf.TableName;
    Guid IUniqueID<Guid>.                      ID               => ID.Value;
    RecordID<TSelf> IRecordPair<TSelf>.        ID               => ID;
    DateTimeOffset IDateCreated.               DateCreated      => DateCreated;


    public UInt128 GetHash() => ID.GetHash() | new UInt128(0, (ulong)DateCreated.GetHashCode());


    public int CompareTo( object? other ) => other is RecordPair<TSelf> pair
                                                 ? CompareTo(pair)
                                                 : throw new ExpectedValueTypeException(other, typeof(RecordPair<TSelf>));
    public          int  CompareTo( RecordPair<TSelf> other ) => DateCreated.CompareTo(other.DateCreated);
    public          bool Equals( RecordPair<TSelf>    other ) => ID.Equals(other.ID)             && DateCreated.Equals(other.DateCreated);
    public override bool Equals( object?              other ) => other is RecordPair<TSelf> pair && Equals(pair);
    public override int  GetHashCode()                        => __hash;


    public static implicit operator RecordPair<TSelf>( (RecordID<TSelf> id, DateTimeOffset dateCreated) value ) => new(value.id, value.dateCreated);
    public static implicit operator (RecordID<TSelf> id, DateTimeOffset dateCreated)( RecordPair<TSelf> value ) => ( value.ID, value.DateCreated );
    public static implicit operator KeyValuePair<RecordID<TSelf>, DateTimeOffset>( RecordPair<TSelf>    value ) => new(value.ID, value.DateCreated);
    public static implicit operator KeyValuePair<Guid, DateTimeOffset>( RecordPair<TSelf>               value ) => new(value.ID.Value, value.DateCreated);


    public static MigrationRecord CreateTable( ulong migrationID ) => throw new NotImplementedException("Not Implemented by design");
    [Pure] public static RecordPair<TSelf> Create( NpgsqlDataReader reader )
    {
        DateTimeOffset  dateCreated = reader.GetFieldValue<DateTimeOffset>(nameof(DateCreated));
        RecordID<TSelf> id          = RecordID<TSelf>.ID(reader);
        return new RecordPair<TSelf>(id, dateCreated);
    }
    [Pure] public static async IAsyncEnumerable<RecordPair<TSelf>> CreateAsync( NpgsqlDataReader reader, [EnumeratorCancellation] CancellationToken token = default )
    {
        while ( await reader.ReadAsync(token) ) { yield return Create(reader); }
    }


    public ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public ValueTask Import( NpgsqlBatchCommand   batch,    CancellationToken token ) => default;
    public async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        await importer.WriteAsync(ID.Value,    NpgsqlDbType.Uuid,        token);
        await importer.WriteAsync(DateCreated, NpgsqlDbType.TimestampTz, token);
    }

    [Pure] public PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(ID),          ID);
        parameters.Add(nameof(DateCreated), DateCreated);
        return parameters;
    }


    public static bool operator ==( RecordPair<TSelf>? left, RecordPair<TSelf>? right ) => Nullable.Equals(left, right);
    public static bool operator !=( RecordPair<TSelf>? left, RecordPair<TSelf>? right ) => !Nullable.Equals(left, right);
    public static bool operator ==( RecordPair<TSelf>  left, RecordPair<TSelf>  right ) => EqualityComparer<RecordPair<TSelf>>.Default.Equals(left, right);
    public static bool operator !=( RecordPair<TSelf>  left, RecordPair<TSelf>  right ) => !EqualityComparer<RecordPair<TSelf>>.Default.Equals(left, right);
    public static bool operator >( RecordPair<TSelf>   left, RecordPair<TSelf>  right ) => Comparer<RecordPair<TSelf>>.Default.Compare(left, right) > 0;
    public static bool operator >=( RecordPair<TSelf>  left, RecordPair<TSelf>  right ) => Comparer<RecordPair<TSelf>>.Default.Compare(left, right) >= 0;
    public static bool operator <( RecordPair<TSelf>   left, RecordPair<TSelf>  right ) => Comparer<RecordPair<TSelf>>.Default.Compare(left, right) < 0;
    public static bool operator <=( RecordPair<TSelf>  left, RecordPair<TSelf>  right ) => Comparer<RecordPair<TSelf>>.Default.Compare(left, right) <= 0;
}
