namespace MicroNamaa.Domain.Enums;

/// <summary>Who sees a notification: one user, the owners and managers of a merchant, or every platform admin.</summary>
public enum NotificationAudience
{
    User = 1,

    Merchant = 2,

    Admins = 3
}
