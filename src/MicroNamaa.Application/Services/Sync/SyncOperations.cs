using Microsoft.Extensions.DependencyInjection;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Catalog;

using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Employees;

using MicroNamaa.Application.DTOs.Inventory;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Application.DTOs.Parties;

using MicroNamaa.Application.DTOs.Purchases;

using MicroNamaa.Application.DTOs.Sales;

using MicroNamaa.Application.Services.Catalog;

using MicroNamaa.Application.Services.Employees;

using MicroNamaa.Application.Services.Inventory;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Application.Services.Parties;

using MicroNamaa.Application.Services.Purchases;

using MicroNamaa.Application.Services.Sales;

namespace MicroNamaa.Application.Services.Sync;

/// <summary>
/// Every operation the device can do offline and send later. Each one calls the same service as its online endpoint,
/// so the rules are the same; only a sale with OccurredAt is recorded as an offline sale.
/// Images are not synced (upload them when online).
/// </summary>
public static class SyncOperations
{
    public static readonly IReadOnlyDictionary<string, SyncOperationDefinition> All = new List<SyncOperationDefinition>
    {
        Define("customers.create", SyncAccess.Store, "Customer", false, (ICustomerService service, SyncOperationContext op, CustomerRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("customers.update", SyncAccess.Store, "Customer", true, (ICustomerService service, SyncOperationContext op, CustomerRequest body, CancellationToken ct) => service.UpdateAsync(op.Id, body, ct)),
        Define("customers.delete", SyncAccess.Managers, "Customer", (ICustomerService service, SyncOperationContext op, CancellationToken ct) => service.DeleteAsync(op.Id, ct)),
        Define("customers.payment", SyncAccess.Store, null, true, (ICustomerService service, SyncOperationContext op, PaymentRequest body, CancellationToken ct) => service.ReceivePaymentAsync(op.Id, body, ct)),
        Define("customers.adjust", SyncAccess.Managers, null, true, (ICustomerService service, SyncOperationContext op, BalanceAdjustmentRequest body, CancellationToken ct) => service.AdjustBalanceAsync(op.Id, body, ct)),

        Define("suppliers.create", SyncAccess.Managers, "Supplier", false, (ISupplierService service, SyncOperationContext op, SupplierRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("suppliers.update", SyncAccess.Managers, "Supplier", true, (ISupplierService service, SyncOperationContext op, SupplierRequest body, CancellationToken ct) => service.UpdateAsync(op.Id, body, ct)),
        Define("suppliers.delete", SyncAccess.Managers, "Supplier", (ISupplierService service, SyncOperationContext op, CancellationToken ct) => service.DeleteAsync(op.Id, ct)),
        Define("suppliers.payment", SyncAccess.Managers, null, true, (ISupplierService service, SyncOperationContext op, PaymentRequest body, CancellationToken ct) => service.PayAsync(op.Id, body, ct)),
        Define("suppliers.adjust", SyncAccess.Managers, null, true, (ISupplierService service, SyncOperationContext op, BalanceAdjustmentRequest body, CancellationToken ct) => service.AdjustBalanceAsync(op.Id, body, ct)),

        Define("categories.create", SyncAccess.Managers, "Category", false, (ICategoryService service, SyncOperationContext op, CategoryRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("categories.update", SyncAccess.Managers, "Category", true, (ICategoryService service, SyncOperationContext op, CategoryRequest body, CancellationToken ct) => service.UpdateAsync(op.Id, body, ct)),
        Define("categories.delete", SyncAccess.Managers, "Category", (ICategoryService service, SyncOperationContext op, CancellationToken ct) => service.DeleteAsync(op.Id, ct)),

        Define("products.create", SyncAccess.Managers, "Product", false, (IProductService service, SyncOperationContext op, CreateProductRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("products.update", SyncAccess.Managers, "Product", true, (IProductService service, SyncOperationContext op, ProductRequest body, CancellationToken ct) => service.UpdateAsync(op.Id, body, ct)),
        Define("products.delete", SyncAccess.Managers, "Product", (IProductService service, SyncOperationContext op, CancellationToken ct) => service.DeleteAsync(op.Id, ct)),

        Define("warehouses.create", SyncAccess.Managers, "Warehouse", false, (IWarehouseService service, SyncOperationContext op, WarehouseRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("warehouses.update", SyncAccess.Managers, "Warehouse", true, (IWarehouseService service, SyncOperationContext op, WarehouseRequest body, CancellationToken ct) => service.UpdateAsync(op.Id, body, ct)),
        Define("warehouses.delete", SyncAccess.Managers, "Warehouse", (IWarehouseService service, SyncOperationContext op, CancellationToken ct) => service.DeleteAsync(op.Id, ct)),

        Define("promotions.create", SyncAccess.Managers, "Promotion", false, (IPromotionService service, SyncOperationContext op, PromotionRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("promotions.update", SyncAccess.Managers, "Promotion", true, (IPromotionService service, SyncOperationContext op, PromotionRequest body, CancellationToken ct) => service.UpdateAsync(op.Id, body, ct)),
        Define("promotions.delete", SyncAccess.Managers, "Promotion", (IPromotionService service, SyncOperationContext op, CancellationToken ct) => service.DeleteAsync(op.Id, ct)),

        Define("branches.create", SyncAccess.Owner, "Branch", false, (IBranchService service, SyncOperationContext op, BranchRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("branches.update", SyncAccess.Managers, "Branch", true, (IBranchService service, SyncOperationContext op, BranchRequest body, CancellationToken ct) => service.UpdateAsync(op.Id, body, ct)),
        Define("branches.delete", SyncAccess.Owner, "Branch", (IBranchService service, SyncOperationContext op, CancellationToken ct) => service.DeleteAsync(op.Id, ct)),

        Define("employees.create", SyncAccess.Managers, "Employee", false, (IEmployeeService service, SyncOperationContext op, CreateEmployeeRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("employees.update", SyncAccess.Managers, "Employee", true, (IEmployeeService service, SyncOperationContext op, UpdateEmployeeRequest body, CancellationToken ct) => service.UpdateAsync(op.Id, body, ct)),
        Define("employees.delete", SyncAccess.Managers, "Employee", (IEmployeeService service, SyncOperationContext op, CancellationToken ct) => service.DeleteAsync(op.Id, ct)),

        Define("settings.update", SyncAccess.Owner, null, false, (IMerchantSettingsService service, SyncOperationContext op, UpdateMerchantSettingsRequest body, CancellationToken ct) => service.UpdateAsync(body, ct)),

        Define("sales.invoice.create", SyncAccess.Store, "SalesInvoice", false, (ISalesInvoiceService service, SyncOperationContext op, SaveSalesInvoiceRequest body, CancellationToken ct) => op.OccurredAt is { } at
            ? service.CreateOfflineAsync(body, new OfflineSale(at > DateTime.UtcNow ? DateTime.UtcNow : DateTime.SpecifyKind(at, DateTimeKind.Utc), op.ClientKey), ct)
            : service.CreateAsync(body, ct)),
        Define("sales.invoice.update", SyncAccess.Store, "SalesInvoice", true, (ISalesInvoiceService service, SyncOperationContext op, SaveSalesInvoiceRequest body, CancellationToken ct) => service.UpdateAsync(op.LongId, body, ct)),
        Define("sales.invoice.complete", SyncAccess.Store, "SalesInvoice", true, (ISalesInvoiceService service, SyncOperationContext op, CompleteSalesInvoiceRequest body, CancellationToken ct) => service.CompleteAsync(op.LongId, body, ct)),
        Define("sales.invoice.cancel", SyncAccess.Managers, "SalesInvoice", true, (ISalesInvoiceService service, SyncOperationContext op, ReasonRequest body, CancellationToken ct) => service.CancelAsync(op.LongId, body, ct)),
        Define("sales.invoice.delete", SyncAccess.Store, "SalesInvoice", (ISalesInvoiceService service, SyncOperationContext op, CancellationToken ct) => service.DeleteDraftAsync(op.LongId, ct)),

        Define("sales.return.create", SyncAccess.Store, "SalesReturn", false, (ISalesReturnService service, SyncOperationContext op, CreateSalesReturnRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),

        Define("purchases.create", SyncAccess.Managers, "PurchaseOrder", false, (IPurchaseOrderService service, SyncOperationContext op, SavePurchaseOrderRequest body, CancellationToken ct) => service.CreateAsync(body, ct)),
        Define("purchases.update", SyncAccess.Managers, "PurchaseOrder", true, (IPurchaseOrderService service, SyncOperationContext op, SavePurchaseOrderRequest body, CancellationToken ct) => service.UpdateAsync(op.LongId, body, ct)),
        Define("purchases.status", SyncAccess.Managers, "PurchaseOrder", true, (IPurchaseOrderService service, SyncOperationContext op, UpdatePurchaseStatusRequest body, CancellationToken ct) => service.UpdateStatusAsync(op.LongId, body, ct)),
        Define("purchases.deliver", SyncAccess.Managers, "PurchaseOrder", true, (IPurchaseOrderService service, SyncOperationContext op, DeliverPurchaseOrderRequest body, CancellationToken ct) => service.DeliverAsync(op.LongId, body, ct)),
        Define("purchases.cancel", SyncAccess.Managers, "PurchaseOrder", true, (IPurchaseOrderService service, SyncOperationContext op, ReasonRequest body, CancellationToken ct) => service.CancelAsync(op.LongId, body, ct)),
        Define("purchases.delete", SyncAccess.Managers, "PurchaseOrder", (IPurchaseOrderService service, SyncOperationContext op, CancellationToken ct) => service.DeleteDraftAsync(op.LongId, ct)),

        Define("inventory.adjust", SyncAccess.Managers, null, false, (IInventoryService service, SyncOperationContext op, StockAdjustmentRequest body, CancellationToken ct) => service.AdjustAsync(body, ct)),
        Define("inventory.transfer", SyncAccess.Managers, null, false, (IInventoryService service, SyncOperationContext op, StockTransferRequest body, CancellationToken ct) => service.TransferAsync(body, ct))
    }.ToDictionary(definition => definition.Type, StringComparer.OrdinalIgnoreCase);

    private static SyncOperationDefinition Define<TService, TBody, TResult>(string type, SyncAccess access, string? entityType, bool needsTarget, Func<TService, SyncOperationContext, TBody, CancellationToken, Task<TResult>> run)
        where TService : notnull
        where TBody : class
        where TResult : Result =>
        new(type, access, entityType, needsTarget, typeof(TBody).Name, async (services, op, ct) => await run(services.GetRequiredService<TService>(), op, op.Body<TBody>(), ct));

    /// <summary>An operation on a target with no body (delete).</summary>
    private static SyncOperationDefinition Define<TService, TResult>(string type, SyncAccess access, string? entityType, Func<TService, SyncOperationContext, CancellationToken, Task<TResult>> run)
        where TService : notnull
        where TResult : Result =>
        new(type, access, entityType, true, null, async (services, op, ct) => await run(services.GetRequiredService<TService>(), op, ct));
}
