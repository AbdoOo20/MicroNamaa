using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

/// <summary>A product card of the POS. Available is the stock of the branch's selling warehouse.</summary>
public record PosProductDto(int Id, string Name, string Sku, string? Barcode, string? ImageUrl, int? CategoryId, decimal Price, int Available, StockState StockState, string? PromotionName, decimal? PromotionPrice);
