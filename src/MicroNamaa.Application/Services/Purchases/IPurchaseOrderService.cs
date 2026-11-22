using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Purchases;

namespace MicroNamaa.Application.Services.Purchases;

/// <summary>
/// Buying from suppliers: Draft → Pending → In transit → Delivered (or Cancelled). Only delivery changes stock,
/// the products' average cost and the supplier's balance.
/// </summary>
public interface IPurchaseOrderService
{
    Task<Result<PagedResult<PurchaseOrderListItemDto>>> GetAllAsync(PurchaseOrderQuery query, CancellationToken cancellationToken = default);

    Task<Result<PurchaseSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<Result<PurchaseOrderDto>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<PurchaseOrderDto>> CreateAsync(SavePurchaseOrderRequest request, CancellationToken cancellationToken = default);

    Task<Result<PurchaseOrderDto>> UpdateAsync(long id, SavePurchaseOrderRequest request, CancellationToken cancellationToken = default);

    Task<Result<PurchaseOrderDto>> UpdateStatusAsync(long id, UpdatePurchaseStatusRequest request, CancellationToken cancellationToken = default);

    Task<Result<PurchaseOrderDto>> DeliverAsync(long id, DeliverPurchaseOrderRequest request, CancellationToken cancellationToken = default);

    Task<Result<PurchaseOrderDto>> CancelAsync(long id, ReasonRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteDraftAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(PurchaseOrderQuery query, CancellationToken cancellationToken = default);
}
