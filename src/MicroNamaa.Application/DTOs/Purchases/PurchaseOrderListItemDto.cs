using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Purchases;

public record PurchaseOrderListItemDto(long Id, string Code, int SupplierId, string SupplierName, string? SupplierLogoUrl, string WarehouseName, DateTime OrderDate, PurchaseOrderStatus Status, int ItemsCount, int UnitsCount, decimal Total, decimal PaidAmount, string? PaymentTerms, DateTime? DueDate, DateTime CreatedAt);
