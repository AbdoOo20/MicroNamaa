namespace MicroNamaa.Application.DTOs.Notifications;

/// <summary>RelatedType + RelatedId tell the frontend which screen to open (Product, SalesInvoice, PurchaseOrder, SalesReturn, Ticket...).</summary>
public record NotificationDto(long Id, string Type, string Title, string? Body, string? RelatedType, long? RelatedId, bool IsRead, DateTime CreatedAt);
