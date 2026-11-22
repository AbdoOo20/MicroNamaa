using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Purchases;

public record UpdatePurchaseStatusRequest
{
    /// <summary>Pending or InTransit.</summary>
    public PurchaseOrderStatus Status { get; init; }
}
