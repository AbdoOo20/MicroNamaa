namespace MicroNamaa.Application.DTOs.Reports;

/// <summary>Libyan dates (both included); the last 30 days when empty. BranchId narrows to one branch.</summary>
public class ReportQuery
{
    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int? BranchId { get; set; }
}
