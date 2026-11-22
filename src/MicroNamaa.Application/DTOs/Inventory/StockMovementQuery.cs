using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Inventory;

public class StockMovementQuery : PagedQuery
{
    public int? ProductId { get; set; }

    public int? WarehouseId { get; set; }

    public StockMovementType? Type { get; set; }

    /// <summary>Libyan date, included.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Libyan date, included.</summary>
    public DateOnly? To { get; set; }
}
