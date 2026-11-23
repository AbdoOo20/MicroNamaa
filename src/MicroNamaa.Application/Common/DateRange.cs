namespace MicroNamaa.Application.Common;

/// <summary>A UTC range [From, To) and the range of the same length just before it (for "vs last period").</summary>
public record DateRange(DateTime From, DateTime To)
{
    public DateRange Previous => new(From - (To - From), From);

    /// <summary>From and To are Libyan dates (the To day is included). Defaults to the last 30 days.</summary>
    public static DateRange FromLocalDates(DateOnly? from, DateOnly? to, int defaultDays = 30)
    {
        var lastDay = to ?? DateOnly.FromDateTime(LibyaTime.Now);

        var firstDay = from ?? lastDay.AddDays(-(defaultDays - 1));

        if (firstDay > lastDay)
        {
            (firstDay, lastDay) = (lastDay, firstDay);
        }

        return new DateRange(LibyaTime.ToUtc(firstDay.ToDateTime(TimeOnly.MinValue)), LibyaTime.ToUtc(lastDay.AddDays(1).ToDateTime(TimeOnly.MinValue)));
    }
}
