namespace Jakar.Database;


public static class JwtParser
{
    private static byte[] ParseBase64WithoutPadding( string base64 )
    {
        switch ( base64.Length % 4 )
        {
            case 2:
                base64 += "==";
                break;

            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }
    public static List<Claim> ParseClaimsFromJwt( string jwt )
    {
        List<Claim> claims  = [];
        string      payload = jwt.Split('.')[1];

        string json = ParseBase64WithoutPadding(payload).ConvertToString(Encoding.Default);

        JObjectNode keyValuePairs = JsonAdditionalData.Parse(json) ?? [];

        ExtractRolesFromJwt(claims, keyValuePairs);

        foreach ( ( string key, JNode? value ) in keyValuePairs ) { claims.Add(new Claim(key, GetClaimValue(value))); }

        return claims;
    }
    private static void ExtractRolesFromJwt( List<Claim> claims, JObjectNode keyValuePairs )
    {
        if ( !keyValuePairs.Remove(ClaimTypes.Role, out JNode? roles) || roles is null ) { return; }

        if ( roles is JArrayNode array )
        {
            foreach ( JNode? role in array ) { claims.Add(new Claim(ClaimTypes.Role, GetClaimValue(role))); }
        }
        else { claims.Add(new Claim(ClaimTypes.Role, GetClaimValue(roles))); }
    }
    private static string GetClaimValue( JNode? value ) => value switch
                                                           {
                                                               null                                       => EMPTY,
                                                               JValueNode { Kind: JNodeKind.String } text => text.GetString(),
                                                               _                                          => value.ToJson()
                                                           };
}
