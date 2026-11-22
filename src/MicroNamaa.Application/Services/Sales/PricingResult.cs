namespace MicroNamaa.Application.Services.Sales;

public record PricingResult(
    IReadOnlyList<PricedLine> Lines,
    decimal Subtotal,
    decimal ItemsDiscount,
    int? PromotionId,
    string? PromotionName,
    decimal OrderDiscount,
    int PointsRedeemed,
    decimal PointsDiscount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    int PointsEarned)
{
    public decimal NetBeforeTax => Subtotal - ItemsDiscount - OrderDiscount - PointsDiscount;
}
