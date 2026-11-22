using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Sales;

namespace MicroNamaa.Application.Services.Sales;

/// <summary>Goods brought back from a completed invoice. One kind of return for now; refunded in cash, by card, or to the customer's balance.</summary>
public interface ISalesReturnService
{
    Task<Result<PagedResult<SalesReturnListItemDto>>> GetAllAsync(SalesReturnQuery query, CancellationToken cancellationToken = default);

    Task<Result<SalesReturnDto>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(SalesReturnQuery query, CancellationToken cancellationToken = default);
}
