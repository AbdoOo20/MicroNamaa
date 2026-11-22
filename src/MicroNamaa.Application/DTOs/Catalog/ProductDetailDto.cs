using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Catalog;

public record ProductDetailDto(
    int Id,
    string Name,
    string Sku,
    string? Barcode,
    string? Description,
    string? ImageUrl,
    int? CategoryId,
    string? CategoryName,
    decimal Cost,
    decimal Price,
    int? LowStockThreshold,
    int EffectiveLowStockThreshold,
    RecordStatus Status,
    int Quantity,
    StockState StockState,
    IReadOnlyList<ProductStockDto> Stock,
    ProductPerformanceDto Performance,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
