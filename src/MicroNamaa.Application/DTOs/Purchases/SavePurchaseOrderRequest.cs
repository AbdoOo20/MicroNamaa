using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Purchases;

public record SavePurchaseOrderRequest
{
    [Range(1, int.MaxValue)]
    public int SupplierId { get; init; }

    /// <summary>Where the goods will be received.</summary>
    [Range(1, int.MaxValue)]
    public int WarehouseId { get; init; }

    /// <summary>Libyan date; today when empty.</summary>
    public DateOnly? OrderDate { get; init; }

    public DateOnly? ExpectedAt { get; init; }

    /// <summary>Net 15, Net 30, due on receipt...</summary>
    [MaxLength(50)]
    public string? PaymentTerms { get; init; }

    public DateOnly? DueDate { get; init; }

    [Required, MinLength(1), MaxLength(500)]
    public IReadOnlyList<PurchaseOrderItemRequest> Items { get; init; } = [];

    [Range(0, 100000000)]
    public decimal Discount { get; init; }

    /// <summary>Percent; the store's tax rate when empty.</summary>
    [Range(0, 100)]
    public decimal? TaxRate { get; init; }

    [Range(0, 100000000)]
    public decimal ShippingCost { get; init; }

    [MaxLength(1000)]
    public string? Note { get; init; }

    /// <summary>Draft, Pending (ordered) or InTransit. Delivery is its own step.</summary>
    public PurchaseOrderStatus Status { get; init; } = PurchaseOrderStatus.Pending;
}
