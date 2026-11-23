using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

/// <summary>
/// The POS cart. With <see cref="Complete" /> false it is saved as a draft (Save As Draft / Hold); with true it is
/// completed at once (stock, balance and points change) and <see cref="Payment" /> is required.
/// </summary>
public record SaveSalesInvoiceRequest
{
    /// <summary>Owners and managers may pick the branch; a cashier always sells from their own.</summary>
    public int? BranchId { get; init; }

    public int? RegisterId { get; init; }

    public int? CustomerId { get; init; }

    [Required, MinLength(1), MaxLength(300)]
    public IReadOnlyList<SalesInvoiceItemRequest> Items { get; init; } = [];

    /// <summary>An order promotion (scope Order) chosen by the cashier.</summary>
    public int? PromotionId { get; init; }

    public DiscountType? DiscountType { get; init; }

    [Range(0, 100000000)]
    public decimal? DiscountValue { get; init; }

    [Range(0, 100000000)]
    public int PointsToRedeem { get; init; }

    [MaxLength(500)]
    public string? Note { get; init; }

    public bool Complete { get; init; }

    public InvoicePaymentRequest? Payment { get; init; }
}
