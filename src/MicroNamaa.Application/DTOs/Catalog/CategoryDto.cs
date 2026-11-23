using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Catalog;

public record CategoryDto(int Id, string Name, string? Description, string? ImageUrl, RecordStatus Status, int SortOrder, int ProductsCount, int StockUnits, decimal StockValue, DateTime CreatedAt);
