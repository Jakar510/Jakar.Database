// Jakar.Extensions :: Jakar.Database
// 09/29/2023  9:25 PM

namespace Jakar.Database;


[Table(TABLE_NAME)]
public sealed record AddressRecord : OwnedTableRecord<AddressRecord>, IAddress<AddressRecord, Guid>, ITableRecord<AddressRecord>
{
    public const string TABLE_NAME = "addresses";


    public static                                                                                          string               TableName       => TABLE_NAME;
    [ProtectedPersonalData] [Indexed<AddressRecord>(nameof(Address))] [Fixed(4096)] public                 string?              Address         { get => field ??= GetAddress(); init; }
    [ProtectedPersonalData] [Indexed<AddressRecord>(nameof(City))] [Fixed(   512)]  public                 string               City            { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [Indexed<AddressRecord>(nameof(Country))] [Fixed(512)]  public                 string               Country         { get;                           init; } = EMPTY;
    public                                                                                                 bool                 IsPrimary       { get;                           init; }
    [ProtectedPersonalData] [Indexed<AddressRecord>(nameof(Line1))] [Fixed(          512)] public          string               Line1           { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [Indexed<AddressRecord>(nameof(Line2))] [Fixed(          512)] public          string               Line2           { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [Indexed<AddressRecord>(nameof(PostalCode))] [Fixed(     512)] public          string               PostalCode      { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [Indexed<AddressRecord>(nameof(StateOrProvince))] [Fixed(512)] public          string               StateOrProvince { get;                           init; } = EMPTY;
    [ForeignKey<AddressRecord, UserRecord>]                                                public override RecordID<UserRecord> UserID          { get;                           init; }


    public AddressRecord( in RecordID<UserRecord> userID, in RecordID<AddressRecord> id, in DateTimeOffset dateCreated, in DateTimeOffset? lastModified = null, JObject? additionalData = null ) : base(in userID, in id, in dateCreated, in lastModified, additionalData) { }
    public AddressRecord( Match match, RecordID<UserRecord> userID = default ) : this(userID, RecordID<AddressRecord>.New(), DateTimeOffset.UtcNow)
    {
        Line1           = match.Groups["StreetName"].Value;
        Line2           = match.Groups["Apt"].Value;
        City            = match.Groups["City"].Value;
        StateOrProvince = match.Groups["State"].Value;
        Country         = match.Groups["Country"].Value;
        PostalCode      = match.Groups["ZipCode"].Value;
    }
    public AddressRecord( IAddress<Guid> address, RecordID<UserRecord> userID = default ) : this(userID, RecordID<AddressRecord>.Create(address.ID), DateTimeOffset.UtcNow)
    {
        Line1           = address.Line1;
        Line2           = address.Line2;
        City            = address.City;
        StateOrProvince = address.StateOrProvince;
        Country         = address.Country;
        PostalCode      = address.PostalCode;
        Address         = address.Address;
        IsPrimary       = IsPrimary;
    }
    internal AddressRecord( DbDataReader reader ) : base(reader)
    {
        Line1           = reader.GetFieldValue<AddressRecord, string>(nameof(Line1));
        Line2           = reader.GetFieldValue<AddressRecord, string>(nameof(Line2));
        City            = reader.GetFieldValue<AddressRecord, string>(nameof(City));
        StateOrProvince = reader.GetFieldValue<AddressRecord, string>(nameof(StateOrProvince));
        Country         = reader.GetFieldValue<AddressRecord, string>(nameof(Country));
        PostalCode      = reader.GetFieldValue<AddressRecord, string>(nameof(PostalCode));
        Address         = reader.GetFieldValue<AddressRecord, string>(nameof(Address));
        AdditionalData  = reader.GetAdditionalData<AddressRecord>();
        IsPrimary       = reader.GetFieldValue<AddressRecord, bool>(nameof(IsPrimary));
    }


    private string GetAddress() => string.IsNullOrWhiteSpace(Line2)
                                       ? $"{Line1}. {City}, {StateOrProvince}. {Country}. {PostalCode}"
                                       : $"{Line1} {Line2}. {City}, {StateOrProvince}. {Country}. {PostalCode}";
    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;
    protected override async ValueTask Import( NpgsqlBinaryImporter importer, string propertyName, NpgsqlDbType postgresDbType, CancellationToken token )
    {
        switch ( propertyName )
        {
            case nameof(ID):
                await importer.WriteAsync(ID.Value, postgresDbType, token);
                return;

            case nameof(DateCreated):
                await importer.WriteAsync(DateCreated, postgresDbType, token);
                return;

            case nameof(UserID):
                await importer.WriteAsync(UserID.Value, postgresDbType, token);
                return;

            case nameof(Address):
                await importer.WriteAsync(Address, postgresDbType, token);
                return;

            case nameof(City):
                await importer.WriteAsync(City, postgresDbType, token);
                return;

            case nameof(Country):
                await importer.WriteAsync(Country, postgresDbType, token);
                return;

            case nameof(StateOrProvince):
                await importer.WriteAsync(StateOrProvince, postgresDbType, token);
                return;

            case nameof(Line1):
                await importer.WriteAsync(Line1, postgresDbType, token);
                return;

            case nameof(Line2):
                await importer.WriteAsync(Line2, postgresDbType, token);
                return;

            case nameof(LastModified):
                if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, postgresDbType, token); }
                else { await importer.WriteNullAsync(token); }

                return;

            default:
                throw new InvalidOperationException($"Unknown column: {propertyName}");
        }
    }
    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(Line1)].DataColumn]           = Line1;
        row[MetaData[nameof(Line2)].DataColumn]           = Line2;
        row[MetaData[nameof(City)].DataColumn]            = City;
        row[MetaData[nameof(StateOrProvince)].DataColumn] = StateOrProvince;
        row[MetaData[nameof(Country)].DataColumn]         = Country;
        row[MetaData[nameof(PostalCode)].DataColumn]      = PostalCode;
        row[MetaData[nameof(Address)].DataColumn]         = Address;
        row[MetaData[nameof(IsPrimary)].DataColumn]       = IsPrimary;
        return base.Import(row, token);
    }
    [Pure] public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(Line1),           Line1);
        parameters.Add(nameof(Line2),           Line2);
        parameters.Add(nameof(City),            City);
        parameters.Add(nameof(PostalCode),      PostalCode);
        parameters.Add(nameof(StateOrProvince), StateOrProvince);
        parameters.Add(nameof(Country),         Country);
        parameters.Add(nameof(Address),         Address);
        return parameters;
    }


    public static AddressRecord Parse( string s, IFormatProvider? provider ) => Create(Regexes.Address.Match(s));
    public static bool TryParse( [NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out AddressRecord result )
    {
        Match match = Regexes.Address.Match(s ?? EMPTY);

        if ( !match.Success )
        {
            result = null;
            return false;
        }

        result = Create(match);
        return true;
    }


    [Pure] public static AddressRecord Create( DbDataReader   reader )                                                                                                         => new AddressRecord(reader).Validate();
    [Pure] public static AddressRecord Create( Match          match )                                                                                                          => new(match);
    [Pure] public static AddressRecord Create( IAddress<Guid> address )                                                                                                        => new(address);
    public static        AddressRecord Create( string         line1, string line2, string city, string stateOrProvince, string postalCode, string country, Guid id = default ) => Create(line1, line2, city, stateOrProvince, postalCode, country, id, RecordID<UserRecord>.Empty);
    [Pure] public static AddressRecord Create( string line1, string line2, string city, string stateOrProvince, string postalCode, string country, Guid id, RecordID<UserRecord> userID ) =>
        new(userID, RecordID<AddressRecord>.Create(id), DateTimeOffset.UtcNow)
        {
            Line1           = line1,
            Line2           = line2,
            City            = city,
            StateOrProvince = stateOrProvince,
            PostalCode      = postalCode,
            Country         = country
        };


    [Pure] public static async ValueTask<AddressRecord?> TryFromClaims( DbConnectionContext context, Database db, Claim[] claims, ClaimType types, CancellationToken token )
    {
        CommandParameters   parameters = CommandParameters.Create<AddressRecord>();
        ReadOnlySpan<Claim> span       = claims;

        if ( hasFlag(types, ClaimType.StreetAddressLine1) ) { parameters.Add(nameof(Line1), span.Single(static ( ref readonly x ) => x.Type == ClaimType.StreetAddressLine1.ToClaimTypes()).Value); }

        if ( hasFlag(types, ClaimType.StreetAddressLine2) ) { parameters.Add(nameof(Line2), span.Single(static ( ref readonly x ) => x.Type == ClaimType.StreetAddressLine2.ToClaimTypes()).Value); }

        if ( hasFlag(types, ClaimType.StateOrProvince) ) { parameters.Add(nameof(StateOrProvince), span.Single(static ( ref readonly x ) => x.Type == ClaimType.StateOrProvince.ToClaimTypes()).Value); }

        if ( hasFlag(types, ClaimType.Country) ) { parameters.Add(nameof(Country), span.Single(static ( ref readonly x ) => x.Type == ClaimType.Country.ToClaimTypes()).Value); }

        if ( hasFlag(types, ClaimType.PostalCode) ) { parameters.Add(nameof(PostalCode), span.Single(static ( ref readonly x ) => x.Type == ClaimType.PostalCode.ToClaimTypes()).Value); }

        return await db.Addresses.Get(context, parameters, token);


        static bool hasFlag( ClaimType value, ClaimType flag ) => ( value & flag ) != 0;
    }
    [Pure] public static async IAsyncEnumerable<AddressRecord> TryFromClaims( DbConnectionContext context, Database db, Claim claim, [EnumeratorCancellation] CancellationToken token )
    {
        CommandParameters parameters = CommandParameters.Create<AddressRecord>();

        switch ( claim.Type )
        {
            case ClaimTypes.StreetAddress:
                parameters.Add(nameof(Line1), claim.Value);
                break;

            case ClaimTypes.Locality:
                parameters.Add(nameof(Line2), claim.Value);
                break;

            case ClaimTypes.StateOrProvince:
                parameters.Add(nameof(StateOrProvince), claim.Value);
                break;

            case ClaimTypes.Country:
                parameters.Add(nameof(Country), claim.Value);
                break;

            case ClaimTypes.PostalCode:
                parameters.Add(nameof(PostalCode), claim.Value);
                break;
        }

        await foreach ( AddressRecord record in db.Addresses.Where(context, parameters, token) ) { yield return record; }
    }


    [Pure] public IEnumerable<Claim> GetUserClaims( ClaimType types )
    {
        if ( hasFlag(types, ClaimType.StreetAddressLine1) ) { yield return new Claim(ClaimType.StreetAddressLine1.ToClaimTypes(), Line1, ClaimValueTypes.String); }

        if ( hasFlag(types, ClaimType.StreetAddressLine2) ) { yield return new Claim(ClaimType.StreetAddressLine2.ToClaimTypes(), Line2, ClaimValueTypes.String); }

        if ( hasFlag(types, ClaimType.StateOrProvince) ) { yield return new Claim(ClaimType.StateOrProvince.ToClaimTypes(), StateOrProvince, ClaimValueTypes.String); }

        if ( hasFlag(types, ClaimType.Country) ) { yield return new Claim(ClaimType.Country.ToClaimTypes(), Country, ClaimValueTypes.String); }

        if ( hasFlag(types, ClaimType.PostalCode) ) { yield return new Claim(ClaimType.PostalCode.ToClaimTypes(), PostalCode, ClaimValueTypes.String); }

        yield break;


        static bool hasFlag( ClaimType value, ClaimType flag ) => ( value & flag ) != 0;
    }


    public UserAddress ToAddressModel() => UserAddress.Create(this);
    public TAddress ToAddressModel<TAddress>()
        where TAddress : class, IAddress<TAddress, Guid> => TAddress.Create(this);


    [Pure] public AddressRecord WithUserData( IAddress<Guid> value ) =>
        this with
        {
            Line1 = value.Line1,
            Line2 = value.Line2,
            City = value.City,
            StateOrProvince = value.StateOrProvince,
            Country = value.Country,
            PostalCode = value.PostalCode
        };
    public override bool Equals( AddressRecord? other )
    {
        if ( other is null ) { return false; }

        if ( ReferenceEquals(this, other) ) { return true; }

        return base.Equals(other) && Line1 == other.Line1 && Line2 == other.Line2 && City == other.City && PostalCode == other.PostalCode && StateOrProvince == other.StateOrProvince && Country == other.Country && Address == other.Address;
    }
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Line1, Line2, City, PostalCode, StateOrProvince, Country, Address);


    public static bool operator >( AddressRecord  left, AddressRecord right ) => left.CompareTo(right) > 0;
    public static bool operator >=( AddressRecord left, AddressRecord right ) => left.CompareTo(right) >= 0;
    public static bool operator <( AddressRecord  left, AddressRecord right ) => left.CompareTo(right) < 0;
    public static bool operator <=( AddressRecord left, AddressRecord right ) => left.CompareTo(right) <= 0;
}
