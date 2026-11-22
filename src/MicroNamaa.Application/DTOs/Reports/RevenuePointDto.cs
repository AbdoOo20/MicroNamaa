namespace MicroNamaa.Application.DTOs.Reports;

/// <summary>One bar of a revenue chart: a Libyan day (yyyy-MM-dd) or month (yyyy-MM).</summary>
public record RevenuePointDto(string Period, string Label, decimal Revenue, int Orders);
