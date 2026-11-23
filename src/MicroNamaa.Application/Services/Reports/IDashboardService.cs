using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Reports;

namespace MicroNamaa.Application.Services.Reports;

public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>range: 7d (last 7 days), 1m (last 30 days) or 1y (last 12 months).</summary>
    Task<Result<IReadOnlyList<RevenuePointDto>>> GetRevenueAsync(string? range, CancellationToken cancellationToken = default);

    /// <summary>Best sellers of this month by units.</summary>
    Task<Result<IReadOnlyList<TopProductDto>>> GetTopProductsAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>The latest sales, purchases, returns and stock alerts, newest first.</summary>
    Task<Result<IReadOnlyList<ActivityDto>>> GetActivityAsync(int limit, CancellationToken cancellationToken = default);
}
