// ToothFairyDispatch :: ToothFairyDispatch.Cloud
// 10/10/2022  1:47 PM

using MailKit.Security;



namespace Jakar.Database;


[Serializable]
public sealed class EmailSettings : BaseClass<EmailSettings>, ILoginRequest, IJsonModel<EmailSettings>
{
    public bool                IsValid      => !string.IsNullOrWhiteSpace(UserLogin) && !string.IsNullOrWhiteSpace(UserPassword) && !string.IsNullOrWhiteSpace(Site) && Port.IsValidPort();
    public SecureSocketOptions Options      { get; init; } = SecureSocketOptions.Auto;
    public string              UserPassword { get; init; } = EMPTY;
    public int                 Port         { get; init; }
    public string              Site         { get; init; } = EMPTY;
    public string              UserLogin    { get; init; } = EMPTY;
    public AppVersion          Version      { get; init; } = AppVersion.Default;


    public EmailSettings() { }
    public static EmailSettings Create( IConfiguration configuration ) => configuration.GetSection(nameof(EmailSettings))
                                                                                       .Get<EmailSettings>() ??
                                                                          throw new InvalidOperationException($"Section '{nameof(EmailSettings)}' is invalid");
    public MailboxAddress    Address()                                 => MailboxAddress.Parse(UserLogin);
    public NetworkCredential GetCredential( Uri uri, string authType ) => new(UserLogin, UserPassword, Site);


    public override bool Equals( EmailSettings? other ) => ReferenceEquals(this, other) || ( other is not null && string.Equals(UserLogin, other.UserLogin, StringComparison.InvariantCulture) && string.Equals(UserPassword, other.UserPassword, StringComparison.InvariantCulture) && string.Equals(Site, other.Site, StringComparison.InvariantCulture) && Port == other.Port );
    public override int CompareTo( EmailSettings? other )
    {
        if ( ReferenceEquals(this, other) ) { return 0; }

        if ( other is null ) { return 1; }

        int siteComparison = string.Compare(Site, other.Site, StringComparison.InvariantCultureIgnoreCase);
        if ( siteComparison != 0 ) { return siteComparison; }

        int userNameComparison = string.Compare(UserLogin, other.UserLogin, StringComparison.InvariantCultureIgnoreCase);
        if ( userNameComparison != 0 ) { return userNameComparison; }

        int portComparison = Port.CompareTo(other.Port);
        if ( portComparison != 0 ) { return portComparison; }

        int optionsComparison = Options.CompareTo(other.Options);
        if ( optionsComparison != 0 ) { return optionsComparison; }

        return string.Compare(UserPassword, other.UserPassword, StringComparison.InvariantCultureIgnoreCase);
    }
    public override int  GetHashCode()           => HashCode.Combine(UserLogin, UserPassword, Site, Port, Options);
    public override bool Equals( object? other ) => base.Equals(other);


    public static bool operator ==( EmailSettings? left, EmailSettings? right ) => EqualityComparer<EmailSettings>.Default.Equals(left, right);
    public static bool operator !=( EmailSettings? left, EmailSettings? right ) => !EqualityComparer<EmailSettings>.Default.Equals(left, right);
    public static bool operator >( EmailSettings   left, EmailSettings  right ) => Comparer<EmailSettings>.Default.Compare(left, right) > 0;
    public static bool operator >=( EmailSettings  left, EmailSettings  right ) => Comparer<EmailSettings>.Default.Compare(left, right) >= 0;
    public static bool operator <( EmailSettings   left, EmailSettings  right ) => Comparer<EmailSettings>.Default.Compare(left, right) < 0;
    public static bool operator <=( EmailSettings  left, EmailSettings  right ) => Comparer<EmailSettings>.Default.Compare(left, right) <= 0;
}
