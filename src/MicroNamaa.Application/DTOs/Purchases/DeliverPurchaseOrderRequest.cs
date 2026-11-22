using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Purchases;

/// <summary>The goods arrived: they enter the warehouse, and what is not paid now goes to the supplier's balance.</summary>
public record DeliverPurchaseOrderRequest
{
    [Range(0, 100000000)]
    public decimal PaidAmount { get; init; }

    public PaymentMethod? PaymentMethod { get; init; }

    public DateTime? DeliveredAt { get; init; }
}
