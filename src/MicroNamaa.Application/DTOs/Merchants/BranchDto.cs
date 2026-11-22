namespace MicroNamaa.Application.DTOs.Merchants;

public record BranchDto(int Id, string Name, string? City, string? Address, string? Phone, int? DefaultWarehouseId, string? DefaultWarehouseName, bool IsActive, int EmployeesCount, IReadOnlyList<RegisterDto> Registers, DateTime CreatedAt);
