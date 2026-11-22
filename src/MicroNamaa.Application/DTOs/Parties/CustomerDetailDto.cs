using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public record CustomerDetailDto(
    int Id,
    string Code,
    string Name,
    string? Phone,
    string? Email,
    string? City,
    string? Address,
    string? AvatarUrl,
    int? BranchId,
    string? BranchName,
    decimal Balance,
    decimal? CreditLimit,
    int LoyaltyPoints,
    RecordStatus Status,
    string? Notes,
    int OrdersCount,
    decimal LifetimeValue,
    decimal AverageOrderValue,
    DateTime? LastPurchaseAt,
    IReadOnlyList<CustomerInvoiceDto> RecentInvoices,
    DateTime CreatedAt);
