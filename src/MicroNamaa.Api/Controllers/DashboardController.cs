using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Reports;

using MicroNamaa.Application.Services.Reports;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Managers)]
public class DashboardController(IDashboardService dashboardService) : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<Result<DashboardSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        return Respond(await dashboardService.GetSummaryAsync(cancellationToken));
    }

    /// <summary>?range=7d|1m|1y</summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<Result<IReadOnlyList<RevenuePointDto>>>> GetRevenue([FromQuery] string? range, CancellationToken cancellationToken)
    {
        return Respond(await dashboardService.GetRevenueAsync(range, cancellationToken));
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<Result<IReadOnlyList<TopProductDto>>>> GetTopProducts([FromQuery] int limit = 4, CancellationToken cancellationToken = default)
    {
        return Respond(await dashboardService.GetTopProductsAsync(limit, cancellationToken));
    }

    [HttpGet("activity")]
    public async Task<ActionResult<Result<IReadOnlyList<ActivityDto>>>> GetActivity([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        return Respond(await dashboardService.GetActivityAsync(limit, cancellationToken));
    }
}
