using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

public record CreateSalesReturnRequest
{
    [Range(1, long.MaxValue)]
    public long SalesInvoiceId { get; init; }

    [Required, MinLength(1), MaxLength(300)]
    public IReadOnlyList<ReturnItemRequest> Items { get; init; } = [];

    public RefundMethod RefundMethod { get; init; } = RefundMethod.Cash;

    [MaxLength(500)]
    public string? Reason { get; init; }
}
