using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Employees;

namespace MicroNamaa.Application.Services.Employees;

/// <summary>
/// The merchant's users. Owners manage everyone; managers manage cashiers only. Nobody can deactivate or demote
/// themselves, and a merchant always keeps one active owner.
/// </summary>
public interface IEmployeeService
{
    Task<Result<PagedResult<EmployeeDto>>> GetAllAsync(EmployeeQuery query, CancellationToken cancellationToken = default);

    Task<Result<EmployeeSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<Result<EmployeeDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<Result<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(int id, ResetEmployeePasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result<EmployeeDto>> SetAvatarAsync(int id, FileUpload? file, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(EmployeeQuery query, CancellationToken cancellationToken = default);
}
