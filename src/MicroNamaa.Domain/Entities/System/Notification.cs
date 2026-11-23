using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

public class Notification : BaseEntity<long>
{
    public NotificationAudience Audience { get; set; }

    public int? UserId { get; set; }

    public int? MerchantId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    /// <summary>What the notification is about (Product, SalesInvoice, PurchaseOrder, Ticket...), to open it from the bell.</summary>
    public string? RelatedType { get; set; }

    public long? RelatedId { get; set; }

    public bool IsRead { get; set; }
}
