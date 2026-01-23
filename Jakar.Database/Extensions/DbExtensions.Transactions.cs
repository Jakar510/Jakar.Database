namespace Jakar.Database;


public static partial class DbExtensions
{
    extension( IConnectableDb self )
    {
        public async IAsyncEnumerable<TResult> TryCall<TResult>( Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, IAsyncEnumerable<TResult>> func, [EnumeratorCancellation] CancellationToken token = default )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn, transaction, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn, transaction, arg1, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn, transaction, arg1, arg2, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn, transaction, arg1, arg2, arg3, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn, transaction, arg1, arg2, arg3, arg4, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn, transaction, arg1, arg2, arg3, arg4, arg5, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, [EnumeratorCancellation] CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn, transaction, arg1, arg2, arg3, arg4, arg5, arg6, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, IAsyncEnumerable<TResult>> func,
                                                                                                                  TArg1                                                                                                                                    arg1,
                                                                                                                  TArg2                                                                                                                                    arg2,
                                                                                                                  TArg3                                                                                                                                    arg3,
                                                                                                                  TArg4                                                                                                                                    arg4,
                                                                                                                  TArg5                                                                                                                                    arg5,
                                                                                                                  TArg6                                                                                                                                    arg6,
                                                                                                                  TArg7                                                                                                                                    arg7,
                                                                                                                  [EnumeratorCancellation] CancellationToken                                                                                               token
        )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn, transaction, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, IAsyncEnumerable<TResult>> func,
                                                                                                                         TArg1                                                                                                                                           arg1,
                                                                                                                         TArg2                                                                                                                                           arg2,
                                                                                                                         TArg3                                                                                                                                           arg3,
                                                                                                                         TArg4                                                                                                                                           arg4,
                                                                                                                         TArg5                                                                                                                                           arg5,
                                                                                                                         TArg6                                                                                                                                           arg6,
                                                                                                                         TArg7                                                                                                                                           arg7,
                                                                                                                         TArg8                                                                                                                                           arg8,
                                                                                                                         [EnumeratorCancellation] CancellationToken                                                                                                      token
        )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);
            IAsyncEnumerable<TResult>     result;

            try
            {
                result = func(conn,
                              transaction,
                              arg1,
                              arg2,
                              arg3,
                              arg4,
                              arg5,
                              arg6,
                              arg7,
                              arg8,
                              token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async ValueTask TryCall( Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, ValueTask> func, CancellationToken token = default )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn, transaction, token);
                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, CancellationToken, ValueTask> func, TArg1 arg1, CancellationToken token = default )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn, transaction, arg1, token);
                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, CancellationToken token = default )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn, transaction, arg1, arg2, token);
                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token = default )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn, transaction, arg1, arg2, arg3, token);
                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn, transaction, arg1, arg2, arg3, arg4, token);
                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4, TArg5>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn, transaction, arg1, arg2, arg3, arg4, arg5, token);
                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn, transaction, arg1, arg2, arg3, arg4, arg5, arg6, token);
                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn, transaction, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask> func,
                                                                                                TArg1                                                                                                                           arg1,
                                                                                                TArg2                                                                                                                           arg2,
                                                                                                TArg3                                                                                                                           arg3,
                                                                                                TArg4                                                                                                                           arg4,
                                                                                                TArg5                                                                                                                           arg5,
                                                                                                TArg6                                                                                                                           arg6,
                                                                                                TArg7                                                                                                                           arg7,
                                                                                                TArg8                                                                                                                           arg8,
                                                                                                CancellationToken                                                                                                               token
        )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                await func(conn,
                           transaction,
                           arg1,
                           arg2,
                           arg3,
                           arg4,
                           arg5,
                           arg6,
                           arg7,
                           arg8,
                           token);

                await transaction.CommitAsync(token);
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TResult>( Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, ValueTask<TResult>> func, CancellationToken token = default )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn, transaction, token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, CancellationToken token = default )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn, transaction, arg1, token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, CancellationToken token = default )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn, transaction, arg1, arg2, token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn, transaction, arg1, arg2, arg3, token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn, transaction, arg1, arg2, arg3, arg4, token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn, transaction, arg1, arg2, arg3, arg4, arg5, token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn, transaction, arg1, arg2, arg3, arg4, arg5, arg6, token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, CancellationToken token )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn, transaction, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<NpgsqlConnection, NpgsqlTransaction, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask<TResult>> func,
                                                                                                                  TArg1                                                                                                                                    arg1,
                                                                                                                  TArg2                                                                                                                                    arg2,
                                                                                                                  TArg3                                                                                                                                    arg3,
                                                                                                                  TArg4                                                                                                                                    arg4,
                                                                                                                  TArg5                                                                                                                                    arg5,
                                                                                                                  TArg6                                                                                                                                    arg6,
                                                                                                                  TArg7                                                                                                                                    arg7,
                                                                                                                  TArg8                                                                                                                                    arg8,
                                                                                                                  CancellationToken                                                                                                                        token
        )
        {
            await using NpgsqlConnection  conn        = await self.ConnectAsync(token);
            await using NpgsqlTransaction transaction = await conn.BeginTransactionAsync(self.TransactionIsolationLevel, token);

            try
            {
                TResult result = await func(conn,
                                            transaction,
                                            arg1,
                                            arg2,
                                            arg3,
                                            arg4,
                                            arg5,
                                            arg6,
                                            arg7,
                                            arg8,
                                            token);

                await transaction.CommitAsync(token);
                return result;
            }
            catch ( Exception )
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }
    }
}
