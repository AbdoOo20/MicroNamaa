using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Admin;

public record AdminMerchantListItemDto(int Id, string Code, string Name, string OwnerName, string Phone, string? City, string? LogoUrl, MerchantStatus Status, DateTime? SubscriptionEndsAt, bool SubscriptionActive, int BranchesCount, int UsersCount, decimal SalesLast30Days, DateTime? LastSaleAt, DateTime CreatedAt);
