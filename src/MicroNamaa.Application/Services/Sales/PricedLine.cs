namespace MicroNamaa.Application.Services.Sales;

public record PricedLine(int ProductId, string Name, string Sku, int Quantity, decimal UnitPrice, decimal UnitCost, decimal Discount, int? PromotionId, string? PromotionName, decimal LineTotal);
