using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Staff;

public record StaffPermissionDto(AdminArea Area, AdminActions Actions);
