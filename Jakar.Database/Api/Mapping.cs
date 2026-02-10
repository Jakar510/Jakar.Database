using Microsoft.AspNetCore.DataProtection.KeyManagement;



namespace Jakar.Database;


public interface ICreateMapping<TSelf, TKey, TValue> : ITableRecord<TSelf>
    where TValue : PairRecord<TValue>, ITableRecord<TValue>
    where TKey : PairRecord<TKey>, ITableRecord<TKey>
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>, ICreateMapping<TSelf, TKey, TValue>
{
    public RecordID<TKey>   KeyID   { get; init; }
    public RecordID<TValue> ValueID { get; init; }


    [Pure]                                  public abstract static TSelf                 Create( TKey           key, TValue                                value );
    [Pure]                                  public abstract static TSelf                 Create( RecordID<TKey> key, RecordID<TValue>                      value );
    [Pure]                                  public abstract static ImmutableArray<TSelf> Create( TKey           key, params ReadOnlySpan<TValue>           values );
    [Pure]                                  public abstract static ImmutableArray<TSelf> Create( RecordID<TKey> key, params ReadOnlySpan<RecordID<TValue>> values );
    [Pure] [OverloadResolutionPriority(-1)] public abstract static IEnumerable<TSelf>    Create( RecordID<TKey> key, IEnumerable<RecordID<TValue>>         values );
}



[Serializable]
[TableExtras(nameof(KeyID), nameof(ValueID))]
public abstract record Mapping<TSelf, TKey, TValue> : TableRecord<TSelf>
    where TValue : PairRecord<TValue>, ITableRecord<TValue>
    where TKey : PairRecord<TKey>, ITableRecord<TKey>
    where TSelf : Mapping<TSelf, TKey, TValue>, ICreateMapping<TSelf, TKey, TValue>, ITableRecord<TSelf>
{
    private WeakReference<TKey>?   __owner;
    private WeakReference<TValue>? __value;


    public abstract RecordID<TKey>   KeyID   { get; init; }
    public abstract RecordID<TValue> ValueID { get; init; }


    protected Mapping( RecordID<TKey> key, RecordID<TValue> value ) : this(key, value, DateTimeOffset.UtcNow) { }
    protected Mapping( RecordID<TKey> keyID, RecordID<TValue> valueID, DateTimeOffset DateCreated ) : base(in DateCreated)
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


    public        bool            IsValid()                        => KeyID.IsValid() && ValueID.IsValid();
    public static MigrationRecord CreateTable( ulong migrationID ) => MigrationRecord.CreateTable<TSelf>(migrationID);


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        using PooledArray<ColumnMetaData> buffer = PropertyMetaData.SortedColumns;

        foreach ( ColumnMetaData column in buffer.Array )
        {
            switch ( column.PropertyName )
            {
                case nameof(DateCreated):
                    await importer.WriteAsync(DateCreated, column.PostgresDbType, token);
                    break;

                case nameof(KeyID):
                    await importer.WriteAsync(KeyID.Value, column.PostgresDbType, token);
                    break;

                case nameof(ValueID):
                    await importer.WriteAsync(ValueID.Value, column.PostgresDbType, token);
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


    public async ValueTask<TKey?> Get( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TKey> table, CancellationToken token )
    {
        if ( __owner is not null && __owner.TryGetTarget(out TKey? value) ) { return value; }

        TKey? record = await table.Get(connection, transaction, KeyID, token)
                                  .ConfigureAwait(false);

        if ( record is not null ) { __owner = new WeakReference<TKey>(record); }

        return record;
    }
    public async ValueTask<TValue?> Get( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TValue> table, CancellationToken token )
    {
        if ( __value is not null && __value.TryGetTarget(out TValue? value) ) { return value; }

        ErrorOrResult<TValue> record = await table.Get(connection, transaction, ValueID, token)
                                                  .ConfigureAwait(false);

        if ( record.TryGetValue(out value) ) { __value = new WeakReference<TValue>(value); }

        return record;
    }


    public static async ValueTask<bool> TryAdd( NpgsqlConnection connection, NpgsqlTransaction transaction, RecordID<TKey> key, RecordID<TValue> value, CancellationToken token )
    {
        if ( await Exists(connection, transaction, key, value, token) ) { return false; }

        TSelf             record  = TSelf.Create(key, value);
        SqlCommand<TSelf> command = SqlCommand<TSelf>.GetInsert(record);
        await command.ExecuteNonQueryAsync(connection, transaction, token);

        return record.IsValid();
    }
    public static async ValueTask<bool> TryAdd( NpgsqlConnection connection, NpgsqlTransaction transaction, TSelf self, CancellationToken token )
    {
        if ( await Exists(connection, transaction, self, token) ) { return false; }

        SqlCommand<TSelf> command = SqlCommand<TSelf>.GetInsert(self);
        await command.ExecuteNonQueryAsync(connection, transaction, token);
        return self.IsValid();
    }
    public static async ValueTask TryAdd( NpgsqlConnection connection, NpgsqlTransaction transaction, DbTable<TValue> valueTable, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        PostgresParameters parameters = GetDynamicParameters(key);
        StringBuilder      ids        = new();
        ids.AppendJoin(", ", values.Select(static x => $"'{x.Value}'"));

        string sql = $"""
                      SELECT * FROM {TValue.TableName} v
                      LEFT JOIN {TSelf.TableName} s
                      WHERE 
                      v.{ColumnMetaData.ID.ColumnName} != s.{nameof(ValueID).SqlColumnName()} 
                      AND v.{ColumnMetaData.ID.ColumnName} IN ( {ids} )
                      AND s.{nameof(ValueID).SqlColumnName()} NOT IN ( {ids} ) 
                      AND s.{nameof(KeyID).SqlColumnName()} = '{key.Value}'
                      """;

        ImmutableArray<RecordID<TValue>> missingValueIDs = await valueTable.WhereID(connection, transaction, sql, parameters, token)
                                                                           .ToImmutableArray(token: token);


        List<TSelf> list = new(DEFAULT_CAPACITY);

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach ( RecordID<TValue> value in missingValueIDs )
        {
            TSelf record = TSelf.Create(key, value);
            list.Add(record);
        }

        SqlCommand<TSelf> command = SqlCommand<TSelf>.GetInsert(list);
        await command.ExecuteNonQueryAsync(connection, transaction, token);
    }
    public static async ValueTask TryAdd( NpgsqlConnection connection, NpgsqlTransaction transaction, ImmutableArray<TSelf> records, CancellationToken token )
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        StringBuilder      sb         = new();

        for ( int index = 0; index < records.Length; index++ )
        {
            TSelf self = records[index];
            sb.Append($"('{self.KeyID.Value}'::uuid, '{self.ValueID.Value}'::uuid)");
            if ( index < records.Length - 1 ) { sb.Append(", "); }

            sb.Append('\n');
        }

        string sql = $"""
                      INSERT INTO mapping_table ({nameof(KeyID).SqlColumnName()}, {nameof(ValueID).SqlColumnName()}, {nameof(DateCreated).SqlColumnName()})
                      SELECT v.KeyID, v.ValueID, NOW()
                      FROM (VALUES
                      {sb}
                      ) AS v(KeyID, ValueID)
                      ON CONFLICT ({nameof(KeyID).SqlColumnName()}, {nameof(ValueID).SqlColumnName()}) DO NOTHING;
                      """;

        SqlCommand<TSelf> command = new(sql, parameters);
        await command.ExecuteNonQueryAsync(connection, transaction, token);
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


    /*
    public static       IAsyncEnumerable<TSelf> Where( NpgsqlConnection  connection, NpgsqlTransaction? transaction, TKey             key,   [EnumeratorCancellation] CancellationToken token ) => Where(connection, transaction, key.ID, token);
    public static       IAsyncEnumerable<TSelf> Where( NpgsqlConnection  connection, NpgsqlTransaction? transaction, RecordID<TKey>   key,   [EnumeratorCancellation] CancellationToken token ) => selfTable.Where(connection, transaction, true, GetDynamicParameters(key), token);
    public static       IAsyncEnumerable<TSelf> Where( NpgsqlConnection  connection, NpgsqlTransaction? transaction, TValue           value, [EnumeratorCancellation] CancellationToken token ) => Where(connection, transaction, value.ID, token);
    public static       IAsyncEnumerable<TSelf> Where( NpgsqlConnection  connection, NpgsqlTransaction? transaction, RecordID<TValue> value, [EnumeratorCancellation] CancellationToken token ) => selfTable.Where(connection, transaction, true, GetDynamicParameters(value), token);
    */


    public static ValueTask<bool> Exists( NpgsqlConnection connection, NpgsqlTransaction transaction, TSelf self, CancellationToken token )                          => Exists(connection, transaction, self.KeyID, self.ValueID, token);
    public static ValueTask<bool> Exists( NpgsqlConnection connection, NpgsqlTransaction transaction, TKey  key,  TValue            value, CancellationToken token ) => Exists(connection, transaction, key.ID,     value.ID,     token);
    public static async ValueTask<bool> Exists( NpgsqlConnection connection, NpgsqlTransaction transaction, RecordID<TKey> key, RecordID<TValue> value, CancellationToken token )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName} 
                      WHERE 
                             {nameof(KeyID).SqlColumnName()} = '{key.Value}'
                          AND 
                             {nameof(ValueID).SqlColumnName()} = '{value.Value}'
                      """;

        SqlCommand<TSelf> command = sql;

        return await command.ExecuteAsync(connection, transaction, token)
                            .Any(token);
    }


    public static IAsyncEnumerable<TValue> Where( NpgsqlConnection connection, NpgsqlTransaction? transaction, DbTable<TValue> valueTable, RecordID<TKey> key, [EnumeratorCancellation] CancellationToken token )
    {
        string sql = $"""
                      SELECT * FROM {TValue.TableName} v
                      INNER JOIN {TSelf.TableName} s ON s.{nameof(ValueID).SqlColumnName()} = v.{ColumnMetaData.ID.ColumnName} 
                      WHERE s.{nameof(KeyID).SqlColumnName()} = '{key.Value}'
                      """;

        return valueTable.Where(connection, transaction, sql, GetDynamicParameters(key), token);
    }
    public static IAsyncEnumerable<TKey> Where( NpgsqlConnection connection, NpgsqlTransaction? transaction, DbTable<TKey> keyTable, RecordID<TValue> value, [EnumeratorCancellation] CancellationToken token )
    {
        string sql = $"""
                      SELECT * FROM {TKey.TableName} k
                      INNER JOIN {TSelf.TableName} s ON s.{nameof(KeyID).SqlColumnName()} = k.{ColumnMetaData.ID.ColumnName} 
                      WHERE s.{nameof(ValueID).SqlColumnName()} = '{value.Value}'
                      """;

        return keyTable.Where(connection, transaction, sql, GetDynamicParameters(value), token);
    }


    public static async ValueTask Replace( NpgsqlConnection connection, NpgsqlTransaction transaction, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        await Delete(connection, transaction, key, token);
        List<TSelf> list = new(DEFAULT_CAPACITY);

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( RecordID<TValue> value in values )
        {
            TSelf record = TSelf.Create(key, value);
            list.Add(record);
        }

        SqlCommand<TSelf> command = SqlCommand<TSelf>.GetInsert(list);
        await command.ExecuteNonQueryAsync(connection, transaction, token);
    }


    public static async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction transaction, TSelf self, CancellationToken token )
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE 
                             {nameof(KeyID).SqlColumnName()} = '{self.KeyID.Value}'
                          AND 
                             {nameof(ValueID).SqlColumnName()} = '{self.ValueID.Value}'
                      """;

        SqlCommand<TSelf> command = sql;
        await command.ExecuteNonQueryAsync(connection, transaction, token);
    }
    public static async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction transaction, RecordID<TKey> key, CancellationToken token )
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE {nameof(KeyID).SqlColumnName()} = '{key.Value}'
                      """;

        SqlCommand<TSelf> command = new(sql, GetDynamicParameters(key));
        await command.ExecuteNonQueryAsync(connection, transaction, token);
    }
    public static async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction transaction, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE
                              {nameof(ValueID).SqlColumnName()} IN ( {values.Ids()} ) 
                           AND 
                              {nameof(KeyID).SqlColumnName()} = '{key.Value}'
                      """;

        SqlCommand<TSelf> command = new(sql, GetDynamicParameters(key));
        await command.ExecuteNonQueryAsync(connection, transaction, token);
    }
    public static async ValueTask Delete( NpgsqlConnection connection, NpgsqlTransaction transaction, RecordID<TKey> key, RecordID<TValue> value, CancellationToken token )
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE 
                            {nameof(ValueID).SqlColumnName()} = '{value.Value}'
                         AND 
                            {nameof(KeyID).SqlColumnName()} = '{key.Value}'
                      """;

        SqlCommand<TSelf> command = new(sql, GetDynamicParameters(key, value));
        await command.ExecuteNonQueryAsync(connection, transaction, token);
    }
}
