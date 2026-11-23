using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Inventory;

namespace MicroNamaa.Application.Services.Inventory;

/// <summary>Stock levels per warehouse, the movement history, stock counts and transfers between warehouses.</summary>
public interface IInventoryService
{
    Task<Result<PagedResult<StockRowDto>>> GetStockAsync(StockQuery query, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<StockMovementDto>>> GetMovementsAsync(StockMovementQuery query, CancellationToken cancellationToken = default);

    /// <summary>Sets each product to the counted quantity; the difference is written as an adjustment.</summary>
    Task<Result<StockOperationResultDto>> AdjustAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default);

    Task<Result<StockOperationResultDto>> TransferAsync(StockTransferRequest request, CancellationToken cancellationToken = default);
}
