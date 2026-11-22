namespace MicroNamaa.Application.Settings;

public class PlatformSettings
{
    public const string SectionName = "Platform";

    /// <summary>The name shown to merchants on support replies.</summary>
    public string SupportName { get; set; } = "فريق الدعم";

    /// <summary>The subscription length given to a new merchant when the admin does not set an end date.</summary>
    public int DefaultSubscriptionDays { get; set; } = 30;

    /// <summary>The owners get a reminder this many days before the subscription ends.</summary>
    public int SubscriptionReminderDays { get; set; } = 5;

    public int MaxUploadSizeMb { get; set; } = 5;
}
