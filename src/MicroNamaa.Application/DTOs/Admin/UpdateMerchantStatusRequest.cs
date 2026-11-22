using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Admin;

public record UpdateMerchantStatusRequest
{
    public MerchantStatus Status { get; init; }

    [MaxLength(500)]
    public string? Note { get; init; }
}
