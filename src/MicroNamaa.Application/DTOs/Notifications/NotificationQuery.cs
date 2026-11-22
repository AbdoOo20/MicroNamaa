using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Notifications;

public class NotificationQuery : PagedQuery
{
    public bool UnreadOnly { get; set; }
}
