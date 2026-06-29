// Jakar.Database ::  Jakar.Database 
// 04/12/2023  11:34 AM

namespace Jakar.Database;


public class RoleValidator : RoleValidator<RoleRecord>
{
    public override Task<IdentityResult> ValidateAsync( RoleManager<RoleRecord> manager, RoleRecord role )
    {
        List<IdentityError> errors = [];

        if ( string.IsNullOrWhiteSpace(role.NameOfRole) )
        {
            errors.Add(new IdentityError
                       {
                           Description = "AppName of Role Invalid",
                           Code        = nameof(RoleRecord.NameOfRole)
                       });
        }

        if ( string.IsNullOrWhiteSpace(role.NormalizedName) )
        {
            errors.Add(new IdentityError
                       {
                           Description = "NormalizedName of Role Invalid",
                           Code        = nameof(RoleRecord.NormalizedName)
                       });
        }

        IdentityResult result = errors.Count > 0
                                    ? IdentityResult.Failed([..errors])
                                    : IdentityResult.Success;

        return Task.FromResult(result);
    }
}
