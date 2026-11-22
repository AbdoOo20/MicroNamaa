using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Merchants;

public record MerchantSettingsDto(
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
    int? MaxBranches,
    int? MaxUsers,
    decimal TaxRate,
    bool AllowNegativeStock,
    int DefaultLowStockThreshold,
    bool LoyaltyEnabled,
    decimal PointsPerCurrencyUnit,
    decimal PointValue,
    int MinPointsToRedeem,
    string? ReceiptFooter,
    string Currency);
