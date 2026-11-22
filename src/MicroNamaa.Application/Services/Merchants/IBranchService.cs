using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

namespace MicroNamaa.Application.Services.Merchants;

public interface IBranchService
{
    Task<Result<IReadOnlyList<BranchDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<BranchDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<BranchDto>> CreateAsync(BranchRequest request, CancellationToken cancellationToken = default);

    Task<Result<BranchDto>> UpdateAsync(int id, BranchRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<RegisterDto>> AddRegisterAsync(int branchId, RegisterRequest request, CancellationToken cancellationToken = default);

    Task<Result<RegisterDto>> UpdateRegisterAsync(int branchId, int registerId, RegisterRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteRegisterAsync(int branchId, int registerId, CancellationToken cancellationToken = default);
}
