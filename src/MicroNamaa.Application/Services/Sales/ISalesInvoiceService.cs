using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Sales;

namespace MicroNamaa.Application.Services.Sales;

public interface ISalesInvoiceService
{
    Task<Result<PagedResult<SalesInvoiceListItemDto>>> GetAllAsync(SalesInvoiceQuery query, CancellationToken cancellationToken = default);

    Task<Result<SalesInvoiceSummaryDto>> GetSummaryAsync(SalesInvoiceQuery query, CancellationToken cancellationToken = default);

    Task<Result<SalesInvoiceDto>> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Prices the cart without saving anything.</summary>
    Task<Result<InvoicePreviewDto>> PreviewAsync(SaveSalesInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Saves a new invoice as a draft, or completes it at once.</summary>
    Task<Result<SalesInvoiceDto>> CreateAsync(SaveSalesInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a sale made while the device was offline (from the sync push). It keeps the prices the customer paid,
    /// the promotions running at that time and its real time, and is not refused for stock, credit limit or points.
    /// </summary>
    Task<Result<SalesInvoiceDto>> CreateOfflineAsync(SaveSalesInvoiceRequest request, OfflineSale offline, CancellationToken cancellationToken = default);

    /// <summary>Changes a draft (and completes it when asked).</summary>
    Task<Result<SalesInvoiceDto>> UpdateAsync(long id, SaveSalesInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Completes a draft with today's prices and the given payment.</summary>
    Task<Result<SalesInvoiceDto>> CompleteAsync(long id, CompleteSalesInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Voids a completed invoice without returns: stock, the customer's balance and points are put back. Owners and managers only.</summary>
    Task<Result<SalesInvoiceDto>> CancelAsync(long id, ReasonRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteDraftAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(SalesInvoiceQuery query, CancellationToken cancellationToken = default);
}
