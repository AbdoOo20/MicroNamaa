using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

public class TicketMessage : BaseEntity<long>
{
    public long TicketId { get; set; }

    public int SenderId { get; set; }

    public bool IsFromAdmin { get; set; }

    public string Body { get; set; } = string.Empty;

    public Ticket Ticket { get; set; } = null!;

    public User Sender { get; set; } = null!;
}
