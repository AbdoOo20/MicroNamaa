using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Staff;

public record UpdateStaffRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<StaffPermissionDto> Permissions { get; init; } = [];
}
