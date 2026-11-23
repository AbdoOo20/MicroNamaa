using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Reports;

public record PaymentMethodSalesDto(PaymentMethod Method, int Orders, decimal Amount, decimal PaidNow, decimal OnAccount);
