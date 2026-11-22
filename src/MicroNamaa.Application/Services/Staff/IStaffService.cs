using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Staff;

namespace MicroNamaa.Application.Services.Staff;

/// <summary>Sub-admin accounts; only the owner admin manages them.</summary>
public interface IStaffService
{
    Task<Result<IReadOnlyList<StaffMemberDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<StaffMemberDto>> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);

    Task<Result<StaffMemberDto>> UpdateAsync(int id, UpdateStaffRequest request, CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(int id, ResetStaffPasswordRequest request, CancellationToken cancellationToken = default);
}
