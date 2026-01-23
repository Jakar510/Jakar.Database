namespace Jakar.Database;


public static partial class DbExtensions
{
    extension( IConnectableDb self )
    {
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection             connection = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection, null, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection             connection = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection, null, arg1, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection             connection = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection, null, arg1, arg2, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection             connection = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection, null, arg1, arg2, arg3, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection             connection = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection, null, arg1, arg2, arg3, arg4, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection             connection = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection, null, arg1, arg2, arg3, arg4, arg5, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func,
                                                                                                                       TArg1                                                                                                                                             arg1,
                                                                                                                       TArg2                                                                                                                                             arg2,
                                                                                                                       TArg3                                                                                                                                             arg3,
                                                                                                                       TArg4                                                                                                                                             arg4,
                                                                                                                       TArg5                                                                                                                                             arg5,
                                                                                                                       TArg6                                                                                                                                             arg6,
                                                                                                                       [EnumeratorCancellation] CancellationToken                                                                                                        token
        )
        {
            await using NpgsqlConnection             connection = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection, null, arg1, arg2, arg3, arg4, arg5, arg6, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func,
                                                                                                                              TArg1                                                                                                                                                    arg1,
                                                                                                                              TArg2                                                                                                                                                    arg2,
                                                                                                                              TArg3                                                                                                                                                    arg3,
                                                                                                                              TArg4                                                                                                                                                    arg4,
                                                                                                                              TArg5                                                                                                                                                    arg5,
                                                                                                                              TArg6                                                                                                                                                    arg6,
                                                                                                                              TArg7                                                                                                                                                    arg7,
                                                                                                                              [EnumeratorCancellation] CancellationToken                                                                                                               token
        )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);

            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func,
                                                                                                                                     TArg1                                                                                                                                                           arg1,
                                                                                                                                     TArg2                                                                                                                                                           arg2,
                                                                                                                                     TArg3                                                                                                                                                           arg3,
                                                                                                                                     TArg4                                                                                                                                                           arg4,
                                                                                                                                     TArg5                                                                                                                                                           arg5,
                                                                                                                                     TArg6                                                                                                                                                           arg6,
                                                                                                                                     TArg7                                                                                                                                                           arg7,
                                                                                                                                     TArg8                                                                                                                                                           arg8,
                                                                                                                                     [EnumeratorCancellation] CancellationToken                                                                                                                      token
        )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);

            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(connection,
                                                                       null,
                                                                       arg1,
                                                                       arg2,
                                                                       arg3,
                                                                       arg4,
                                                                       arg5,
                                                                       arg6,
                                                                       arg7,
                                                                       arg8,
                                                                       token);


            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, CancellationToken token )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);
            return await func(connection, null, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, CancellationToken token )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);
            return await func(connection, null, arg1, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, CancellationToken token )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);
            return await func(connection, null, arg1, arg2, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);
            return await func(connection, null, arg1, arg2, arg3, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);
            return await func(connection, null, arg1, arg2, arg3, arg4, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);
            return await func(connection, null, arg1, arg2, arg3, arg4, arg5, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);
            return await func(connection, null, arg1, arg2, arg3, arg4, arg5, arg6, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func,
                                                                                                                       TArg1                                                                                                                                             arg1,
                                                                                                                       TArg2                                                                                                                                             arg2,
                                                                                                                       TArg3                                                                                                                                             arg3,
                                                                                                                       TArg4                                                                                                                                             arg4,
                                                                                                                       TArg5                                                                                                                                             arg5,
                                                                                                                       TArg6                                                                                                                                             arg6,
                                                                                                                       TArg7                                                                                                                                             arg7,
                                                                                                                       CancellationToken                                                                                                                                 token
        )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);

            return await func(connection, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<NpgsqlConnection, NpgsqlTransaction?, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func,
                                                                                                                              TArg1                                                                                                                                                    arg1,
                                                                                                                              TArg2                                                                                                                                                    arg2,
                                                                                                                              TArg3                                                                                                                                                    arg3,
                                                                                                                              TArg4                                                                                                                                                    arg4,
                                                                                                                              TArg5                                                                                                                                                    arg5,
                                                                                                                              TArg6                                                                                                                                                    arg6,
                                                                                                                              TArg7                                                                                                                                                    arg7,
                                                                                                                              TArg8                                                                                                                                                    arg8,
                                                                                                                              CancellationToken                                                                                                                                        token
        )
        {
            await using NpgsqlConnection connection = await self.ConnectAsync(token);

            return await func(connection,
                              null,
                              arg1,
                              arg2,
                              arg3,
                              arg4,
                              arg5,
                              arg6,
                              arg7,
                              arg8,
                              token);
        }
    }
}
