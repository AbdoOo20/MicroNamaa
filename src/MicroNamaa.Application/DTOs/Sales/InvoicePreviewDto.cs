namespace MicroNamaa.Application.DTOs.Sales;

/// <summary>The cart totals as the server will save them, for the right-hand panel of the POS.</summary>
public record InvoicePreviewDto(
    IReadOnlyList<InvoicePreviewLineDto> Lines,
    int ItemsCount,
    int UnitsCount,
    decimal Subtotal,
    decimal ItemsDiscount,
    string? PromotionName,
    decimal OrderDiscount,
    int PointsRedeemed,
    decimal PointsDiscount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    int PointsEarned,
    int? CustomerPoints,
    decimal? CustomerBalance,
    IReadOnlyList<string> Warnings);
