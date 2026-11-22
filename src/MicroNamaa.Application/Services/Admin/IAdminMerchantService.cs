using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

namespace MicroNamaa.Application.Services.Admin;

/// <summary>The platform admin's view of the merchants who bought the system.</summary>
public interface IAdminMerchantService
{
    Task<Result<PagedResult<AdminMerchantListItemDto>>> GetAllAsync(AdminMerchantQuery query, CancellationToken cancellationToken = default);

    Task<Result<AdminMerchantDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<AdminMerchantDetailDto>> CreateAsync(CreateMerchantRequest request, CancellationToken cancellationToken = default);

    Task<Result<AdminMerchantDetailDto>> UpdateAsync(int id, UpdateMerchantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Suspends or reactivates a merchant. A suspended merchant's users cannot sign in.</summary>
    Task<Result<AdminMerchantDetailDto>> UpdateStatusAsync(int id, UpdateMerchantStatusRequest request, CancellationToken cancellationToken = default);

    Task<Result<AdminMerchantDetailDto>> UpdateSubscriptionAsync(int id, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<Result> ResetUserPasswordAsync(int id, int userId, ResetUserPasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result<AdminOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default);
}
