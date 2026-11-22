using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Parties;

/// <summary>The cards of the suppliers screen: money paid to suppliers, how many there are, and their purchase invoices.</summary>
public record SupplierSummaryDto(decimal TotalPayments, int NumberOfSuppliers, int TotalInvoices, decimal TotalOwed, IReadOnlyList<SummaryCount> Tabs);
