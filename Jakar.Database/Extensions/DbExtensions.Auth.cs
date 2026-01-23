namespace Jakar.Database;


// TODO: asp.net authorization dapper
public static partial class DbExtensions
{
    extension( ClaimsPrincipal self )
    {
        public bool TryParse( out RecordID<UserRecord> userID )
        {
            Claim? claim = self.Claims.FirstOrDefault(Claims.IsUserID);

            if ( Guid.TryParse(claim?.Value, out Guid id) )
            {
                userID = RecordID<UserRecord>.Create(id);
                return true;
            }

            userID = RecordID<UserRecord>.Empty;
            return false;
        }
        public bool TryParse( [NotNullWhen(true)] out RecordID<UserRecord>? userID )
        {
            Claim? claim = self.Claims.FirstOrDefault(Claims.IsUserID);

            if ( Guid.TryParse(claim?.Value, out Guid id) )
            {
                userID = RecordID<UserRecord>.Create(id);
                return true;
            }

            userID = null;
            return false;
        }
        public bool TryParse( out RecordID<UserRecord> userID, out string userName ) => self.Claims.ToArray()
                                                                                            .TryParse(out userID, out userName);
        public bool TryParse( [NotNullWhen(true)] out RecordID<UserRecord>? userID, out string userName ) => self.Claims.ToArray()
                                                                                                                 .TryParse(out userID, out userName);
        public bool TryParse( out RecordID<UserRecord> userID, out string userName, out Claim[] roles, out Claim[] groups ) => self.Claims.ToArray()
                                                                                                                                   .TryParse(out userID, out userName, out roles, out groups);
        public bool TryParse( [NotNullWhen(true)] out RecordID<UserRecord>? userID, out string userName, out Claim[] roles, out Claim[] groups ) => self.Claims.ToArray()
                                                                                                                                                        .TryParse(out userID, out userName, out roles, out groups);
    }



    extension( ReadOnlySpan<Claim> self )
    {
        public bool TryParse( out RecordID<UserRecord> userID, out string userName )
        {
            userName = self.FirstOrDefault(static ( ref readonly Claim x ) => x.IsUserName())
                            ?.Value ??
                       EMPTY;

            if ( Guid.TryParse(self.FirstOrDefault(static ( ref readonly Claim x ) => x.IsUserID())
                                    ?.Value,
                               out Guid id) )
            {
                userID = RecordID<UserRecord>.Create(id);
                return true;
            }

            userID = RecordID<UserRecord>.Empty;
            return false;
        }
        public bool TryParse( [NotNullWhen(true)] out RecordID<UserRecord>? userID, out string userName )
        {
            userName = self.FirstOrDefault(static ( ref readonly Claim x ) => x.IsUserName())
                            ?.Value ??
                       EMPTY;

            if ( Guid.TryParse(self.FirstOrDefault(static ( ref readonly Claim x ) => x.IsUserID())
                                    ?.Value,
                               out Guid id) )
            {
                userID = RecordID<UserRecord>.Create(id);
                return true;
            }

            userID = null;
            return false;
        }
        public bool TryParse( out RecordID<UserRecord> userID, out string userName, out Claim[] roles, out Claim[] groups )
        {
            roles = self.AsValueEnumerable()
                          .Where(Claims.IsRole)
                          .ToArray();

            groups = self.AsValueEnumerable()
                           .Where(Claims.IsGroup)
                           .ToArray();

            userName = self.FirstOrDefault(Claims.IsUserName)
                            ?.Value ??
                       EMPTY;

            if ( Guid.TryParse(self.FirstOrDefault(Claims.IsUserID)
                                    ?.Value,
                               out Guid id) )
            {
                userID = RecordID<UserRecord>.Create(id);
                return true;
            }

            userID = RecordID<UserRecord>.Empty;
            return false;
        }
        public bool TryParse( [NotNullWhen(true)] out RecordID<UserRecord>? userID, out string userName, out Claim[] roles, out Claim[] groups )
        {
            roles = self.AsValueEnumerable()
                          .Where(Claims.IsRole)
                          .ToArray();

            groups = self.AsValueEnumerable()
                           .Where(Claims.IsGroup)
                           .ToArray();

            userName = self.FirstOrDefault(Claims.IsUserName)
                            ?.Value ??
                       EMPTY;

            if ( Guid.TryParse(self.FirstOrDefault(Claims.IsUserID)
                                    ?.Value,
                               out Guid id) )
            {
                userID = RecordID<UserRecord>.Create(id);
                return true;
            }

            userID = null;
            return false;
        }
    }



    public static IServiceCollection AddAuth<TValue>( this IServiceCollection services )
        where TValue : class, IAuthenticationService
    {
        services.AddHttpContextAccessor();
        services.AddTransient<IAuthenticationService, TValue>();
        return services;
    }
}
