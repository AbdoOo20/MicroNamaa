namespace MicroNamaa.Application.DTOs.Purchases;

public record PurchaseOrderItemDto(long Id, int ProductId, string ProductName, string Sku, string? ImageUrl, int Quantity, decimal UnitCost, decimal Total);
