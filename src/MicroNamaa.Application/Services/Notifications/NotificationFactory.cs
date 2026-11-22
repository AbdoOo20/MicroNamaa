using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Notifications;

/// <summary>Builds notifications. They are pushed over SignalR automatically after they are saved.</summary>
public static class NotificationFactory
{
    public static Notification ForUser(int userId, string type, string title, string? body = null, string? relatedType = null, long? relatedId = null) => new()
    {
        Audience = NotificationAudience.User,

        UserId = userId,

        Type = type,

        Title = title,

        Body = body,

        RelatedType = relatedType,

        RelatedId = relatedId
    };

    /// <summary>For the owners and managers of a merchant (cashiers do not get store alerts).</summary>
    public static Notification ForMerchant(int merchantId, string type, string title, string? body = null, string? relatedType = null, long? relatedId = null) => new()
    {
        Audience = NotificationAudience.Merchant,

        MerchantId = merchantId,

        Type = type,

        Title = title,

        Body = body,

        RelatedType = relatedType,

        RelatedId = relatedId
    };

    public static Notification ForAdmins(string type, string title, string? body = null, string? relatedType = null, long? relatedId = null, int? merchantId = null) => new()
    {
        Audience = NotificationAudience.Admins,

        MerchantId = merchantId,

        Type = type,

        Title = title,

        Body = body,

        RelatedType = relatedType,

        RelatedId = relatedId
    };
}
