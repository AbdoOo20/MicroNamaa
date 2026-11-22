using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Inventory;

/// <summary>One change for <see cref="StockLedger" />: a positive quantity adds stock, a negative one takes it.</summary>
public record StockChange(int WarehouseId, int ProductId, int Quantity, StockMovementType Type, decimal UnitCost, string? ReferenceType = null, long? ReferenceId = null, string? ReferenceCode = null, string? Note = null);
