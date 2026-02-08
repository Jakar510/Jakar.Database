// Jakar.Extensions :: Jakar.Database
// 09/29/2023  9:25 PM

namespace Jakar.Database;


[Table(TABLE_NAME)]
public sealed record AddressRecord : OwnedTableRecord<AddressRecord>, IAddress<AddressRecord, Guid>, ITableRecord<AddressRecord>
{
    public const string TABLE_NAME = "addresses";


    public static                                                                string  TableName       => TABLE_NAME;
    [ProtectedPersonalData] [ColumnMetaData(ColumnOptions.Indexed, 512)]  public string  Line1           { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [ColumnMetaData(ColumnOptions.Indexed, 512)]  public string  Line2           { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [ColumnMetaData(ColumnOptions.Indexed, 512)]  public string  City            { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [ColumnMetaData(ColumnOptions.Indexed, 512)]  public string  StateOrProvince { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [ColumnMetaData(ColumnOptions.Indexed, 512)]  public string  Country         { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [ColumnMetaData(ColumnOptions.Indexed, 512)]  public string  PostalCode      { get;                           init; } = EMPTY;
    [ProtectedPersonalData] [ColumnMetaData(ColumnOptions.Indexed, 4096)] public string? Address         { get => field ??= GetAddress(); init; }
    public                                                                       bool    IsPrimary       { get;                           init; }


    public AddressRecord( in RecordID<UserRecord>? createdBy, in RecordID<AddressRecord> id, in DateTimeOffset dateCreated, in DateTimeOffset? lastModified = null, JObject? additionalData = null ) : base(in createdBy, in id, in dateCreated, in lastModified, additionalData) { }
    public AddressRecord( Match match ) : this(null, RecordID<AddressRecord>.New(), DateTimeOffset.UtcNow)
    {
        Line1           = match.Groups["StreetName"].Value;
        Line2           = match.Groups["Apt"].Value;
        City            = match.Groups["City"].Value;
        StateOrProvince = match.Groups["State"].Value;
        Country         = match.Groups["Country"].Value;
        PostalCode      = match.Groups["ZipCode"].Value;
    }
    public AddressRecord( IAddress<Guid> address ) : this(null, RecordID<AddressRecord>.Create(address.ID), DateTimeOffset.UtcNow)
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
    internal AddressRecord( NpgsqlDataReader reader ) : base(reader)
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
    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        foreach ( ColumnMetaData column in PropertyMetaData.Values.OrderBy(static x => x.Index) )
        {
            switch ( column.PropertyName )
            {
                case nameof(ID):
                    await importer.WriteAsync(ID.Value, column.PostgresDbType, token);
                    break;

                case nameof(DateCreated):
                    await importer.WriteAsync(DateCreated, column.PostgresDbType, token);
                    break;

                case nameof(CreatedBy):
                    await importer.WriteAsync(CreatedBy?.Value, column.PostgresDbType, token);
                    break;

                case nameof(Address):
                    await importer.WriteAsync(Address, column.PostgresDbType, token);
                    break;

                case nameof(City):
                    await importer.WriteAsync(City, column.PostgresDbType, token);
                    break;

                case nameof(Country):
                    await importer.WriteAsync(Country, column.PostgresDbType, token);
                    break;

                case nameof(StateOrProvince):
                    await importer.WriteAsync(StateOrProvince, column.PostgresDbType, token);
                    break;

                case nameof(Line1):
                    await importer.WriteAsync(Line1, column.PostgresDbType, token);
                    break;

                case nameof(Line2):
                    await importer.WriteAsync(Line2, column.PostgresDbType, token);
                    break;

                case nameof(LastModified):
                    if ( LastModified.HasValue ) { await importer.WriteAsync(LastModified.Value, column.PostgresDbType, token); }
                    else { await importer.WriteNullAsync(token); }

                    break;

                default:
                    throw new InvalidOperationException($"Unknown column: {column.PropertyName}");
            }
        }
    }
    [Pure] public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
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


    [Pure] public static AddressRecord   Create( NpgsqlDataReader reader )      => new AddressRecord(reader).Validate();
    public static        MigrationRecord CreateTable( ulong       migrationID ) => MigrationRecord.CreateTable<AddressRecord>(migrationID);
    [Pure] public static AddressRecord   Create( Match            match )       => new(match);
    [Pure] public static AddressRecord   Create( IAddress<Guid>   address )     => new(address);
    [Pure] public static AddressRecord Create( string line1, string line2, string city, string stateOrProvince, string postalCode, string country, Guid id = default ) => new(null, RecordID<AddressRecord>.Create(id), DateTimeOffset.UtcNow)
                                                                                                                                                                          {
                                                                                                                                                                              Line1 =
                                                                                                                                                                                  line1,
                                                                                                                                                                              Line2           = line2,
                                                                                                                                                                              City            = city,
                                                                                                                                                                              StateOrProvince = stateOrProvince,
                                                                                                                                                                              PostalCode      = postalCode,
                                                                                                                                                                              Country         = country
                                                                                                                                                                          };


    [Pure] public static async ValueTask<AddressRecord?> TryFromClaims( NpgsqlConnection connection, NpgsqlTransaction transaction, Database db, Claim[] claims, ClaimType types, CancellationToken token )
    {
        PostgresParameters  parameters = PostgresParameters.Create<AddressRecord>();
        ReadOnlySpan<Claim> span       = claims;

        if ( hasFlag(types, ClaimType.StreetAddressLine1) )
        {
            parameters.Add(nameof(Line1),
                           span.Single(static ( ref readonly Claim x ) => x.Type == ClaimType.StreetAddressLine1.ToClaimTypes())
                               .Value);
        }

        if ( hasFlag(types, ClaimType.StreetAddressLine2) )
        {
            parameters.Add(nameof(Line2),
                           span.Single(static ( ref readonly Claim x ) => x.Type == ClaimType.StreetAddressLine2.ToClaimTypes())
                               .Value);
        }

        if ( hasFlag(types, ClaimType.StateOrProvince) )
        {
            parameters.Add(nameof(StateOrProvince),
                           span.Single(static ( ref readonly Claim x ) => x.Type == ClaimType.StateOrProvince.ToClaimTypes())
                               .Value);
        }

        if ( hasFlag(types, ClaimType.Country) )
        {
            parameters.Add(nameof(Country),
                           span.Single(static ( ref readonly Claim x ) => x.Type == ClaimType.Country.ToClaimTypes())
                               .Value);
        }

        if ( hasFlag(types, ClaimType.PostalCode) )
        {
            parameters.Add(nameof(PostalCode),
                           span.Single(static ( ref readonly Claim x ) => x.Type == ClaimType.PostalCode.ToClaimTypes())
                               .Value);
        }

        return await db.Addresses.Get(connection, transaction, true, parameters, token);


        static bool hasFlag( ClaimType value, ClaimType flag ) => ( value & flag ) != 0;
    }
    [Pure] public static async IAsyncEnumerable<AddressRecord> TryFromClaims( NpgsqlConnection connection, NpgsqlTransaction transaction, Database db, Claim claim, [EnumeratorCancellation] CancellationToken token )
    {
        PostgresParameters parameters = PostgresParameters.Create<AddressRecord>();

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

        await foreach ( AddressRecord record in db.Addresses.Where(connection, transaction, true, parameters, token) ) { yield return record; }
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
