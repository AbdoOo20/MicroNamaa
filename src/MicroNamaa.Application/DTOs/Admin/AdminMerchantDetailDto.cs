using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Admin;

public record AdminMerchantDetailDto(
    int Id,
    string Code,
    string Name,
    string OwnerName,
    string Phone,
    string? Email,
    string? City,
    string? Address,
    string? TaxNumber,
    string? LogoUrl,
    MerchantStatus Status,
    DateTime? SubscriptionEndsAt,
    bool SubscriptionActive,
    int? MaxBranches,
    int? MaxUsers,
    string? Notes,
    int BranchesCount,
    int ProductsCount,
    int CustomersCount,
    int InvoicesCount,
    decimal TotalSales,
    decimal SalesLast30Days,
    DateTime? LastSaleAt,
    IReadOnlyList<MerchantUserDto> Users,
    DateTime CreatedAt);
