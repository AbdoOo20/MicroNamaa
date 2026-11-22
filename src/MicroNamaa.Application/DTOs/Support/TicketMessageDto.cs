namespace MicroNamaa.Application.DTOs.Support;

public record TicketMessageDto(long Id, string Body, bool IsFromAdmin, string SenderName, DateTime CreatedAt);
