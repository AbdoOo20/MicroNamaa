using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Purchases;

public record PurchaseOrderDto(
    long Id,
    string Code,
    int SupplierId,
    string SupplierName,
    string? SupplierLogoUrl,
    int WarehouseId,
    string WarehouseName,
    DateTime OrderDate,
    DateTime? ExpectedAt,
    PurchaseOrderStatus Status,
    string? PaymentTerms,
    DateTime? DueDate,
    decimal Subtotal,
    decimal Discount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal ShippingCost,
    decimal Total,
    decimal PaidAmount,
    PaymentMethod? PaymentMethod,
    string? Note,
    IReadOnlyList<PurchaseOrderItemDto> Items,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    string? CancelReason);
