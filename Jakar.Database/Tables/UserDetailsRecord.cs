// Jakar.Database :: Jakar.Database
// 02/15/2026  23:08

namespace Jakar.Database;


/*
[Table(TABLE_NAME)]
public sealed record UserDetailsRecord : LastModifiedRecord<UserDetailsRecord>, ITableRecord<UserDetailsRecord>, IUserModel
{
    public const                                                                                                  string               TABLE_NAME = "user_details";
    public static                                                                                                 string               TableName   => TABLE_NAME;
    [Fixed( COMPANY)] [ProtectedPersonalData]                            public          string               Company     { get; set; } = EMPTY;
    [ColumnInfo(DEPARTMENT)]                                                                      public          string               Department  { get; set; } = EMPTY;
    [ColumnInfo(DESCRIPTION)]                                                                     public          string               Description { get; set; } = EMPTY;
    [ColumnInfo(ColumnOptions.Indexed | ColumnOptions.Fixed, EMAIL)] [ProtectedPersonalData]      public          string               Email       { get; set; } = EMPTY;
    [ColumnInfo(PHONE_EXT)] [ProtectedPersonalData]                                               public          string               Ext         { get; set; } = EMPTY;
    [ColumnInfo(ColumnOptions.Indexed | ColumnOptions.Fixed, FIRST_NAME)] [ProtectedPersonalData] public          string               FirstName   { get; set; } = EMPTY;
    [ColumnInfo(ColumnOptions.Indexed | ColumnOptions.Fixed, FULL_NAME)] [ProtectedPersonalData]  public          string               FullName    { get; set; } = EMPTY;
    [ColumnInfo(ColumnOptions.Indexed | ColumnOptions.Fixed, GENDER)] [ProtectedPersonalData]     public          string               Gender      { get; set; } = EMPTY;
    [ColumnInfo(ColumnOptions.Indexed | ColumnOptions.Fixed, LAST_NAME)] [ProtectedPersonalData]  public          string               LastName    { get; set; } = EMPTY;
    [ColumnInfo(ColumnOptions.Indexed | ColumnOptions.Fixed, PHONE)] [ProtectedPersonalData]      public          string               PhoneNumber { get; set; } = EMPTY;
    [ColumnInfo(TITLE)]                                                                           public          string               Title       { get; set; } = EMPTY;
    [Key]                                                                                         public required RecordID<UserRecord> UserID      { get; init; }
    [ColumnInfo(WEBSITE)] [ProtectedPersonalData]                                                 public          string               Website     { get; set; } = EMPTY;
    Guid IUserID.                                                                                                                      UserID      => UserID.Value;


    public override bool Equals( UserDetailsRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        if ( UserID.Equals(other.UserID) ) { return true; }

        return string.Equals(FullName, other.FullName, StringComparison.InvariantCulture);
    }
    public override int CompareTo( UserDetailsRecord? other )
    {
        if ( other is null ) { return 1; }

        if ( ReferenceEquals(this, other) ) { return 0; }

        int fullNameComparison = string.Compare(FullName, other.FullName, StringComparison.InvariantCulture);
        if ( fullNameComparison != 0 ) { return fullNameComparison; }

        int lastNameComparison = string.Compare(LastName, other.LastName, StringComparison.InvariantCulture);
        if ( lastNameComparison != 0 ) { return lastNameComparison; }

        int firstNameComparison = string.Compare(FirstName, other.FirstName, StringComparison.InvariantCulture);
        if ( firstNameComparison != 0 ) { return firstNameComparison; }

        return UserID.CompareTo(other.UserID);
    }
    public override int GetHashCode()                                                   => HashCode.Combine(UserID);
    public static   bool operator >( UserDetailsRecord  left, UserDetailsRecord right ) => left.CompareTo(right) > 0;
    public static   bool operator >=( UserDetailsRecord left, UserDetailsRecord right ) => left.CompareTo(right) >= 0;
    public static   bool operator <( UserDetailsRecord left, UserDetailsRecord right ) => left.CompareTo(right) < 0;
    public static   bool operator <=( UserDetailsRecord left, UserDetailsRecord right ) => left.CompareTo(right) <= 0;
}
*/
