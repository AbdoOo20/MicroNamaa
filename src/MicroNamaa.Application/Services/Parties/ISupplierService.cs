using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Parties;

namespace MicroNamaa.Application.Services.Parties;

public interface ISupplierService
{
    Task<Result<PagedResult<SupplierListItemDto>>> GetAllAsync(SupplierQuery query, CancellationToken cancellationToken = default);

    Task<Result<SupplierSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<Result<SupplierDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<SupplierDetailDto>> CreateAsync(SupplierRequest request, CancellationToken cancellationToken = default);

    Task<Result<SupplierDetailDto>> UpdateAsync(int id, SupplierRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<SupplierDetailDto>> SetLogoAsync(int id, FileUpload? file, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<BalanceEntryDto>>> GetStatementAsync(int id, BalanceEntryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Money the merchant paid the supplier.</summary>
    Task<Result<BalanceEntryDto>> PayAsync(int id, PaymentRequest request, CancellationToken cancellationToken = default);

    Task<Result<BalanceEntryDto>> AdjustBalanceAsync(int id, BalanceAdjustmentRequest request, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(SupplierQuery query, CancellationToken cancellationToken = default);
}
