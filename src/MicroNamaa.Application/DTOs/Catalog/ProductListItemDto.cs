using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Catalog;

public record ProductListItemDto(int Id, string Name, string Sku, string? Barcode, string? ImageUrl, int? CategoryId, string? CategoryName, int Quantity, decimal Cost, decimal Price, RecordStatus Status, StockState StockState, int LowStockThreshold, DateTime CreatedAt);
