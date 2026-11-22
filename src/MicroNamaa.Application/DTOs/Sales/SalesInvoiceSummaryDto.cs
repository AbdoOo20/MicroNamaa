using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Sales;

/// <summary>Totals of the completed invoices that match the filters (net of returns), and the tab counts.</summary>
public record SalesInvoiceSummaryDto(int Invoices, decimal Sales, decimal Returns, decimal Unpaid, decimal AverageInvoice, IReadOnlyList<SummaryCount> Tabs);
