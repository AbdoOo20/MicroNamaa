using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Support;

public record CreateTicketRequest
{
    public TicketCategory Category { get; init; } = TicketCategory.General;

    [Required, MaxLength(200)]
    public string Subject { get; init; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Body { get; init; } = string.Empty;
}
