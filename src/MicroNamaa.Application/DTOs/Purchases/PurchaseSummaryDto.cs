using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Purchases;

/// <summary>The cards of the purchases screen: the cost of every order that is not a draft or cancelled, the suppliers, and the orders.</summary>
public record PurchaseSummaryDto(decimal TotalCost, int TotalSuppliers, int TotalInvoices, decimal AwaitingDelivery, IReadOnlyList<SummaryCount> Tabs);
