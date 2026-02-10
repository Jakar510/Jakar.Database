// Jakar.Database :: Jakar.Database
// 02/01/2026  20:25

using System;
using Jakar.Extensions;
using Microsoft.AspNetCore.Identity;



namespace Jakar.Database;


public interface IUserSecurity<out TSelf>
    where TSelf : IUserSecurity<TSelf>
{
    [StringLength(AUTHENTICATOR_KEY)] public string AuthenticatorKey { get; set; }
    public                                   int?   BadLogins        { get; set; }


    /// <summary> A random value that must change whenever a user is persisted to the store </summary>
    [StringLength(CONCURRENCY_STAMP)] public string ConcurrencyStamp { get; set; }

    public                                             bool            IsActive               { get; set; }
    public                                             bool            IsDisabled             { get; set; }
    public                                             bool            IsEmailConfirmed       { get; set; }
    public                                             bool            IsLocked               { get; set; }
    public                                             bool            IsPhoneNumberConfirmed { get; set; }
    public                                             bool            IsTwoFactorEnabled     { get; set; }
    public                                             DateTimeOffset? LastBadAttempt         { get; set; }
    public                                             DateTimeOffset? LastLogin              { get; set; }
    public                                             DateTimeOffset? LockDate               { get; set; }
    public                                             DateTimeOffset? LockoutEnd             { get; set; }
    [StringLength(ENCRYPTED_MAX_PASSWORD_SIZE)] public string          PasswordHash           { get; set; }
    public                                             UInt128         RefreshTokenHash       { get; set; }
    public                                             DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    [StringLength(SECURITY_STAMP)] public              string          SecurityStamp          { get; set; }
    public                                             Guid?           SessionID              { get; set; }
}



public static class UserSecurities
{
    public static readonly TimeSpan AccessTokenExpireTime  = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan LockoutTime            = TimeSpan.FromHours(6);
    public static readonly TimeSpan RefreshTokenExpireTime = TimeSpan.FromDays(90);



    extension<TSelf>( TSelf self )
        where TSelf : TableRecord<TSelf>, IUserSecurity<TSelf>, ITableRecord<TSelf>
    {
        public bool HasPassword() => !string.IsNullOrWhiteSpace(self.PasswordHash);


        public TSelf WithPassword( string password )
        {
            if ( password.Length > MAX_PASSWORD_SIZE ) { throw new ArgumentException($"Password Must be less than {MAX_PASSWORD_SIZE} chars", nameof(password)); }

            self.PasswordHash = Database.DataProtector.Encrypt(password);
            return self;
        }
        public TSelf WithPassword( in string password, scoped in Requirements requirements ) => self.WithPassword(password, requirements, out _);
        public TSelf WithPassword( in string password, scoped in Requirements requirements, out PasswordValidator.Results results )
        {
            if ( requirements.MaxLength > MAX_PASSWORD_SIZE ) { throw new ArgumentException($"Password Must be less than {MAX_PASSWORD_SIZE} chars", nameof(password)); }

            PasswordValidator validator = new(requirements);
            if ( !validator.Validate(password, out results) ) { return self; }

            return self.WithPassword(password);
        }


        public TSelf MarkBadLogin() => self.MarkBadLogin(LockoutTime);
        public TSelf MarkBadLogin( scoped in TimeSpan lockoutTime, in int badLoginDisableThreshold = DEFAULT_BAD_LOGIN_DISABLE_THRESHOLD )
        {
            int badLogins = self.BadLogins ?? 0;
            badLogins++;
            bool           isDisabled = badLogins > badLoginDisableThreshold;
            bool           isLocked   = isDisabled || !self.IsActive;
            DateTimeOffset now        = DateTimeOffset.UtcNow;

            self.BadLogins      = badLogins;
            self.IsDisabled     = isDisabled;
            self.LastBadAttempt = now;
            self.IsLocked       = isLocked;

            self.LockDate = isLocked
                                ? now
                                : null;

            self.LockoutEnd = isLocked
                                  ? now + lockoutTime
                                  : null;

            return self.Modified();
        }


        public TSelf SetActive()
        {
            self.LastLogin = DateTimeOffset.UtcNow;
            return self.Modified();
        }
        public TSelf SetActive( bool isActive )
        {
            self.IsActive = isActive;
            return self.SetActive();
        }


        public TSelf Disable()
        {
            self.IsActive   = false;
            self.IsDisabled = true;
            return self.Modified();
        }
        public TSelf Enable()
        {
            self.IsDisabled = false;
            self.IsActive   = true;
            return self.Modified();
        }
        public TSelf Reset()
        {
            self.IsLocked   = false;
            self.LockDate   = null;
            self.LockoutEnd = null;
            self.BadLogins  = 0;
            self.IsDisabled = false;
            self.IsActive   = true;
            return self.Modified();
        }


        public TSelf Lock() => self.Lock(LockoutTime);
        public TSelf Lock( scoped in TimeSpan lockoutTime )
        {
            DateTimeOffset lockDate = DateTimeOffset.UtcNow;

            self.IsLocked   = true;
            self.LockDate   = lockDate;
            self.LockoutEnd = lockDate + lockoutTime;
            return self.Modified();
        }
        public TSelf Unlock()
        {
            self.IsLocked   = false;
            self.LockDate   = null;
            self.LockoutEnd = null;
            return self.Modified();
        }


        public TSelf ClearRefreshToken( string? securityStamp = null ) => self.WithRefreshToken(EMPTY, null, securityStamp);

        public TSelf WithRefreshToken( SessionToken token, in TimeSpan? span = null, in string? securityStamp = null ) => self.WithRefreshToken(token.RefreshToken, span, in securityStamp);
        public TSelf WithRefreshToken( in string? refreshToken, in TimeSpan? span = null, in string? securityStamp = null )
        {
            DateTimeOffset expires = DateTimeOffset.UtcNow + ( span ?? RefreshTokenExpireTime );
            return self.WithRefreshToken(in refreshToken, in expires, securityStamp);
        }

        public TSelf WithRefreshToken( SessionToken token, in DateTimeOffset expires, in string? securityStamp = null ) => self.WithRefreshToken(token.RefreshToken, in expires, in securityStamp);
        public TSelf WithRefreshToken( in string? refreshToken, in DateTimeOffset expires, in string? securityStamp = null )
        {
            if ( string.IsNullOrEmpty(refreshToken) )
            {
                self.RefreshTokenHash       = UInt128.Zero;
                self.RefreshTokenExpiryTime = null;
                self.SecurityStamp          = securityStamp ?? Randoms.RandomString(30);
                return self.Modified();
            }

            self.SecurityStamp          = securityStamp ?? refreshToken.Hash_SHA256();
            self.RefreshTokenHash       = refreshToken.Hash128();
            self.RefreshTokenExpiryTime = expires;
            return self.Modified();
        }
    }
}
