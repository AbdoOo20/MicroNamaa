namespace MicroNamaa.Application.Common;

/// <summary>
/// Libya is UTC+2 all year (no daylight saving). Dates are stored in UTC; days, months and "this month" follow Libyan time.
/// </summary>
public static class LibyaTime
{
    public const int OffsetHours = 2;

    public static readonly TimeSpan Offset = TimeSpan.FromHours(OffsetHours);

    public static DateTime Now => DateTime.UtcNow + Offset;

    public static DateTime ToLocal(DateTime utc) => utc + Offset;

    public static DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local - Offset, DateTimeKind.Utc);

    /// <summary>The UTC moment the current Libyan day started.</summary>
    public static DateTime TodayStartUtc => ToUtc(Now.Date);

    /// <summary>The UTC moment the current Libyan month started.</summary>
    public static DateTime MonthStartUtc => ToUtc(new DateTime(Now.Year, Now.Month, 1));

    public static DateTime MonthStartUtcOf(DateTime localDate) => ToUtc(new DateTime(localDate.Year, localDate.Month, 1));
}
