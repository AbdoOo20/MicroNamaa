using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Sales;

/// <summary>Everything <see cref="SalesPricing" /> needs, already loaded and checked.</summary>
public record PricingInput(
    IReadOnlyList<(Product Product, int Quantity)> Lines,
    IReadOnlyList<Promotion> ItemPromotions,
    Promotion? OrderPromotion,
    DiscountType? ManualDiscountType,
    decimal? ManualDiscountValue,
    int PointsToRedeem,
    decimal PointValue,
    decimal TaxRate,
    decimal PointsPerCurrencyUnit,
    bool EarnsPoints);
