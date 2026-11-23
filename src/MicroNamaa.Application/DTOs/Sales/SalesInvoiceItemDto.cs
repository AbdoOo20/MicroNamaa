namespace MicroNamaa.Application.DTOs.Sales;

public record SalesInvoiceItemDto(long Id, int ProductId, string ProductName, string Sku, string? ImageUrl, int Quantity, decimal UnitPrice, decimal DiscountAmount, string? PromotionName, decimal LineTotal, int ReturnedQuantity);
