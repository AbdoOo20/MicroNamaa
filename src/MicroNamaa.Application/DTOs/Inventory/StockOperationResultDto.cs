namespace MicroNamaa.Application.DTOs.Inventory;

/// <summary>What an adjustment or a transfer changed.</summary>
public record StockOperationResultDto(string Reference, int Lines, int UnitsChanged);
