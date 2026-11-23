using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>A help-center conversation between a merchant's user and the platform's support team.</summary>
public class Ticket : BaseEntity<long>
{
    public int OpenedById { get; set; }

    public int? MerchantId { get; set; }

    public TicketCategory Category { get; set; }

    public string Subject { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime LastMessageAt { get; set; }

    public User OpenedBy { get; set; } = null!;

    public Merchant? Merchant { get; set; }

    public ICollection<TicketMessage> Messages { get; set; } = [];
}
