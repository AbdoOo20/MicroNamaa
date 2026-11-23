using MicroNamaa.Application.DTOs.Catalog;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sync;

public record SyncProductDto(int Id, string Name, string Sku, string? Barcode, int? CategoryId, string? Description, string? ImageUrl, decimal Cost, decimal Price, int? LowStockThreshold, RecordStatus Status, IReadOnlyList<ProductStockDto> Stock, DateTime UpdatedAt);
