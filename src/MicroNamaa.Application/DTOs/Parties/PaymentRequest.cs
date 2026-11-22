using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

/// <summary>Money received from a customer, or paid to a supplier.</summary>
public record PaymentRequest
{
    [Range(0.001, 100000000)]
    public decimal Amount { get; init; }

    public PaymentMethod Method { get; init; } = PaymentMethod.Cash;

    [MaxLength(100)]
    public string? Reference { get; init; }

    [MaxLength(500)]
    public string? Note { get; init; }
}
