using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Reports;

namespace MicroNamaa.Application.Services.Reports;

public interface IReportService
{
    Task<Result<ReportOverviewDto>> GetOverviewAsync(ReportQuery query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RevenuePointDto>>> GetSalesTrendAsync(SalesTrendQuery query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TopProductDto>>> GetTopProductsAsync(ReportQuery query, int limit, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CategorySalesDto>>> GetCategoriesAsync(ReportQuery query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EmployeeSalesDto>>> GetEmployeesAsync(ReportQuery query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PaymentMethodSalesDto>>> GetPaymentMethodsAsync(ReportQuery query, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<BranchSalesDto>>> GetBranchesAsync(ReportQuery query, CancellationToken cancellationToken = default);

    /// <summary>Stock on hand per warehouse right now (not tied to the dates).</summary>
    Task<Result<IReadOnlyList<WarehouseStockDto>>> GetWarehousesAsync(CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(ReportQuery query, CancellationToken cancellationToken = default);
}
