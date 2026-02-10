namespace Jakar.Database.Resx;


public sealed class ResxCollection : IResxCollection
{
    private readonly ConcurrentBag<ResxRowRecord> __rows = [];


    public int Count => __rows.Count;


    public ResxCollection() { }
    public ResxCollection( IEnumerable<ResxRowRecord> collection ) => __rows.Add(collection);


    public ResxSet GetSet( in SupportedLanguage language )
    {
        ResxSet set = new(DbOptions.ConcurrencyLevel, Count);
        foreach ( ResxRowRecord row in __rows ) { set[row.KeyID.Value] = row.GetValue(language); }

        return set;
    }

    public async ValueTask<ResxSet> GetSetAsync( IConnectableDb db, IResxProvider provider, SupportedLanguage language, CancellationToken token = default )
    {
        if ( __rows.IsEmpty ) { await Init(db, provider, token); }

        return GetSet(language);
    }


    public ValueTask Init( IConnectableDb db, IResxProvider provider, CancellationToken token = default ) => db.Call(Init, provider, token);
    public async ValueTask Init( NpgsqlConnection connection, NpgsqlTransaction? transaction, IResxProvider provider, CancellationToken token = default )
    {
        __rows.Clear();
        SqlCommand<ResxRowRecord> command = provider.Get;
        await foreach ( ResxRowRecord record in command.ExecuteAsync(connection, transaction, token) ) { __rows.Add(record); }
    }


    [MustDisposeResource] public IEnumerator<ResxRowRecord> GetEnumerator() => __rows.GetEnumerator();
    [MustDisposeResource]        IEnumerator IEnumerable.   GetEnumerator() => GetEnumerator();
}
