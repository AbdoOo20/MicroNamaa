using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Inventory;

namespace MicroNamaa.Application.Services.Inventory;

public interface IWarehouseService
{
    Task<Result<PagedResult<WarehouseDto>>> GetAllAsync(WarehouseQuery query, CancellationToken cancellationToken = default);

    Task<Result<WarehouseSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<Result<WarehouseDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<WarehouseDto>> CreateAsync(WarehouseRequest request, CancellationToken cancellationToken = default);

    Task<Result<WarehouseDto>> UpdateAsync(int id, WarehouseRequest request, CancellationToken cancellationToken = default);

    /// <summary>Only an empty warehouse that no branch sells from can be deleted.</summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(WarehouseQuery query, CancellationToken cancellationToken = default);
}
