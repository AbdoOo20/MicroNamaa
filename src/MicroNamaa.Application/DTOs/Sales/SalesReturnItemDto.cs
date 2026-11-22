namespace MicroNamaa.Application.DTOs.Sales;

public record SalesReturnItemDto(long Id, long SalesInvoiceItemId, int ProductId, string ProductName, int Quantity, decimal UnitRefund, decimal Total, bool Restock);
