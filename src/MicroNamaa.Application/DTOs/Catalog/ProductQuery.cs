using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Catalog;

public class ProductQuery : PagedQuery
{
    public RecordStatus? Status { get; set; }

    public StockState? StockState { get; set; }

    public int? CategoryId { get; set; }

    /// <summary>Quantity and stock state of this warehouse only (otherwise all warehouses).</summary>
    public int? WarehouseId { get; set; }

    /// <summary>name, price, quantity, newest (default).</summary>
    public string? Sort { get; set; }
}
