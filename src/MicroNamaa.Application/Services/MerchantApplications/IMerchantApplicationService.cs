using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

using MicroNamaa.Application.DTOs.MerchantApplications;

namespace MicroNamaa.Application.Services.MerchantApplications;

/// <summary>Businesses asking to buy the system from the landing page, and the admin's answer.</summary>
public interface IMerchantApplicationService
{
    Task<Result> SubmitAsync(SubmitMerchantApplicationRequest request, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<MerchantApplicationDto>>> GetAllAsync(MerchantApplicationQuery query, CancellationToken cancellationToken = default);

    Task<Result<MerchantApplicationDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the merchant and its owner account from the request.</summary>
    Task<Result<AdminMerchantDetailDto>> ApproveAsync(int id, ApproveMerchantApplicationRequest request, CancellationToken cancellationToken = default);

    Task<Result<MerchantApplicationDto>> RejectAsync(int id, RejectMerchantApplicationRequest request, CancellationToken cancellationToken = default);
}
