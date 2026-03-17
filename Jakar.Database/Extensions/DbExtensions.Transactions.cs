namespace Jakar.Database;


public static partial class DbExtensions
{
    extension( IConnectableDb self )
    {
        public async IAsyncEnumerable<TResult> TryCall<TResult>( Func<DbConnectionContext, CancellationToken, IAsyncEnumerable<TResult>> func, [EnumeratorCancellation] CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TResult>( Func<DbConnectionContext, TArg1, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, arg1, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TResult>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, arg1, arg2, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, arg1, arg2, arg3, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, arg1, arg2, arg3, arg4, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, arg1, arg2, arg3, arg4, arg5, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, arg1, arg2, arg3, arg4, arg5, arg6, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, IAsyncEnumerable<TResult>> func,
                                                                                                                         TArg1                                                                                                                           arg1,
                                                                                                                         TArg2                                                                                                                           arg2,
                                                                                                                         TArg3                                                                                                                           arg3,
                                                                                                                         TArg4                                                                                                                           arg4,
                                                                                                                         TArg5                                                                                                                           arg5,
                                                                                                                         TArg6                                                                                                                           arg6,
                                                                                                                         TArg7                                                                                                                           arg7,
                                                                                                                         TArg8                                                                                                                           arg8,
                                                                                                                         [EnumeratorCancellation] CancellationToken                                                                                      token
        )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            List<TResult>                   list    = new(64);

            try
            {
                await foreach ( TResult result in func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token) ) { list.Add(result); }

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }

            foreach ( TResult item in list ) { yield return item; }
        }


        public async ValueTask TryCall( Func<DbConnectionContext, CancellationToken, ValueTask> func, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, token);
                await context.CommitAsync(token);
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1>( Func<DbConnectionContext, TArg1, CancellationToken, ValueTask> func, TArg1 arg1, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, arg1, token);
                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, arg1, arg2, token);
                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, arg1, arg2, arg3, token);
                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, arg1, arg2, arg3, arg4, token);
                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4, TArg5>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, arg1, arg2, arg3, arg4, arg5, token);
                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);
                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);

                await context.CommitAsync(token);
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }


        public async ValueTask<TResult> TryCall<TResult>( Func<DbConnectionContext, CancellationToken, ValueTask<TResult>> func, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, token);
                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TResult>( Func<DbConnectionContext, TArg1, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, arg1, token);
                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TResult>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, arg1, arg2, token);
                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, arg1, arg2, arg3, token);
                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, arg1, arg2, arg3, arg4, token);
                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, arg1, arg2, arg3, arg4, arg5, token);
                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);
                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<TResult> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                TResult result = await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);

                await context.CommitAsync(token);
                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }


        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TResult>( Func<DbConnectionContext, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, [EnumeratorCancellation] CancellationToken token = default )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, token);
            bool                                     passed     = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TArg1, TResult>( Func<DbConnectionContext, TArg1, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, token);
            bool                                     passed     = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TResult>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, token);
            bool                                     passed     = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, token);
            bool                                     passed     = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, token);
            bool                                     passed     = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, arg5, token);
            bool                                     passed     = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);
            bool                                     passed     = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func,
                                                                                                                                 TArg1                                                                                                                                   arg1,
                                                                                                                                 TArg2                                                                                                                                   arg2,
                                                                                                                                 TArg3                                                                                                                                   arg3,
                                                                                                                                 TArg4                                                                                                                                   arg4,
                                                                                                                                 TArg5                                                                                                                                   arg5,
                                                                                                                                 TArg6                                                                                                                                   arg6,
                                                                                                                                 TArg7                                                                                                                                   arg7,
                                                                                                                                 [EnumeratorCancellation] CancellationToken                                                                                              token
        )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

            bool passed = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func,
                                                                                                                                        TArg1                                                                                                                                          arg1,
                                                                                                                                        TArg2                                                                                                                                          arg2,
                                                                                                                                        TArg3                                                                                                                                          arg3,
                                                                                                                                        TArg4                                                                                                                                          arg4,
                                                                                                                                        TArg5                                                                                                                                          arg5,
                                                                                                                                        TArg6                                                                                                                                          arg6,
                                                                                                                                        TArg7                                                                                                                                          arg7,
                                                                                                                                        TArg8                                                                                                                                          arg8,
                                                                                                                                        [EnumeratorCancellation] CancellationToken                                                                                                     token
        )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token, self.TransactionIsolationLevel);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);

            bool passed = true;

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) )
            {
                if ( result.TryGetValue(out TResult? value, out Errors? errors) ) { yield return value; }
                else
                {
                    passed = false;
                    yield return errors;
                }
            }

            if ( passed ) { await context.CommitAsync(token); }
            else { await context.RollbackAsync(token); }
        }


        public async ValueTask<ErrorOrResult<TResult>> TryCall<TResult>( Func<DbConnectionContext, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<ErrorOrResult<TResult>> TryCall<TArg1, TResult>( Func<DbConnectionContext, TArg1, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, arg1, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TResult>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, CancellationToken token = default )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, arg1, arg2, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, arg1, arg2, arg3, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, arg1, arg2, arg3, arg4, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, arg1, arg2, arg3, arg4, arg5, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }
        public async ValueTask<ErrorOrResult<TResult>> TryCall<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token, self.TransactionIsolationLevel);

            try
            {
                ErrorOrResult<TResult> result = await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);
                if ( result.HasValue ) { await context.CommitAsync(token); }

                return result;
            }
            catch ( DbSqlException e ) when ( !string.IsNullOrWhiteSpace(e.RollbackID) )
            {
                await context.RollbackAsync(e.RollbackID, token);
                throw;
            }
            catch ( Exception )
            {
                await context.RollbackAsync(token);
                throw;
            }
        }


        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TResult>( Func<DbConnectionContext, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TResult>( Func<DbConnectionContext, TArg1, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TResult>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, arg5, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func,
                                                                                                                              TArg1                                                                                                                                   arg1,
                                                                                                                              TArg2                                                                                                                                   arg2,
                                                                                                                              TArg3                                                                                                                                   arg3,
                                                                                                                              TArg4                                                                                                                                   arg4,
                                                                                                                              TArg5                                                                                                                                   arg5,
                                                                                                                              TArg6                                                                                                                                   arg6,
                                                                                                                              TArg7                                                                                                                                   arg7,
                                                                                                                              [EnumeratorCancellation] CancellationToken                                                                                              token
        )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }
        public async IAsyncEnumerable<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, IAsyncEnumerable<ErrorOrResult<TResult>>> func,
                                                                                                                                     TArg1                                                                                                                                          arg1,
                                                                                                                                     TArg2                                                                                                                                          arg2,
                                                                                                                                     TArg3                                                                                                                                          arg3,
                                                                                                                                     TArg4                                                                                                                                          arg4,
                                                                                                                                     TArg5                                                                                                                                          arg5,
                                                                                                                                     TArg6                                                                                                                                          arg6,
                                                                                                                                     TArg7                                                                                                                                          arg7,
                                                                                                                                     TArg8                                                                                                                                          arg8,
                                                                                                                                     [EnumeratorCancellation] CancellationToken                                                                                                     token
        )
        {
            await using DbConnectionContext          context    = await self.ConnectAsync(token);
            IAsyncEnumerable<ErrorOrResult<TResult>> enumerable = func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);

            await foreach ( ErrorOrResult<TResult> result in enumerable.WithCancellation(token) ) { yield return result; }
        }


        public async ValueTask<ErrorOrResult<TResult>> Call<TResult>( Func<DbConnectionContext, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TResult>( Func<DbConnectionContext, TArg1, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TResult>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, arg5, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);

            return await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);
        }
        public async ValueTask<ErrorOrResult<TResult>> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask<ErrorOrResult<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);
        }


        public async IAsyncEnumerable<TResult> Call<TResult>( Func<DbConnectionContext, CancellationToken, IAsyncEnumerable<TResult>> func, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> Call<TArg1, TResult>( Func<DbConnectionContext, TArg1, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, arg1, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> Call<TArg1, TArg2, TResult>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, arg1, arg2, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> Call<TArg1, TArg2, TArg3, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, arg1, arg2, arg3, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> Call<TArg1, TArg2, TArg3, TArg4, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, arg1, arg2, arg3, arg4, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, arg1, arg2, arg3, arg4, arg5, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }
        public async IAsyncEnumerable<TResult> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, IAsyncEnumerable<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, [EnumeratorCancellation] CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            IAsyncEnumerable<TResult>       result  = func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);

            await foreach ( TResult item in result.WithCancellation(token) ) { yield return item; }
        }


        public async ValueTask Call( Func<DbConnectionContext, CancellationToken, ValueTask> func, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, token);
        }
        public async ValueTask Call<TArg1>( Func<DbConnectionContext, TArg1, CancellationToken, ValueTask> func, TArg1 arg1, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, arg1, token);
        }
        public async ValueTask Call<TArg1, TArg2>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, arg1, arg2, token);
        }
        public async ValueTask Call<TArg1, TArg2, TArg3>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, arg1, arg2, arg3, token);
        }
        public async ValueTask Call<TArg1, TArg2, TArg3, TArg4>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, arg1, arg2, arg3, arg4, token);
        }
        public async ValueTask Call<TArg1, TArg2, TArg3, TArg4, TArg5>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, arg1, arg2, arg3, arg4, arg5, token);
        }
        public async ValueTask Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);
        }
        public async ValueTask Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);
        }
        public async ValueTask Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);
        }


        public async ValueTask<TResult> Call<TResult>( Func<DbConnectionContext, CancellationToken, ValueTask<TResult>> func, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, token);
        }
        public async ValueTask<TResult> Call<TArg1, TResult>( Func<DbConnectionContext, TArg1, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, token);
        }
        public async ValueTask<TResult> Call<TArg1, TArg2, TResult>( Func<DbConnectionContext, TArg1, TArg2, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, token);
        }
        public async ValueTask<TResult> Call<TArg1, TArg2, TArg3, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, token);
        }
        public async ValueTask<TResult> Call<TArg1, TArg2, TArg3, TArg4, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, token);
        }
        public async ValueTask<TResult> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, arg5, token);
        }
        public async ValueTask<TResult> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, arg5, arg6, token);
        }
        public async ValueTask<TResult> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, token);
        }
        public async ValueTask<TResult> Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>( Func<DbConnectionContext, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, CancellationToken, ValueTask<TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, CancellationToken token )
        {
            await using DbConnectionContext context = await self.ConnectAsync(token);
            return await func(context, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, token);
        }
    }
}
