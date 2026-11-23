namespace MicroNamaa.Application.DTOs.Sync;

/// <summary>Ids deleted since the last pull; remove them from the device.</summary>
public record SyncDeletedDto(IReadOnlyList<int> Categories, IReadOnlyList<int> Products, IReadOnlyList<int> Customers, IReadOnlyList<int> Suppliers, IReadOnlyList<int> Warehouses, IReadOnlyList<int> Promotions);
