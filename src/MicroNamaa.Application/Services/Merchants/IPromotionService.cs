using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

namespace MicroNamaa.Application.Services.Merchants;

public interface IPromotionService
{
    Task<Result<PagedResult<PromotionDto>>> GetAllAsync(PromotionQuery query, CancellationToken cancellationToken = default);

    Task<Result<PromotionDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<PromotionDto>> CreateAsync(PromotionRequest request, CancellationToken cancellationToken = default);

    Task<Result<PromotionDto>> UpdateAsync(int id, PromotionRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
