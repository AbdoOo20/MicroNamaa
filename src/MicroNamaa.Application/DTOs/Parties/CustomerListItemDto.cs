using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

/// <summary>LifetimeValue is every completed invoice minus what was returned.</summary>
public record CustomerListItemDto(int Id, string Code, string Name, string? Phone, string? Email, string? AvatarUrl, int? BranchId, string? BranchName, int OrdersCount, decimal LifetimeValue, decimal Balance, int LoyaltyPoints, RecordStatus Status, DateTime? LastPurchaseAt, DateTime CreatedAt);
