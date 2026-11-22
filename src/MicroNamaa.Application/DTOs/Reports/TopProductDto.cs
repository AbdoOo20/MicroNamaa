namespace MicroNamaa.Application.DTOs.Reports;

public record TopProductDto(int ProductId, string Name, string Sku, string? ImageUrl, int UnitsSold, decimal Revenue);
