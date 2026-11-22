using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Parties;

/// <summary>The cards of the customers screen: customers, their sales (net of returns) and what they owe.</summary>
public record CustomerSummaryDto(int TotalCustomers, decimal TotalSales, decimal OutstandingBalances, IReadOnlyList<SummaryCount> Tabs);
