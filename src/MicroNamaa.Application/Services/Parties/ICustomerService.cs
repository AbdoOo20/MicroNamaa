using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Parties;

namespace MicroNamaa.Application.Services.Parties;

public interface ICustomerService
{
    Task<Result<PagedResult<CustomerListItemDto>>> GetAllAsync(CustomerQuery query, CancellationToken cancellationToken = default);

    Task<Result<CustomerSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<Result<CustomerDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<CustomerDetailDto>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default);

    Task<Result<CustomerDetailDto>> UpdateAsync(int id, CustomerRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<CustomerDetailDto>> SetAvatarAsync(int id, FileUpload? file, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<BalanceEntryDto>>> GetStatementAsync(int id, BalanceEntryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Money the customer paid towards what they owe.</summary>
    Task<Result<BalanceEntryDto>> ReceivePaymentAsync(int id, PaymentRequest request, CancellationToken cancellationToken = default);

    Task<Result<BalanceEntryDto>> AdjustBalanceAsync(int id, BalanceAdjustmentRequest request, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(CustomerQuery query, CancellationToken cancellationToken = default);
}
