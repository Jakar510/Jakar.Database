namespace Jakar.Database;


public interface IUserStore : IUserLoginStore<UserRecord>, IUserClaimStore<UserRecord>, IUserSecurityStampStore<UserRecord>, IUserTwoFactorStore<UserRecord>, IUserPasswordStore<UserRecord>, IUserEmailStore<UserRecord>, IUserLockoutStore<UserRecord>, IUserAuthenticatorKeyStore<UserRecord>, IUserTwoFactorRecoveryCodeStore<UserRecord>, IUserPhoneNumberStore<UserRecord> { }



[SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class UserStore( Database dbContext ) : IUserStore
{
    public virtual void Dispose() { }


    public virtual async Task<string?>              GetAuthenticatorKeyAsync( UserRecord        user,               CancellationToken   token )                           => await dbContext.GetAuthenticatorKeyAsync(user, token);
    public virtual async Task                       SetAuthenticatorKeyAsync( UserRecord        user,               string              key,    CancellationToken token ) => await dbContext.SetAuthenticatorKeyAsync(user, key, token);
    public virtual async Task                       AddClaimsAsync( UserRecord                  user,               IEnumerable<Claim>  claims, CancellationToken token ) => await dbContext.AddClaimsAsync(user, claims, token);
    public virtual async Task<IList<Claim>>         GetClaimsAsync( UserRecord                  user,               CancellationToken   token )                                                       => await dbContext.GetClaimsAsync(user, ClaimType.All, token);
    public virtual async Task<IList<UserRecord>>    GetUsersForClaimAsync( Claim                claim,              CancellationToken   token )                                                       => await dbContext.GetUsersForClaimAsync(claim, token).ToList(DEFAULT_CAPACITY, token);
    public virtual async Task                       RemoveClaimsAsync( UserRecord               user,               IEnumerable<Claim>  claims, CancellationToken token )                             => await dbContext.RemoveClaimsAsync(user, claims, token);
    public virtual async Task                       ReplaceClaimAsync( UserRecord               user,               Claim               claim,  Claim             newClaim, CancellationToken token ) => await dbContext.ReplaceClaimAsync(user, claim, newClaim, token);
    public virtual async Task<UserRecord?>          FindByEmailAsync( string                    email,              CancellationToken   token )                                    => await dbContext.FindByEmailAsync(email, token);
    public virtual async Task<string?>              GetEmailAsync( UserRecord                   user,               CancellationToken   token )                                    => await dbContext.GetEmailAsync(user, token);
    public virtual async Task<string?>              GetNormalizedEmailAsync( UserRecord         user,               CancellationToken   token )                                    => await dbContext.GetNormalizedEmailAsync(user, token);
    public virtual async Task<bool>                 GetEmailConfirmedAsync( UserRecord          user,               CancellationToken   token )                                    => await dbContext.GetEmailConfirmedAsync(user, token);
    public virtual async Task                       SetEmailAsync( UserRecord                   user,               string?             email,           CancellationToken token ) => await dbContext.SetEmailAsync(user, email, token);
    public virtual async Task                       SetEmailConfirmedAsync( UserRecord          user,               bool                confirmed,       CancellationToken token ) => await dbContext.SetEmailConfirmedAsync(user, confirmed, token);
    public virtual async Task                       SetNormalizedEmailAsync( UserRecord         user,               string?             normalizedEmail, CancellationToken token ) => await dbContext.SetNormalizedEmailAsync(user, normalizedEmail, token);
    public virtual async Task<int>                  GetAccessFailedCountAsync( UserRecord       user,               CancellationToken   token )                                => await dbContext.GetAccessFailedCountAsync(user, token);
    public virtual async Task<bool>                 GetLockoutEnabledAsync( UserRecord          user,               CancellationToken   token )                                => await dbContext.GetLockoutEnabledAsync(user, token);
    public virtual async Task<DateTimeOffset?>      GetLockoutEndDateAsync( UserRecord          user,               CancellationToken   token )                                => await dbContext.GetLockoutEndDateAsync(user, token);
    public virtual async Task<int>                  IncrementAccessFailedCountAsync( UserRecord user,               CancellationToken   token )                                => await dbContext.IncrementAccessFailedCountAsync(user, token);
    public virtual async Task                       ResetAccessFailedCountAsync( UserRecord     user,               CancellationToken   token )                                => await dbContext.ResetAccessFailedCountAsync(user, token);
    public virtual async Task                       SetLockoutEnabledAsync( UserRecord          user,               bool                enabled,     CancellationToken token ) => await dbContext.SetLockoutEnabledAsync(user, enabled, token);
    public virtual async Task                       SetLockoutEndDateAsync( UserRecord          user,               DateTimeOffset?     lockoutEnd,  CancellationToken token ) => await dbContext.SetLockoutEndDateAsync(user, lockoutEnd, token);
    public virtual async Task                       AddLoginAsync( UserRecord                   user,               UserLoginInfo       login,       CancellationToken token ) => await dbContext.AddLoginAsync(user, login, token);
    public virtual async Task<UserRecord?>          FindByLoginAsync( string                    loginProvider,      string              providerKey, CancellationToken token ) => await dbContext.FindByLoginAsync(loginProvider, providerKey, token);
    public virtual async Task<IList<UserLoginInfo>> GetLoginsAsync( UserRecord                  user,               CancellationToken   token )                                                      { return await dbContext.GetLoginsAsync(user, token).Select(static x => x.ToUserLoginInfo()).ToList(DEFAULT_CAPACITY, token); }
    public virtual async Task                       RemoveLoginAsync( UserRecord                user,               string              loginProvider, string providerKey, CancellationToken token ) => await dbContext.RemoveLoginAsync(user, loginProvider, providerKey, token);
    public virtual async Task<IdentityResult>       CreateAsync( UserRecord                     user,               CancellationToken   token )                             => await dbContext.CreateAsync(user, token);
    public virtual async Task<IdentityResult>       DeleteAsync( UserRecord                     user,               CancellationToken   token )                             => await dbContext.DeleteAsync(user, token);
    public virtual async Task<UserRecord?>          FindByIdAsync( string                       userId,             CancellationToken   token )                             => await dbContext.Users.Get(nameof(UserRecord.UserName), userId,             token);
    public virtual async Task<UserRecord?>          FindByNameAsync( string                     normalizedUserName, CancellationToken   token )                             => await dbContext.Users.Get(nameof(UserRecord.FullName), normalizedUserName, token);
    public virtual async Task<string?>              GetNormalizedUserNameAsync( UserRecord      user,               CancellationToken   token )                             => await dbContext.GetNormalizedUserNameAsync(user, token);
    public virtual async Task<string>               GetUserIdAsync( UserRecord                  user,               CancellationToken   token )                             => await dbContext.GetUserIdAsync(user, token);
    public virtual async Task<string?>              GetUserNameAsync( UserRecord                user,               CancellationToken   token )                             => await dbContext.GetUserNameAsync(user, token);
    public virtual async Task                       SetNormalizedUserNameAsync( UserRecord      user,               string?             fullName, CancellationToken token ) => await dbContext.SetNormalizedUserNameAsync(user, fullName, token);
    public virtual async Task                       SetUserNameAsync( UserRecord                user,               string?             userName, CancellationToken token ) => await dbContext.SetUserNameAsync(user, userName, token);
    public virtual async Task<IdentityResult>       UpdateAsync( UserRecord                     user,               CancellationToken   token )                                => await dbContext.UpdateAsync(user, token);
    public virtual async Task<string?>              GetPhoneNumberAsync( UserRecord             user,               CancellationToken   token )                                => await dbContext.GetPhoneNumberAsync(user, token);
    public virtual async Task<bool>                 GetPhoneNumberConfirmedAsync( UserRecord    user,               CancellationToken   token )                                => await dbContext.GetPhoneNumberConfirmedAsync(user, token);
    public virtual async Task                       SetPhoneNumberAsync( UserRecord             user,               string?             phoneNumber, CancellationToken token ) => await dbContext.SetPhoneNumberAsync(user, phoneNumber, token);
    public virtual async Task                       SetPhoneNumberConfirmedAsync( UserRecord    user,               bool                confirmed,   CancellationToken token ) => await dbContext.SetPhoneNumberConfirmedAsync(user, confirmed, token);
    public virtual async Task<string?>              GetSecurityStampAsync( UserRecord           user,               CancellationToken   token )                          => await dbContext.GetSecurityStampAsync(user, token);
    public virtual async Task                       SetSecurityStampAsync( UserRecord           user,               string              stamp, CancellationToken token ) => await dbContext.SetSecurityStampAsync(user, stamp, token);
    public virtual async Task<int>                  CountCodesAsync( UserRecord                 user,               CancellationToken   token )                                  => ( await user.Codes(dbContext, token).ToList(DEFAULT_CAPACITY, token) ).Count;
    public virtual async Task<bool>                 RedeemCodeAsync( UserRecord                 user,               string              code,          CancellationToken token ) => await user.RedeemCode(dbContext, code, token);
    public virtual async Task                       ReplaceCodesAsync( UserRecord               user,               IEnumerable<string> recoveryCodes, CancellationToken token ) => await user.ReplaceCodes(dbContext, recoveryCodes, token);
    public virtual async Task<bool>                 GetTwoFactorEnabledAsync( UserRecord        user,               CancellationToken   token )                                 => await dbContext.GetTwoFactorEnabledAsync(user, token);
    public virtual async Task                       SetTwoFactorEnabledAsync( UserRecord        user,               bool                enabled,      CancellationToken token ) => await dbContext.SetTwoFactorEnabledAsync(user, enabled, token);
    public virtual async Task                       SetPasswordHashAsync( UserRecord            user,               string?             passwordHash, CancellationToken token ) => await dbContext.SetPasswordHashAsync(user, passwordHash, token);
    public virtual async Task<string?>              GetPasswordHashAsync( UserRecord            user,               CancellationToken   token ) => await dbContext.GetPasswordHashAsync(user, token);
    public virtual async Task<bool>                 HasPasswordAsync( UserRecord                user,               CancellationToken   token ) => await dbContext.HasPasswordAsync(user, token);
}
