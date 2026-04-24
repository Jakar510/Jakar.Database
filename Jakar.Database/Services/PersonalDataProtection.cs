namespace Jakar.Database;


public sealed class PersonalDataLookupProtector( IDataProtectionProvider provider ) : ILookupProtector
{
    public string? Protect( string keyId, string? data )   => data is null ? null : provider.CreateProtector(GetPurpose(keyId)).Protect(data);
    public string? Unprotect( string keyId, string? data ) => data is null ? null : provider.CreateProtector(GetPurpose(keyId)).Unprotect(data);


    private static string GetPurpose( string keyId ) => $"Jakar.Database.Identity.PersonalData:{keyId}";
}


public sealed class PersonalDataLookupProtectorKeyRing : ILookupProtectorKeyRing
{
    public string CurrentKeyId => DEFAULT_KEY_ID;
    public string this[ string keyId ] => keyId;


    public IEnumerable<string> GetAllKeyIds()
    {
        yield return DEFAULT_KEY_ID;
    }


    private const string DEFAULT_KEY_ID = "default";
}
