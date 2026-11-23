using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sync;

public record SyncCustomerDto(int Id, string Code, string Name, string? Phone, string? Email, string? City, string? Address, string? AvatarUrl, int? BranchId, decimal Balance, decimal? CreditLimit, int LoyaltyPoints, RecordStatus Status, string? Notes, DateTime UpdatedAt);
