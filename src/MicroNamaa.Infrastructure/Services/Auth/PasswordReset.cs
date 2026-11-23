using Microsoft.AspNetCore.Identity;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Services.Auth;

/// <summary>Sets a new password chosen by an admin or a store owner, checked against the password rules, without e-mailed reset tokens.</summary>
public static class PasswordReset
{
    public static async Task<IdentityResult> SetAsync(UserManager<User> userManager, User user, string newPassword)
    {
        foreach (var validator in userManager.PasswordValidators)
        {
            var valid = await validator.ValidateAsync(userManager, user, newPassword);

            if (!valid.Succeeded)
            {
                return valid;
            }
        }

        var removed = await userManager.RemovePasswordAsync(user);

        return removed.Succeeded ? await userManager.AddPasswordAsync(user, newPassword) : removed;
    }
}
