using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public record CustomerInvoiceDto(long Id, string Code, SalesInvoiceStatus Status, decimal Total, decimal DueAmount, decimal ReturnedAmount, PaymentMethod? PaymentMethod, DateTime CreatedAt);
