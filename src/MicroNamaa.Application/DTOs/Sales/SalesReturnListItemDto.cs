using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

public record SalesReturnListItemDto(long Id, string Code, long SalesInvoiceId, string InvoiceCode, string? CustomerName, string BranchName, RefundMethod RefundMethod, int UnitsCount, decimal Total, DateTime CreatedAt);
