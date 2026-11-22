namespace MicroNamaa.Application.DTOs.Reports;

public record CategorySalesDto(int? CategoryId, string Name, int Units, decimal Revenue, decimal? SharePercent);
