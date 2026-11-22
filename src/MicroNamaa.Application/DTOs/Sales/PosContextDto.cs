using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Merchants;

namespace MicroNamaa.Application.DTOs.Sales;

/// <summary>Everything the POS screen needs when it opens.</summary>
public record PosContextDto(
    string MerchantName,
    string? MerchantLogoUrl,
    IdNameDto Branch,
    IReadOnlyList<IdNameDto> Branches,
    IReadOnlyList<RegisterDto> Registers,
    IdNameDto Warehouse,
    IdNameDto Cashier,
    string? CashierAvatarUrl,
    decimal TaxRate,
    string Currency,
    PosLoyaltyDto Loyalty,
    IReadOnlyList<PosCategoryDto> Categories,
    IReadOnlyList<PromotionDto> OrderPromotions,
    string? ReceiptFooter);
