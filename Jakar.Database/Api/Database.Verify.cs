// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:53 PM

namespace Jakar.Database;


public enum SubscriptionStatus
{
    None,
    Invalid,
    Expired,
    Ok
}



public abstract partial class Database
{
    public virtual ValueTask<DateTimeOffset?> GetSubscriptionExpiration( NpgsqlConnection connection, NpgsqlTransaction? transaction, UserRecord record, CancellationToken token = default ) => new(record.SubscriptionExpires);
    public virtual ValueTask<ErrorOrResult<TSelf>> TryGetSubscription<TSelf>( NpgsqlConnection connection, NpgsqlTransaction? transaction, UserRecord record, CancellationToken token = default )
        where TSelf : UserSubscription<TSelf>, ITableRecord<TSelf> => default;


    /// <summary> </summary>
    /// <returns> <see langword="true"/> is Subscription is valid; otherwise <see langword="false"/> </returns>
    [SuppressMessage("ReSharper", "UnusedParameter.Global")] public virtual ValueTask<ErrorOrResult<SubscriptionStatus>> ValidateSubscription( NpgsqlConnection connection, NpgsqlTransaction? transaction, UserRecord record, CancellationToken token = default ) => new(SubscriptionStatus.Ok);


    protected virtual async ValueTask<ErrorOrResult<UserRecord>> VerifyLogin( NpgsqlConnection connection, NpgsqlTransaction transaction, ILoginRequest request, CancellationToken token = default )
    {
        UserRecord? record = await Users.Get(connection, transaction, true, UserRecord.GetDynamicParameters(request), token);
        if ( record is null ) { return Error.NotFound(); }

        try
        {
            if ( !VerifyPassword(ref record, request) )
            {
                record = record.MarkBadLogin();
                return Error.Unauthorized(request.UserLogin);
            }

            if ( !record.IsActive )
            {
                record = record.MarkBadLogin();
                return Error.Disabled();
            }

            if ( record.IsDisabled )
            {
                record = record.MarkBadLogin();
                return Error.Disabled();
            }

            if ( record.IsLocked )
            {
                record = record.MarkBadLogin();
                return Error.Locked();
            }

            ErrorOrResult<SubscriptionStatus> status = await ValidateSubscription(connection, transaction, record, token);

            if ( status.HasErrors )
            {
                record = record.MarkBadLogin();
                return status.Error;
            }

            return record;
        }
        finally
        {
            record = record.SetActive(true);
            await Users.Update(connection, transaction, record, token);
        }
    }


    public virtual async ValueTask<ErrorOrResult<TValue>> Verify<TValue>( NpgsqlConnection connection, NpgsqlTransaction transaction, ILoginRequest request, Func<NpgsqlConnection, NpgsqlTransaction, UserRecord, ErrorOrResult<TValue>> func, CancellationToken token = default )
    {
        ErrorOrResult<UserRecord> loginResult = await VerifyLogin(connection, transaction, request, token);

        return loginResult.TryGetValue(out UserRecord? record, out Errors? errors)
                   ? func(connection, transaction, record)
                   : errors;
    }

    public virtual async ValueTask<ErrorOrResult<TValue>> Verify<TValue>( NpgsqlConnection connection, NpgsqlTransaction transaction, ILoginRequest request, Func<NpgsqlConnection, NpgsqlTransaction, UserRecord, CancellationToken, ValueTask<ErrorOrResult<TValue>>> func, CancellationToken token = default )
    {
        ErrorOrResult<UserRecord> loginResult = await VerifyLogin(connection, transaction, request, token);

        return loginResult.TryGetValue(out UserRecord? record, out Errors? errors)
                   ? await func(connection, transaction, record, token)
                   : errors;
    }

    public virtual async ValueTask<ErrorOrResult<TValue>> Verify<TValue>( NpgsqlConnection connection, NpgsqlTransaction transaction, ILoginRequest request, Func<NpgsqlConnection, NpgsqlTransaction, UserRecord, CancellationToken, Task<ErrorOrResult<TValue>>> func, CancellationToken token = default )
    {
        ErrorOrResult<UserRecord> loginResult = await VerifyLogin(connection, transaction, request, token);

        return loginResult.TryGetValue(out UserRecord? record, out Errors? errors)
                   ? await func(connection, transaction, record, token)
                   : errors;
    }

    public virtual async ValueTask<ErrorOrResult<SessionToken>> Verify( NpgsqlConnection connection, NpgsqlTransaction transaction, ILoginRequest request, ClaimType types, CancellationToken token = default )
    {
        ErrorOrResult<UserRecord> loginResult = await VerifyLogin(connection, transaction, request, token);

        return loginResult.TryGetValue(out UserRecord? record, out Errors? errors)
                   ? await GetToken(connection, transaction, record, types, token)
                   : errors;
    }


    public virtual async ValueTask<ErrorOrResult<SessionToken>> Register<TUser>( NpgsqlConnection connection, NpgsqlTransaction transaction, ILoginRequest<TUser> request, CancellationToken token = default )
        where TUser : class, IUserData<Guid>
    {
        UserRecord? record = await Users.Get(connection, transaction, true, UserRecord.GetDynamicParameters(request), token);
        if ( record is not null ) { return Error.Conflict($"{nameof(UserRecord.UserName)} is already taken. Chose another {nameof(request.UserLogin)}"); }

        record = CreateNewUser(request);
        record = await Users.Insert(connection, transaction, record, token);
        return await GetToken(connection, transaction, record, DEFAULT_CLAIM_TYPES, token);
    }
    protected virtual UserRecord CreateNewUser<TUser>( ILoginRequest<TUser> request, UserRecord? caller = null )
        where TUser : class, IUserData<Guid> => UserRecord.Create(request, request.Data.Rights, caller);


    public ValueTask<ErrorOrResult<SessionToken>> Register<TUser>( ILoginRequest<TUser> request, CancellationToken token = default )
        where TUser : class, IUserData<Guid> => this.TryCall(Register, request, token);
    public ValueTask<ErrorOrResult<TValue>> Verify<TValue>( ILoginRequest request, Func<NpgsqlConnection, NpgsqlTransaction, UserRecord, ErrorOrResult<TValue>>                               func, CancellationToken token = default ) => this.TryCall(Verify, request, func, token);
    public ValueTask<ErrorOrResult<TValue>> Verify<TValue>( ILoginRequest request, Func<NpgsqlConnection, NpgsqlTransaction, UserRecord, CancellationToken, ValueTask<ErrorOrResult<TValue>>> func, CancellationToken token = default ) => this.TryCall(Verify, request, func, token);
    public ValueTask<ErrorOrResult<TValue>> Verify<TValue>( ILoginRequest request, Func<NpgsqlConnection, NpgsqlTransaction, UserRecord, CancellationToken, Task<ErrorOrResult<TValue>>>      func, CancellationToken token = default ) => this.TryCall(Verify, request, func, token);


    public async ValueTask<Permissions<TEnum>> GetRights<TEnum>( NpgsqlConnection connection, NpgsqlTransaction transaction, UserRecord user, CancellationToken token )
        where TEnum : unmanaged, Enum
    {
        RecordID<UserRecord> userID = user.ID;
        Permissions<TEnum>   result = user.Rights;

        string rights = nameof(UserRecord.Rights)
           .SqlColumnName();

        string id = nameof(UserRecord.ID)
           .SqlColumnName();

        string sql = $"""
                      SELECT {rights} FROM {UserRecord.TABLE_NAME}
                      INNER JOIN {UserGroupRecord.TABLE_NAME} ON {UserRecord.TABLE_NAME}.{id} = {UserGroupRecord.TABLE_NAME}.;
                      INNER JOIN {GroupRecord.TABLE_NAME} ON {UserRecord.TABLE_NAME}.{id} = {GroupRecord.TABLE_NAME}.;
                      WHERE {id} = {userID}
                      """;

        // await foreach ( GroupRecord group in UserGroupRecord.Where(connection, transaction, Groups, userID, token) ) { }
        // await foreach ( RoleRecord role in UserRoleRecord.Where(connection, transaction, Roles, userID, token) ) { }

        // await foreach ( GroupRecord record in user.GetGroups(connection, transaction, this, token) ) { }
        // await foreach ( RoleRecord record in user.GetRoles(connection, transaction, this, token) ) { }

        return result;
    }
}
