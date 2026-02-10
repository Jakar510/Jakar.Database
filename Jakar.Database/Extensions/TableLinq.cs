// Jakar.Extensions :: Jakar.Database
// 09/06/2022  4:54 PM

namespace Jakar.Database;


public static class TableLinq
{
    extension( NpgsqlDataReader reader )
    {
        public async ValueTask<ErrorOrResult<TSelf>> FirstAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
            where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
        {
            await foreach ( TSelf self in reader.CreateAsync<TSelf>(token) ) { return self; }

            throw new InvalidOperationException("Sequence contains no elements");
        }
        public async ValueTask<ErrorOrResult<TSelf>> FirstOrDefaultAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
            where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
        {
            await foreach ( TSelf self in reader.CreateAsync<TSelf>(token) ) { return self; }

            return Error.NotFound();
        }
        public async ValueTask<ErrorOrResult<TSelf>> SingleAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
            where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
        {
            TSelf? record = null;

            await foreach ( TSelf self in reader.CreateAsync<TSelf>(token) )
            {
                if ( record is not null ) { throw new InvalidOperationException("Sequence contains more than one element"); }

                record = self;
            }

            return record is null
                       ? Error.NotFound()
                       : record;
        }
        public async ValueTask<ErrorOrResult<TSelf>> SingleOrDefaultAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
            where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
        {
            TSelf? record = null;

            await foreach ( TSelf self in reader.CreateAsync<TSelf>(token) )
            {
                if ( record is not null )
                {
                    record = null;
                    break;
                }

                record = self;
            }

            return record is null
                       ? Error.NotFound()
                       : record;
        }
        public async ValueTask<ErrorOrResult<TSelf>> LastAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
            where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
        {
            TSelf? record = null;
            await foreach ( TSelf self in reader.CreateAsync<TSelf>(token) ) { record = self; }

            return record is null
                       ? Error.NotFound()
                       : record;
        }
        public async ValueTask<ErrorOrResult<TSelf>> LastOrDefaultAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
            where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
        {
            TSelf? record = null;
            await foreach ( TSelf self in reader.CreateAsync<TSelf>(token) ) { record = self; }

            return record is null
                       ? Error.NotFound()
                       : record;
        }
        public async IAsyncEnumerable<TSelf> CreateAsync<TSelf>( [EnumeratorCancellation] CancellationToken token = default )
            where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
        {
            while ( await reader.ReadAsync(token) ) { yield return TSelf.Create(reader); }
        }
        public async ValueTask<ImmutableArray<TSelf>> CreateAsync<TSelf>( int initialCapacity, [EnumeratorCancellation] CancellationToken token = default )
            where TSelf : TableRecord<TSelf>,  ITableRecord<TSelf>
        {
            List<TSelf> list = new(initialCapacity);
            while ( await reader.ReadAsync(token) ) { list.Add(TSelf.Create(reader)); }

            return [..list];
        }
    }



    extension<TSelf>( IAsyncEnumerable<TSelf> self )
    {
        public async IAsyncEnumerable<TSelf> Where( Func<NpgsqlConnection, NpgsqlTransaction?, TSelf, CancellationToken, bool> func, NpgsqlConnection connection, NpgsqlTransaction? transaction, [EnumeratorCancellation] CancellationToken token = default )
        {
            await foreach ( TSelf record in self.WithCancellation(token) )
            {
                if ( func(connection, transaction, record, token) ) { yield return record; }
            }
        }
        public async IAsyncEnumerable<TSelf> Where( Func<NpgsqlConnection, NpgsqlTransaction?, TSelf, CancellationToken, ValueTask<bool>> func, NpgsqlConnection connection, NpgsqlTransaction? transaction, [EnumeratorCancellation] CancellationToken token = default )
        {
            await foreach ( TSelf record in self.WithCancellation(token) )
            {
                if ( await func(connection, transaction, record, token) ) { yield return record; }
            }
        }
        public async IAsyncEnumerable<TResult> Select<TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TSelf, CancellationToken, TResult> func, NpgsqlConnection connection, NpgsqlTransaction? transaction, [EnumeratorCancellation] CancellationToken token = default )
        {
            await foreach ( TSelf record in self.WithCancellation(token) ) { yield return func(connection, transaction, record, token); }
        }
        public async IAsyncEnumerable<TResult> Select<TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TSelf, CancellationToken, ValueTask<TResult>> func, NpgsqlConnection connection, NpgsqlTransaction? transaction, [EnumeratorCancellation] CancellationToken token = default )
        {
            await foreach ( TSelf record in self.WithCancellation(token) ) { yield return await func(connection, transaction, record, token); }
        }
    }
}
