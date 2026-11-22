using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

namespace MicroNamaa.Application.Services.Admin;

/// <summary>Creates a merchant with its owner account, first branch, warehouse and register in one transaction.</summary>
public interface IMerchantProvisioningService
{
    /// <returns>The new merchant's id.</returns>
    Task<Result<int>> CreateAsync(CreateMerchantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sets a new password for one of a merchant's users (for example an owner who forgot it).</summary>
    Task<Result> ResetPasswordAsync(int merchantId, int userId, string newPassword, CancellationToken cancellationToken = default);
}
