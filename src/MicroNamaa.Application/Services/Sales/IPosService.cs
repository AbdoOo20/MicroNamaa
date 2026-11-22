using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Sales;

namespace MicroNamaa.Application.Services.Sales;

/// <summary>The cashier screen: what it opens with, the product cards and the barcode scan.</summary>
public interface IPosService
{
    Task<Result<PosContextDto>> GetContextAsync(int? branchId, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<PosProductDto>>> GetProductsAsync(PosProductQuery query, CancellationToken cancellationToken = default);

    /// <summary>Finds an active product by its exact barcode or SKU.</summary>
    Task<Result<PosProductDto>> LookupAsync(string code, int? branchId, CancellationToken cancellationToken = default);
}
