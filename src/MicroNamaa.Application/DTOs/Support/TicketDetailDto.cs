namespace MicroNamaa.Application.DTOs.Support;

public record TicketDetailDto(TicketDto Ticket, IReadOnlyList<TicketMessageDto> Messages);
