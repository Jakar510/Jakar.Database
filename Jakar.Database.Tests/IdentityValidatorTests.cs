// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using Jakar.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Jakar.Database.Tests;


[TestFixture]
public sealed class IdentityValidatorTests : Assert
{
    private static UserRecord CreateUser() => UserRecord.Create("identity-user", "P@ssword123!", new UserRights(string.Empty));


#region UserValidator

    [Test] public async Task UserValidator_accepts_a_user_with_a_name()
    {
        IdentityResult result = await new UserValidator().ValidateAsync(null!, CreateUser());

        That(result.Succeeded, Is.True);
    }

    [Test] public async Task UserValidator_rejects_a_blank_user_name()
    {
        IdentityResult result = await new UserValidator().ValidateAsync(null!, CreateUser() with { UserName = string.Empty });

        Multiple(() =>
                 {
                     That(result.Succeeded, Is.False);
                     That(result.Errors.Select(static x => x.Description), Does.Contain($"{nameof(UserRecord.UserName)} is invalid"));
                 });
    }

#endregion



#region RoleValidator

    [Test] public async Task RoleValidator_accepts_a_fully_populated_role()
    {
        IdentityResult result = await new RoleValidator().ValidateAsync(null!, new RoleRecord("Admin", "ADMINS"));

        That(result.Succeeded, Is.True);
    }

    [Test] public async Task RoleValidator_rejects_missing_name_and_normalized_name()
    {
        IdentityResult missingName       = await new RoleValidator().ValidateAsync(null!, new RoleRecord(string.Empty, string.Empty));
        IdentityResult missingNormalized = await new RoleValidator().ValidateAsync(null!, new RoleRecord("Admin", string.Empty));

        Multiple(() =>
                 {
                     That(missingName.Succeeded, Is.False);
                     That(missingName.Errors.Select(static x => x.Code), Does.Contain(nameof(RoleRecord.NameOfRole)));
                     That(missingNormalized.Succeeded, Is.False);
                     That(missingNormalized.Errors.Select(static x => x.Code), Does.Contain(nameof(RoleRecord.NormalizedName)));
                 });
    }

#endregion



#region UserPasswordValidator

    [Test] public async Task UserPasswordValidator_sync_and_async_paths_agree()
    {
        UserPasswordValidator validator = new(Options.Create(PasswordRequirements.Current));
        UserRecord            user      = CreateUser();

        IdentityResult sync  = validator.Validate("P@ssword123!");
        IdentityResult async = await validator.ValidateAsync(null!, user, "P@ssword123!");

        Multiple(() =>
                 {
                     That(sync,             Is.Not.Null);
                     That(async.Succeeded,  Is.EqualTo(sync.Succeeded));
                     That(validator.Validate("P@ssword123!").Succeeded, Is.EqualTo(sync.Succeeded)); // deterministic
                 });
    }

#endregion



#region Password / recovery-code hashing

    [Test] public void UserRecord_password_hash_round_trips()
    {
        UserRecord user = UserRecord.Create("hash-user", "Sup3r$ecret!", new UserRights(string.Empty));

        PasswordVerificationResult correct = UserRecord.Hasher.VerifyHashedPassword(user, user.PasswordHash!, "Sup3r$ecret!");
        PasswordVerificationResult wrong   = UserRecord.Hasher.VerifyHashedPassword(user, user.PasswordHash!, "not-the-password");

        Multiple(() =>
                 {
                     That(user.HasPassword, Is.True);
                     That(correct, Is.Not.EqualTo(PasswordVerificationResult.Failed));
                     That(wrong,   Is.EqualTo(PasswordVerificationResult.Failed));
                 });
    }

    [Test] public void RecoveryCode_hash_validates_only_the_original_code()
    {
        UserRecord user = UserRecord.Create("recovery-user", "P@ssword123!", new UserRights(string.Empty));

        ( string code, RecoveryCodeRecord record ) = RecoveryCodeRecord.Create(user, "recovery-code-abc");

        Multiple(() =>
                 {
                     That(code, Is.EqualTo("recovery-code-abc"));
                     That(RecoveryCodeRecord.IsValid("recovery-code-abc", record), Is.True);
                     That(RecoveryCodeRecord.IsValid("wrong-code",        record), Is.False);
                 });
    }

#endregion
}
