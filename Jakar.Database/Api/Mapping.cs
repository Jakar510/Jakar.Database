namespace Jakar.Database;


public interface ICreateMapping<TSelf, TKey, TValue> : ITableRecord<TSelf>
    where TValue : PairRecord<TValue>, ITableRecord<TValue>
    where TKey : PairRecord<TKey>, ITableRecord<TKey>
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>, ICreateMapping<TSelf, TKey, TValue>
{
    [Key] [DbIgnore] public RecordID<TKey, TValue> ID      { get; }
    public                  RecordID<TKey>         KeyID   { get; init; }
    public                  RecordID<TValue>       ValueID { get; init; }


    [Pure]                                  public abstract static TSelf                 Create( TKey           key, TValue                                value );
    [Pure]                                  public abstract static TSelf                 Create( RecordID<TKey> key, RecordID<TValue>                      value );
    [Pure]                                  public abstract static ImmutableArray<TSelf> Create( TKey           key, params ReadOnlySpan<TValue>           values );
    [Pure]                                  public abstract static ImmutableArray<TSelf> Create( RecordID<TKey> key, params ReadOnlySpan<RecordID<TValue>> values );
    [Pure] [OverloadResolutionPriority(-1)] public abstract static IEnumerable<TSelf>    Create( RecordID<TKey> key, IEnumerable<RecordID<TValue>>         values );
}



[Serializable]
[TableExtras(nameof(KeyID), nameof(ValueID))]
public abstract record Mapping<TSelf, TKey, TValue> : TableRecord<TSelf>
    where TValue : OwnedTableRecord<TValue>, ITableRecord<TValue>
    where TKey : PairRecord<TKey>, ITableRecord<TKey>
    where TSelf : Mapping<TSelf, TKey, TValue>, ICreateMapping<TSelf, TKey, TValue>, ITableRecord<TSelf>
{
    private WeakReference<TKey>?   __owner;
    private WeakReference<TValue>? __value;


    [Key] public    RecordID<TKey, TValue> ID      => new(KeyID, ValueID);
    public abstract RecordID<TKey>         KeyID   { get; init; }
    public abstract RecordID<TValue>       ValueID { get; init; }


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
    protected internal Mapping( DbDataReader reader ) : base(reader)
    {
        KeyID   = RecordID<TKey>.Create(reader, nameof(KeyID));
        ValueID = RecordID<TValue>.Create(reader, nameof(ValueID));
    }


    public static CommandParameters GetDynamicParameters( TValue record ) => GetDynamicParameters(record.ID);
    public static CommandParameters GetDynamicParameters( RecordID<TValue> value )
    {
        CommandParameters parameters = CommandParameters.Create<TSelf>();
        parameters.Add(nameof(ValueID), value);
        return parameters;
    }
    public static CommandParameters GetDynamicParameters( TKey key ) => GetDynamicParameters(key.ID);
    public static CommandParameters GetDynamicParameters( RecordID<TKey> key )
    {
        CommandParameters parameters = CommandParameters.Create<TSelf>();
        parameters.Add(nameof(KeyID), key);
        return parameters;
    }
    public static CommandParameters GetDynamicParameters( TKey key, TValue value ) => GetDynamicParameters(key.ID, value.ID);
    public static CommandParameters GetDynamicParameters( RecordID<TKey> key, RecordID<TValue> value )
    {
        CommandParameters parameters = CommandParameters.Create<TSelf>();
        parameters.Add(nameof(KeyID),   key);
        parameters.Add(nameof(ValueID), value);
        return parameters;
    }


    public bool IsValid() => KeyID.IsValid() && ValueID.IsValid();


    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    protected override async ValueTask Import( NpgsqlBinaryImporter importer, string propertyName, NpgsqlDbType postgresDbType, CancellationToken token )
    {
        switch ( propertyName )
        {
            case nameof(DateCreated):
                await importer.WriteAsync(DateCreated, postgresDbType, token);
                break;

            case nameof(KeyID):
                await importer.WriteAsync(KeyID.Value, postgresDbType, token);
                break;

            case nameof(ValueID):
                await importer.WriteAsync(ValueID.Value, postgresDbType, token);
                break;

            default:
                throw new InvalidOperationException($"Unknown column: {propertyName}");
        }
    }
    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(KeyID)].DataColumn]   = KeyID;
        row[MetaData[nameof(ValueID)].DataColumn] = ValueID;
        return base.Import(row, token);
    }
    public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
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


    public async ValueTask<TKey?> Get( DbConnectionContext context, DbTable<TKey> table, CancellationToken token )
    {
        if ( __owner is not null && __owner.TryGetTarget(out TKey? value) ) { return value; }

        TKey? record = await table.Get(context, KeyID, token).ConfigureAwait(false);

        if ( record is not null ) { __owner = new WeakReference<TKey>(record); }

        return record;
    }
    public async ValueTask<TValue?> Get( DbConnectionContext context, DbTable<TValue> table, CancellationToken token )
    {
        if ( __value is not null && __value.TryGetTarget(out TValue? value) ) { return value; }

        ErrorOrResult<TValue> record = await table.Get(context, ValueID, token).ConfigureAwait(false);

        if ( record.TryGetValue(out value) ) { __value = new WeakReference<TValue>(value); }

        return record;
    }


    public static async ValueTask<bool> TryAdd( DbConnectionContext context, RecordID<TKey> key, RecordID<TValue> value, CancellationToken token )
    {
        if ( await Exists(context, key, value, token) ) { return false; }

        TSelf             self       = TSelf.Create(key, value);
        CommandParameters parameters = self.ToDynamicParameters();

        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      INSERT INTO {TSelf.TableName}
                                                      (
                                                      {TSelf.MetaData.ColumnNames(1)}
                                                      )
                                                      VALUES
                                                      {parameters.VariableNames(1)}

                                                      RETURNING {nameof(IUniqueID.ID)};
                                                      """);

        await context.ExecuteNonQueryAsync(command, token);

        return self.IsValid();
    }
    public static async ValueTask<bool> TryAdd( DbConnectionContext context, TSelf self, CancellationToken token )
    {
        if ( await Exists(context, self, token) ) { return false; }

        CommandParameters parameters = self.ToDynamicParameters();

        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      INSERT INTO {TSelf.TableName}
                                                      (
                                                      {TSelf.MetaData.ColumnNames(1)}
                                                      )
                                                      VALUES
                                                      {parameters.VariableNames(1)}

                                                      RETURNING {nameof(IUniqueID.ID)};
                                                      """);

        await context.ExecuteNonQueryAsync(command, token);
        return true;
    }
    public static async ValueTask TryAdd( DbConnectionContext context, DbTable<TValue> valueTable, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        CommandParameters parameters = GetDynamicParameters(key);

        // ReSharper disable PossibleMultipleEnumeration
        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      SELECT *
                                                      FROM {TValue.TableName} v
                                                        LEFT JOIN {TSelf.TableName} s
                                                      WHERE
                                                            v.{nameof(IUniqueID.ID)} != s.{nameof(ValueID)}
                                                          AND
                                                            v.{nameof(IUniqueID.ID)} IN ( {values.Select(RecordID<TValue>.GetValue)} )
                                                          AND 
                                                            s.{nameof(ValueID)} NOT IN ( {values.Select(RecordID<TValue>.GetValue)} )
                                                          AND 
                                                            s.{nameof(KeyID)} = {key.Value}
                                                      """);

        // ReSharper restore PossibleMultipleEnumeration

        ImmutableArray<RecordID<TValue>> missingValueIDs = await valueTable.WhereID(context, command, token).ToImmutableArray(token: token);

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach ( RecordID<TValue> value in missingValueIDs ) { parameters.AddGroup(TSelf.Create(key, value)); }

        SqlCommand insertCommand = SqlCommand.Parse<TSelf>($"""
                                                            INSERT INTO {TSelf.TableName}
                                                            (
                                                            {TSelf.MetaData.ColumnNames(1)}
                                                            )
                                                            VALUES
                                                            {parameters.VariableNames(1)}
                                                            RETURNING {nameof(IUniqueID.ID)};
                                                            """);

        await context.ExecuteNonQueryAsync(insertCommand, token);
    }
    public static async ValueTask TryAdd( DbConnectionContext context, ImmutableArray<TSelf> records, CancellationToken token )
    {
        StringBuilder sb = new(10240);

        for ( int index = 0; index < records.Length; index++ )
        {
            TSelf self = records[index];
            sb.Append($"('{self.KeyID.Value}'::uuid, '{self.ValueID.Value}'::uuid)");
            if ( index < records.Length - 1 ) { sb.Append(", "); }

            sb.Append('\n');
        }

        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      INSERT INTO {TSelf.TableName} ({nameof(KeyID)}, {nameof(ValueID)}, {nameof(DateCreated)})
                                                      SELECT v.KeyID, v.ValueID, NOW()
                                                      FROM (VALUES
                                                      {sb}) AS v(KeyID, ValueID) ON CONFLICT ({nameof(KeyID)}, {nameof(ValueID)}) DO NOTHING;
                                                      """);

        await context.ExecuteNonQueryAsync(command, token);
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
    public static       IAsyncEnumerable<TSelf> Where( DbConnectionContext context, TKey             key,   [EnumeratorCancellation] CancellationToken token ) => Where(context, key.ID, token);
    public static       IAsyncEnumerable<TSelf> Where( DbConnectionContext context, RecordID<TKey>   key,   [EnumeratorCancellation] CancellationToken token ) => selfTable.Where(context, true, GetDynamicParameters(key), token);
    public static       IAsyncEnumerable<TSelf> Where( DbConnectionContext context, TValue           value, [EnumeratorCancellation] CancellationToken token ) => Where(context, value.ID, token);
    public static       IAsyncEnumerable<TSelf> Where( DbConnectionContext context, RecordID<TValue> value, [EnumeratorCancellation] CancellationToken token ) => selfTable.Where(context, true, GetDynamicParameters(value), token);
    */


    public static ValueTask<bool> Exists( DbConnectionContext context, TSelf self, CancellationToken token )                          => Exists(context, self.KeyID, self.ValueID, token);
    public static ValueTask<bool> Exists( DbConnectionContext context, TKey  key,  TValue            value, CancellationToken token ) => Exists(context, key.ID,     value.ID,     token);
    public static async ValueTask<bool> Exists( DbConnectionContext context, RecordID<TKey> key, RecordID<TValue> value, CancellationToken token )
    {
        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      SELECT *
                                                      FROM {TSelf.TableName}
                                                      WHERE
                                                        {nameof(KeyID)} = {key.Value}
                                                      AND
                                                        {nameof(ValueID)} = {value.Value}
                                                      """);

        return await context.ExecuteAsync<TSelf>(command, token).AnyAsync(token);
    }


    public static IAsyncEnumerable<TValue> Where( DbConnectionContext context, DbTable<TValue> valueTable, RecordID<TKey> key, [EnumeratorCancellation] CancellationToken token )
    {
        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      SELECT *
                                                      FROM {TValue.TableName} v
                                                      INNER JOIN {TSelf.TableName} s
                                                        ON s.{nameof(ValueID)} = v.{nameof(IUniqueID.ID)}
                                                      WHERE s.{nameof(KeyID)} = @{key.Value}
                                                      """);

        return valueTable.Where(context, command, token);
    }
    public static IAsyncEnumerable<TKey> Where( DbConnectionContext context, DbTable<TKey> keyTable, RecordID<TValue> value, [EnumeratorCancellation] CancellationToken token )
    {
        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      SELECT *
                                                      FROM {TKey.TableName} k
                                                      INNER JOIN {TSelf.TableName} s
                                                        ON s.{nameof(KeyID)} = k.{nameof(IUniqueID.ID)}
                                                      WHERE s.{nameof(ValueID)} = {value.Value}
                                                      """);

        return keyTable.Where(context, command, token);
    }


    public static async ValueTask Replace( DbConnectionContext context, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        await Delete(context, key, token);
        List<TSelf> list = new(DEFAULT_CAPACITY);

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( RecordID<TValue> value in values )
        {
            TSelf record = TSelf.Create(key, value);
            list.Add(record);
        }

        CommandParameters parameters = CommandParameters.Create(list.AsSpan());

        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      INSERT INTO {TSelf.TableName} 
                                                      (
                                                      {TSelf.MetaData.ColumnNames(1)}
                                                      )
                                                      VALUES
                                                      {parameters.VariableNames(1)}

                                                      RETURNING {nameof(IUniqueID.ID)};
                                                      """);

        await context.ExecuteNonQueryAsync(command, token);
    }


    public static async ValueTask Delete( DbConnectionContext context, TSelf self, CancellationToken token )
    {
        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      DELETE FROM {TSelf.TableName}
                                                      WHERE
                                                            {nameof(KeyID)} = {self.KeyID.Value}
                                                          AND
                                                            {nameof(ValueID)} = {self.ValueID.Value}
                                                      """);

        await context.ExecuteNonQueryAsync(command, token);
    }
    public static async ValueTask Delete( DbConnectionContext context, RecordID<TKey> key, CancellationToken token )
    {
        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      DELETE FROM {TSelf.TableName}
                                                      WHERE {nameof(KeyID)} = {key.Value}
                                                      """);

        await context.ExecuteNonQueryAsync(command, token);
    }
    public static async ValueTask Delete( DbConnectionContext context, RecordID<TKey> key, IEnumerable<RecordID<TValue>> values, CancellationToken token )
    {
        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      DELETE FROM {TSelf.TableName}
                                                      WHERE
                                                            {nameof(ValueID)} IN ( {values} )
                                                          AND
                                                            {nameof(KeyID)} = {key.Value}
                                                      """);

        await context.ExecuteNonQueryAsync(command, token);
    }
    public static async ValueTask Delete( DbConnectionContext context, RecordID<TKey> key, RecordID<TValue> value, CancellationToken token )
    {
        SqlCommand command = SqlCommand.Parse<TSelf>($"""
                                                      DELETE FROM {TSelf.TableName} 
                                                      WHERE 
                                                            {nameof(ValueID)} = {value.Value}
                                                         AND 
                                                            {nameof(KeyID)} = {key.Value}
                                                      """);

        await context.ExecuteNonQueryAsync(command, token);
    }
}
