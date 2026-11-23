using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Reports;

using MicroNamaa.Application.Services.Reports;

namespace MicroNamaa.Api.Controllers;

/// <summary>Every report takes ?from=&amp;to= (Libyan dates, last 30 days by default) and ?branchId=.</summary>
[Authorize(Roles = AppRoles.Managers)]
public class ReportsController(IReportService reportService) : ApiControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<Result<ReportOverviewDto>>> GetOverview([FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        return Respond(await reportService.GetOverviewAsync(query, cancellationToken));
    }

    /// <summary>?groupBy=day|month</summary>
    [HttpGet("sales-trend")]
    public async Task<ActionResult<Result<IReadOnlyList<RevenuePointDto>>>> GetSalesTrend([FromQuery] SalesTrendQuery query, CancellationToken cancellationToken)
    {
        return Respond(await reportService.GetSalesTrendAsync(query, cancellationToken));
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<Result<IReadOnlyList<TopProductDto>>>> GetTopProducts([FromQuery] ReportQuery query, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        return Respond(await reportService.GetTopProductsAsync(query, limit, cancellationToken));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<Result<IReadOnlyList<CategorySalesDto>>>> GetCategories([FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        return Respond(await reportService.GetCategoriesAsync(query, cancellationToken));
    }

    [HttpGet("employees")]
    public async Task<ActionResult<Result<IReadOnlyList<EmployeeSalesDto>>>> GetEmployees([FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        return Respond(await reportService.GetEmployeesAsync(query, cancellationToken));
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<Result<IReadOnlyList<PaymentMethodSalesDto>>>> GetPaymentMethods([FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        return Respond(await reportService.GetPaymentMethodsAsync(query, cancellationToken));
    }

    [HttpGet("branches")]
    public async Task<ActionResult<Result<IReadOnlyList<BranchSalesDto>>>> GetBranches([FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        return Respond(await reportService.GetBranchesAsync(query, cancellationToken));
    }

    [HttpGet("warehouses")]
    public async Task<ActionResult<Result<IReadOnlyList<WarehouseStockDto>>>> GetWarehouses(CancellationToken cancellationToken)
    {
        return Respond(await reportService.GetWarehousesAsync(cancellationToken));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ReportQuery query, CancellationToken cancellationToken)
    {
        return Download(await reportService.ExportAsync(query, cancellationToken));
    }
}
