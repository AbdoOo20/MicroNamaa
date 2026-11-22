namespace MicroNamaa.Application.DTOs.Sales;

/// <summary>A category chip of the POS; the first one (Id empty) is "All Items".</summary>
public record PosCategoryDto(int? Id, string Name, int ProductsCount);
