using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Support;

public record TicketDto(long Id, string Subject, TicketCategory Category, TicketStatus Status, int? MerchantId, string? MerchantName, string OpenedBy, DateTime LastMessageAt, DateTime CreatedAt);
