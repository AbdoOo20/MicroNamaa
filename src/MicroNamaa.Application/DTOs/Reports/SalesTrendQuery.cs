namespace MicroNamaa.Application.DTOs.Reports;

public class SalesTrendQuery : ReportQuery
{
    /// <summary>day or month (default day).</summary>
    public string? GroupBy { get; set; }
}
