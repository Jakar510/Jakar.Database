// Jakar.Extensions :: Jakar.Database
// 09/06/2022  4:54 PM

namespace Jakar.Database;


public static class TableLinq
{
    extension( DbDataReader reader )
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
        public async IAsyncEnumerable<TSelf> Where( Func<DbConnectionContext, TSelf, CancellationToken, bool> func, DbConnectionContext context, [EnumeratorCancellation] CancellationToken token = default )
        {
            await foreach ( TSelf record in self.WithCancellation(token) )
            {
                if ( func(context, record, token) ) { yield return record; }
            }
        }
        public async IAsyncEnumerable<TSelf> Where( Func<DbConnectionContext, TSelf, CancellationToken, ValueTask<bool>> func, DbConnectionContext context, [EnumeratorCancellation] CancellationToken token = default )
        {
            await foreach ( TSelf record in self.WithCancellation(token) )
            {
                if ( await func(context, record, token) ) { yield return record; }
            }
        }
        public async IAsyncEnumerable<TResult> Select<TResult>( Func<DbConnectionContext, TSelf, CancellationToken, TResult> func, DbConnectionContext context, [EnumeratorCancellation] CancellationToken token = default )
        {
            await foreach ( TSelf record in self.WithCancellation(token) ) { yield return func(context, record, token); }
        }
        public async IAsyncEnumerable<TResult> Select<TResult>( Func<DbConnectionContext, TSelf, CancellationToken, ValueTask<TResult>> func, DbConnectionContext context, [EnumeratorCancellation] CancellationToken token = default )
        {
            await foreach ( TSelf record in self.WithCancellation(token) ) { yield return await func(context, record, token); }
        }
    }
}
