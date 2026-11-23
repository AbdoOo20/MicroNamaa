namespace MicroNamaa.Application.DTOs.Admin;

public record MerchantUserDto(int Id, string FullName, string? Phone, string? Email, string Role, bool IsActive, DateTime? LastLoginAt);
