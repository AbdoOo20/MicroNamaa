namespace MicroNamaa.Application.DTOs.Sales;

public record InvoicePreviewLineDto(int ProductId, string Name, string Sku, string? ImageUrl, int Quantity, decimal UnitPrice, decimal Discount, string? PromotionName, decimal LineTotal, int Available);
