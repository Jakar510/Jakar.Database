namespace Jakar.Database;


public interface ICreateMapping<TSelf, TKey, TValue> : ITableRecord<TSelf>
    where TValue : class, ITableRecord<TValue>
    where TKey : class, ITableRecord<TKey>
    where TSelf : class, ITableRecord<TSelf>, ICreateMapping<TSelf, TKey, TValue>
{
    [Pure]                                  public abstract static TSelf                 Create( TKey           key, TValue                                value );
    [Pure]                                  public abstract static TSelf                 Create( RecordID<TKey> key, RecordID<TValue>                      value );
    [Pure]                                  public abstract static ImmutableArray<TSelf> Create( TKey           key, params ReadOnlySpan<TValue>           values );
    [Pure]                                  public abstract static ImmutableArray<TSelf> Create( RecordID<TKey> key, params ReadOnlySpan<RecordID<TValue>> values );
    [Pure] [OverloadResolutionPriority(-1)] public abstract static IEnumerable<TSelf>    Create( RecordID<TKey> key, IEnumerable<RecordID<TValue>>         values );
}



[Serializable]
public abstract record Mapping<TSelf, TKey, TValue> : TableRecord<TSelf>
    where TValue : class, ITableRecord<TValue>
    where TKey : class, ITableRecord<TKey>
    where TSelf : Mapping<TSelf, TKey, TValue>, ICreateMapping<TSelf, TKey, TValue>, ITableRecord<TSelf>
{
    protected static readonly string _key_id = nameof(KeyID)
       .SqlColumnName();
    protected static readonly string _value_id = nameof(ValueID)
       .SqlColumnName();
    private WeakReference<TKey>?   __owner;
    private WeakReference<TValue>? __value;


    public abstract RecordID<TKey>   KeyID   { get; init; }
    public abstract RecordID<TValue> ValueID { get; init; }


    protected Mapping( RecordID<TKey> key, RecordID<TValue> value ) : this(key, value, RecordID<TSelf>.New(), DateTimeOffset.UtcNow) { }
    protected Mapping( RecordID<TKey> keyID, RecordID<TValue> valueID, RecordID<TSelf> ID, DateTimeOffset DateCreated, DateTimeOffset? LastModified = null ) : base(in ID, in DateCreated, in LastModified)
    {
        KeyID   = keyID;
        ValueID = valueID;
    }
    protected Mapping( TKey key, TValue value ) : this(key.ID, value.ID)
    {
        __owner = new WeakReference<TKey>(key);
        __value = new WeakReference<TValue>(value);
    }
    protected internal Mapping( NpgsqlDataReader reader ) : base(reader)
    {
        KeyID   = RecordID<TKey>.Create(reader, nameof(KeyID));
        ValueID = RecordID<TValue>.Create(reader, nameof(ValueID));
    }


    public static PostgresParameters GetDynamicParameters( TValue record ) => GetDynamicParameters(record.ID);
    public static PostgresParameters GetDynamicParameters( RecordID<TValue> value )
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(ValueID), value);
        return parameters;
    }
    public static PostgresParameters GetDynamicParameters( TKey key ) => GetDynamicParameters(key.ID);
    public static PostgresParameters GetDynamicParameters( RecordID<TKey> key )
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(KeyID), key);
        return parameters;
    }
    public static PostgresParameters GetDynamicParameters( TKey key, TValue value ) => GetDynamicParameters(key.ID, value.ID);
    public static PostgresParameters GetDynamicParameters( RecordID<TKey> key, RecordID<TValue> value )
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(KeyID),   key);
        parameters.Add(nameof(ValueID), value);
        return parameters;
    }


    public static MigrationRecord CreateTable( ulong migrationID ) => MigrationRecord.CreateTable<TSelf>(migrationID);


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

                case nameof(KeyID):
                    await importer.WriteAsync(KeyID.Value, column.PostgresDbType, token);
                    break;

                case nameof(ValueID):
                    await importer.WriteAsync(ValueID.Value, column.PostgresDbType, token);
                    break;

                case nameof(LastModified):
                    if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, column.PostgresDbType, token); }
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
        parameters.Add(nameof(KeyID),   KeyID);
        parameters.Add(nameof(ValueID), ValueID);
        return parameters;
    }


    public override bool Equals( TSelf? other ) => ReferenceEquals(this, other) || ( other is not null && KeyID == other.KeyID && ValueID == other.ValueID );
    public override int CompareTo( TSelf? other )
    {
        if ( other is null ) { return -1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        int ownerComparision = KeyID.CompareTo(other.KeyID);
        if ( ownerComparision == 0 ) { return ownerComparision; }

        return ValueID.CompareTo(other.ValueID);
    }
    public override int GetHashCode() => HashCode.Combine(KeyID, ValueID);


    public async ValueTask<TKey?> Get( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TKey> selfTable, CancellationToken token )
    {
        if ( __owner is not null && __owner.TryGetTarget(out TKey? value) ) { return value; }

        TKey? record = await selfTable.Get(connection, transaction, KeyID, token)
                                      .ConfigureAwait(false);

        if ( record is not null ) { __owner = new WeakReference<TKey>(record); }

        return record;
    }
    public async ValueTask<TValue?> Get( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TValue> selfTable, CancellationToken token )
    {
        if ( __value is not null && __value.TryGetTarget(out TValue? value) ) { return value; }

        TValue? record = await selfTable.Get(connection, transaction, ValueID, token)
                                        .ConfigureAwait(false);

        if ( record is not null ) { __value = new WeakReference<TValue>(record); }

        return record;
    }

    public static async ValueTask<bool> TryAdd( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TSelf> selfTable, RecordID<TKey> key, RecordID<TValue> value, CancellationToken token )
    {
        if ( await Exists(connection, transaction, selfTable, key, value, token) ) { return false; }

        TSelf record = TSelf.Create(key, value);

        TSelf self = await selfTable.Insert(connection, transaction, record, token)
                                    .ConfigureAwait(false);

        return self.IsValidID();
    }
    public static async ValueTask TryAdd( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TSelf> selfTable, DbTable<TValue> valueTable, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        PostgresParameters parameters = GetDynamicParameters(key);
        StringBuilder      ids        = new();
        ids.AppendJoin(", ", values.Select(static x => $"'{x.Value}'"));

        string sql = $"""
                      SELECT * FROM {TValue.TableName}
                      LEFT JOIN {TSelf.TableName}
                      WHERE 
                          {TValue.TableName}.{ColumnMetaData.ID.ColumnName} != {TSelf.TableName}.{_value_id} 
                          AND {TValue.TableName}.{ColumnMetaData.ID.ColumnName} IN ( {ids} )
                          AND {TSelf.TableName}.{_value_id} NOT IN ( {ids} ) 
                          AND {TSelf.TableName}.{_key_id} = @{_key_id}
                      """;

        await foreach ( TValue value in valueTable.Where(connection, transaction, sql, parameters, token) )
        {
            TSelf self = TSelf.Create(key, value);

            await selfTable.Insert(connection, transaction, self, token)
                           .ConfigureAwait(false);
        }
    }


    public static bool Exists( scoped in ReadOnlySpan<TSelf> existing, in RecordID<TValue> target )
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( TSelf self in existing )
        {
            if ( self.ValueID == target ) { return true; }
        }

        return false;
    }
    public static bool Exists( scoped in ReadOnlySpan<TValue> existing, in RecordID<TValue> target )
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( TValue value in existing )
        {
            if ( value.ID == target ) { return true; }
        }

        return false;
    }


    public static       ValueTask<bool>                   Exists( NpgsqlConnection  connection, NpgsqlTransaction  transaction, DbTable<TSelf> selfTable, TKey             key,   TValue                                     value, CancellationToken token ) => Exists(connection, transaction, selfTable, key.ID, value.ID, token);
    public static async ValueTask<bool>                   Exists( NpgsqlConnection  connection, NpgsqlTransaction  transaction, DbTable<TSelf> selfTable, RecordID<TKey>   key,   RecordID<TValue>                           value, CancellationToken token ) => await selfTable.Exists(connection, transaction, true, GetDynamicParameters(key, value), token);
    public static       IAsyncEnumerable<TSelf>           Where( NpgsqlConnection   connection, NpgsqlTransaction? transaction, DbTable<TSelf> selfTable, TKey             key,   [EnumeratorCancellation] CancellationToken token ) => Where(connection, transaction, selfTable, key.ID, token);
    public static       IAsyncEnumerable<TSelf>           Where( NpgsqlConnection   connection, NpgsqlTransaction? transaction, DbTable<TSelf> selfTable, RecordID<TKey>   key,   [EnumeratorCancellation] CancellationToken token ) => selfTable.Where(connection, transaction, true, GetDynamicParameters(key), token);
    public static       IAsyncEnumerable<TSelf>           Where( NpgsqlConnection   connection, NpgsqlTransaction? transaction, DbTable<TSelf> selfTable, TValue           value, [EnumeratorCancellation] CancellationToken token ) => Where(connection, transaction, selfTable, value.ID, token);
    public static       IAsyncEnumerable<TSelf>           Where( NpgsqlConnection   connection, NpgsqlTransaction? transaction, DbTable<TSelf> selfTable, RecordID<TValue> value, [EnumeratorCancellation] CancellationToken token ) => selfTable.Where(connection, transaction, true, GetDynamicParameters(value), token);
    public static       IAsyncEnumerable<RecordID<TSelf>> WhereID( NpgsqlConnection connection, NpgsqlTransaction? transaction, DbTable<TSelf> selfTable, TKey             key,   [EnumeratorCancellation] CancellationToken token ) => WhereID(connection, transaction, selfTable, key.ID, token);
    public static       IAsyncEnumerable<RecordID<TSelf>> WhereID( NpgsqlConnection connection, NpgsqlTransaction? transaction, DbTable<TSelf> selfTable, RecordID<TKey>   key,   [EnumeratorCancellation] CancellationToken token ) => selfTable.WhereID(connection, transaction, true, GetDynamicParameters(key), token);
    public static       IAsyncEnumerable<RecordID<TSelf>> WhereID( NpgsqlConnection connection, NpgsqlTransaction? transaction, DbTable<TSelf> selfTable, TValue           value, [EnumeratorCancellation] CancellationToken token ) => WhereID(connection, transaction, selfTable, value.ID, token);
    public static       IAsyncEnumerable<RecordID<TSelf>> WhereID( NpgsqlConnection connection, NpgsqlTransaction? transaction, DbTable<TSelf> selfTable, RecordID<TValue> value, [EnumeratorCancellation] CancellationToken token ) => selfTable.WhereID(connection, transaction, true, GetDynamicParameters(value), token);


    public static IAsyncEnumerable<TValue> Where( NpgsqlConnection connection, NpgsqlTransaction? transaction, DbTable<TValue> valueTable, RecordID<TKey> key, [EnumeratorCancellation] CancellationToken token )
    {
        string sql = $"""
                      SELECT * FROM {TValue.TableName}
                      INNER JOIN {TSelf.TableName} ON {TSelf.TableName}.{_value_id} = {TValue.TableName}.{ColumnMetaData.ID.ColumnName} 
                      WHERE {TSelf.TableName}.{_key_id} = @{_key_id}
                      """;

        token.ThrowIfCancellationRequested();
        return valueTable.Where(connection, transaction, sql, GetDynamicParameters(key), token);
    }
    public static IAsyncEnumerable<TKey> Where( NpgsqlConnection connection, NpgsqlTransaction? transaction, DbTable<TKey> keyTable, RecordID<TValue> value, [EnumeratorCancellation] CancellationToken token )
    {
        string sql = $"""
                      SELECT * FROM {TKey.TableName}
                      INNER JOIN {TSelf.TableName} ON {TSelf.TableName}.{_key_id} = {TKey.TableName}.{ColumnMetaData.ID.ColumnName} 
                      WHERE {TSelf.TableName}.{_value_id} = @{_value_id}
                      """;

        return keyTable.Where(connection, transaction, sql, GetDynamicParameters(value), token);
    }


    public static async ValueTask Replace( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TSelf> selfTable, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        await Delete(connection, transaction, selfTable, key, token);
        List<TSelf> list = new(DEFAULT_CAPACITY);

        foreach ( RecordID<TValue> value in values )
        {
            TSelf record = TSelf.Create(key, value);
            list.Add(record);
        }

        await selfTable.Insert(connection, transaction, list, token)
                       .ConfigureAwait(false);
    }


    public static async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TSelf> selfTable, RecordID<TKey> key, CancellationToken token ) // TODO: OPTIMIZE THIS!!!
        => await selfTable.Delete(connection, transaction, WhereID(connection, transaction, selfTable, key, token), token)
                          .ConfigureAwait(false);
    public static async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TSelf> selfTable, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE {_value_id} IN ( {values.Ids()} ) AND {_key_id} = @{_key_id}
                      """;

        await selfTable.Execute(connection, transaction, sql, GetDynamicParameters(key), token);
    }
    public static async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TSelf> selfTable, RecordID<TKey> key, RecordID<TValue> value, CancellationToken token ) => await selfTable.Delete(connection, transaction, true, GetDynamicParameters(key, value), token);
}
