using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Catalog;

public record CreateProductRequest : ProductRequest
{
    /// <summary>The "Available Quantity" of the add screen, put in <see cref="WarehouseId" /> as opening stock.</summary>
    [Range(0, 10000000)]
    public int OpeningQuantity { get; init; }

    /// <summary>Where the opening stock goes; empty uses the main branch's selling warehouse.</summary>
    public int? WarehouseId { get; init; }
}
