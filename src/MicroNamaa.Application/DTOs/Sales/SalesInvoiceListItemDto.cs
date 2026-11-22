using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

public record SalesInvoiceListItemDto(long Id, string Code, SalesInvoiceStatus Status, string BranchName, int? CustomerId, string? CustomerName, string CashierName, int ItemsCount, int UnitsCount, decimal Total, decimal PaidAmount, decimal DueAmount, decimal ReturnedAmount, PaymentMethod? PaymentMethod, DateTime CreatedAt, DateTime? CompletedAt);
