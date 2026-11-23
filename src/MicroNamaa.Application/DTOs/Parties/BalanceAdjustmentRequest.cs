using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Parties;

/// <summary>A correction of the balance (an opening balance, a mistake). Positive adds to the balance, negative takes from it.</summary>
public record BalanceAdjustmentRequest
{
    [Range(-100000000, 100000000)]
    public decimal Amount { get; init; }

    [Required, MaxLength(500)]
    public string Note { get; init; } = string.Empty;
}
