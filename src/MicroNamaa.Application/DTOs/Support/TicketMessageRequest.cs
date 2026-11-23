using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Support;

public record TicketMessageRequest
{
    [Required, MaxLength(2000)]
    public string Body { get; init; } = string.Empty;
}
