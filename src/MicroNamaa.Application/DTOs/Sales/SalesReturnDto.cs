using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

public record SalesReturnDto(long Id, string Code, long SalesInvoiceId, string InvoiceCode, int? CustomerId, string? CustomerName, int BranchId, string BranchName, string WarehouseName, RefundMethod RefundMethod, decimal Total, int PointsReversed, string? Reason, string? CreatedByName, IReadOnlyList<SalesReturnItemDto> Items, DateTime CreatedAt);
