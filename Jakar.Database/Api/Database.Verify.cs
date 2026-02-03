// Jakar.Extensions :: Jakar.Database
// 03/12/2023  1:53 PM

namespace Jakar.Database;


public enum SubscriptionState
{
    NotSet = 0,
    /// <summary> Created but not yet activated </summary>
    Pending,
    /// <summary> Currently valid (often instead of "Ok") </summary>
    Active,
    /// <summary> Past end date </summary>
    Expired,
    /// <summary> User or system canceled before expiry </summary>
    Canceled,
    /// <summary> Currently valid and in trial period </summary>
    Trial
}



[Flags]
public enum SubscriptionStatus
{
    NotSet = 0,
    /// <summary> Indicates that the value or state is unknown or could not be determined. </summary>
    /// <remarks>
    /// Use this value when the actual state is unavailable, indeterminate, or not applicable.
    /// This can be useful as a default or error state in scenarios where a specific value cannot be provided.
    /// This should not be used to represent a valid or meaningful state, but rather to indicate the absence of information, and should override all other flags.
    /// </remarks>
    Unknown = -1,
    /// <summary> Temporarily suspended, resumable </summary>
    Paused = 1 << 1,
    /// <summary> Disabled due to policy, billing, or abuse </summary>
    Suspended = 1 << 2,
    /// <summary> Payment failed, grace period begins </summary>
    PastDue = 1 << 3,
    /// <summary> No valid payment method </summary>
    PaymentRequired = 1 << 4,
    /// <summary> Payment reversed </summary>
    Chargeback = 1 << 5,
    /// <summary> Subscription refunded </summary>
    Refunded = 1 << 6,
    /// <summary> Trial period is active </summary>
    TrialActive = 1 << 7,
    /// <summary> Trial period is expired </summary>
    TrialExpired = 1 << 8,
    /// <summary> After expiration but still usable </summary>
    GracePeriod = 1 << 9,
    /// <summary>  Provider temporarily blocked it </summary>
    OnHold = 1 << 10,

    PaymentIssueMask = PastDue | PaymentRequired | Chargeback,
}



public readonly record struct SubscriptionInfo( SubscriptionState State, SubscriptionStatus Status, DateTimeOffset? Expires );



public abstract partial class Database
{
    public virtual ValueTask<ErrorOrResult<TSelf>> TryGetSubscription<TSelf>( NpgsqlConnection connection, NpgsqlTransaction? transaction, UserRecord record, CancellationToken token = default )
        where TSelf : UserSubscription<TSelf>, ITableRecord<TSelf> => default;

    public virtual ValueTask<ErrorOrResult<SubscriptionInfo>> ValidateSubscription( NpgsqlConnection connection, NpgsqlTransaction? transaction, UserRecord record, CancellationToken token = default ) => new(new SubscriptionInfo(SubscriptionState.NotSet, SubscriptionStatus.NotSet, null));


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

            ErrorOrResult<SubscriptionInfo> subscription = await ValidateSubscription(connection, transaction, record, token);

            if ( subscription.TryGetValue(out Errors? errors) )
            {
                record = record.SetActive();
                return errors;
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


    public ValueTask<ErrorOrResult<UserRecord>> VerifyLogin( string jsonToken, ClaimType types = DEFAULT_CLAIM_TYPES, CancellationToken token = default ) => this.TryCall(VerifyLogin, jsonToken, types, token);
    protected virtual async ValueTask<ErrorOrResult<UserRecord>> VerifyLogin( NpgsqlConnection connection, NpgsqlTransaction transaction, string jsonToken, ClaimType types = DEFAULT_CLAIM_TYPES, CancellationToken token = default )
    {
        JwtSecurityTokenHandler   handler              = new();
        TokenValidationParameters validationParameters = await GetTokenValidationParameters(token);
        TokenValidationResult     validationResult     = await handler.ValidateTokenAsync(jsonToken, validationParameters);

        if ( validationResult.Exception is not null )
        {
            Exception e = validationResult.Exception;

            string typeName = e.GetType()
                               .Name;

            return Error.Create(Status.InternalServerError, e.Message, e.Source, e.MethodName(), type: typeName);
        }

        Claim[]                   claims = validationResult.ClaimsIdentity.Claims.ToArray();
        ErrorOrResult<UserRecord> result = await UserRecord.TryFromClaims(connection, transaction, this, claims.AsValueEnumerable(), types | DEFAULT_CLAIM_TYPES, token);
        if ( !result.TryGetValue(out UserRecord? record, out Errors? errors) ) { return errors; }

        record.LastLogin = DateTimeOffset.UtcNow;
        await Users.Update(connection, transaction, record, token);
        return record;
    }


    public virtual async ValueTask<Permissions<TEnum>> GetRights<TEnum>( NpgsqlConnection connection, NpgsqlTransaction transaction, RecordID<UserRecord> userID, CancellationToken token )
        where TEnum : unmanaged, Enum
    {
        string rights = nameof(UserRecord.Rights)
           .SqlColumnName();

        string id = nameof(UserRecord.ID)
           .SqlColumnName();

        string sql = $"""
                      SELECT u.{rights}
                      FROM {UserRecord.TABLE_NAME} u
                      WHERE u.{id} = '{userID}'

                      UNION ALL

                      SELECT g.{rights}
                      FROM {UserGroupRecord.TABLE_NAME} ug
                      INNER JOIN {GroupRecord.TABLE_NAME} g
                      ON ug.{nameof(UserGroupRecord.ValueID)} = g.{id}
                      WHERE ug.{nameof(UserGroupRecord.KeyID)} = '{userID}'

                      UNION ALL

                      SELECT r.{rights}
                      FROM {UserRoleRecord.TABLE_NAME} ur
                      INNER JOIN {RoleRecord.TABLE_NAME} r
                      ON ur.{nameof(UserRoleRecord.ValueID)} = r.{id}
                      WHERE ur.{nameof(UserRoleRecord.KeyID)} = '{userID}'
                      """;

        await using NpgsqlCommand    command = new(sql, connection, transaction);
        await using NpgsqlDataReader reader  = await command.ExecuteReaderAsync(token);
        Permissions<TEnum>           result  = Permissions<TEnum>.Default;

        while ( await reader.ReadAsync(token) )
        {
            Permissions<TEnum> other = Permissions<TEnum>.Create(reader.GetFieldValue<string>(0));
            result |= other;
        }

        return result;
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
}
