namespace MicroNamaa.Application.DTOs.Staff;

public record StaffMemberDto(int Id, string FullName, string? Email, string? Phone, bool IsActive, DateTime? LastLoginAt, DateTime CreatedAt, IReadOnlyList<StaffPermissionDto> Permissions);
