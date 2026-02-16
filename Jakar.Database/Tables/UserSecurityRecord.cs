// Jakar.Database :: Jakar.Database
// 02/15/2026  23:08

namespace Jakar.Database;


/*
[Table(TABLE_NAME)]
public sealed record UserSecurityRecord : LastModifiedRecord<UserSecurityRecord>, IUserSecurity, ITableRecord<UserSecurityRecord>, IUserID
{
    public const                                     string               TABLE_NAME = "user_security";
    public static                                    string               TableName              => TABLE_NAME;
    public                                           UserRights           Rights                 { get; set; } = new();
    [ColumnInfo(SECURITY_STAMP)] public              string               SecurityStamp          { get; set; } = EMPTY;
    public                                           DateTimeOffset?      LockDate               { get; set; }
    public                                           DateTimeOffset?      LockoutEnd             { get; set; }
    [ColumnInfo(ENCRYPTED_MAX_PASSWORD_SIZE)] public string               PasswordHash           { get; set; } = EMPTY;
    public                                           DateTimeOffset?      RefreshTokenExpiryTime { get; set; }
    [ColumnInfo(REFRESH_TOKEN)] public               UInt128              RefreshTokenHash       { get; set; }
    public                                           Guid?                SessionID              { get; set; }
    [ColumnInfo(AUTHENTICATOR_KEY)] public           string               AuthenticatorKey       { get; set; } = EMPTY;
    public                                           int?                 BadLogins              { get; set; }
    public                                           bool                 IsActive               { get; set; }
    public                                           bool                 IsDisabled             { get; set; }
    public                                           bool                 IsEmailConfirmed       { get; set; }
    public                                           bool                 IsLocked               { get; set; }
    public                                           bool                 IsPhoneNumberConfirmed { get; set; }
    public                                           bool                 IsTwoFactorEnabled     { get; set; }
    public                                           DateTimeOffset?      LastBadAttempt         { get; set; }
    public                                           DateTimeOffset?      LastLogin              { get; set; }
    [Key] public required                            RecordID<UserRecord> UserID                 { get; init; }
    Guid IUserID.                                                         UserID                 => UserID.Value;

    /// <summary> A random value that must change whenever a user is persisted to the store </summary>
    [ColumnInfo(CONCURRENCY_STAMP)] public string ConcurrencyStamp { get; set; } = EMPTY;


    public override bool Equals( UserSecurityRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        if ( UserID.Equals(other.UserID) ) { return true; }

        return Nullable.Equals(SessionID, other.SessionID);
    }
    public override int CompareTo( UserSecurityRecord? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        return UserID.CompareTo(other.UserID);
    }
    public override int GetHashCode()                                                     => HashCode.Combine(UserID);
    public static   bool operator >( UserSecurityRecord  left, UserSecurityRecord right ) => left.CompareTo(right) > 0;
    public static   bool operator >=( UserSecurityRecord left, UserSecurityRecord right ) => left.CompareTo(right) >= 0;
    public static   bool operator <( UserSecurityRecord  left, UserSecurityRecord right ) => left.CompareTo(right) < 0;
    public static   bool operator <=( UserSecurityRecord left, UserSecurityRecord right ) => left.CompareTo(right) <= 0;
}
*/
