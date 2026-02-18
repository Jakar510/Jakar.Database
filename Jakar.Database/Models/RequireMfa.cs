// Jakar.Extensions :: Jakar.Database
// 08/18/2022  10:35 PM

namespace Jakar.Database;


public sealed class RequireMfa : IAuthorizationRequirement
{
    public static readonly RequireMfa Instance = new();
}
