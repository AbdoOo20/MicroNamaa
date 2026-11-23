using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Admin;

/// <summary>Either a new end date, or a number of days added to the current end (or to today when it already ended).</summary>
public record UpdateSubscriptionRequest
{
    public DateOnly? EndsOn { get; init; }

    [Range(1, 3650)]
    public int? ExtendDays { get; init; }

    /// <summary>True removes the end date (no limit).</summary>
    public bool Unlimited { get; init; }
}
