using Microsoft.Extensions.DependencyInjection;

using MicroNamaa.Application.Services.Admin;

using MicroNamaa.Application.Services.Audit;

using MicroNamaa.Application.Services.Catalog;

using MicroNamaa.Application.Services.Common;

using MicroNamaa.Application.Services.Files;

using MicroNamaa.Application.Services.Inventory;

using MicroNamaa.Application.Services.Lookups;

using MicroNamaa.Application.Services.Maintenance;

using MicroNamaa.Application.Services.MerchantApplications;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Application.Services.Parties;

using MicroNamaa.Application.Services.Purchases;

using MicroNamaa.Application.Services.Reports;

using MicroNamaa.Application.Services.Sales;

using MicroNamaa.Application.Services.Staff;

using MicroNamaa.Application.Services.Support;

using MicroNamaa.Application.Services.Sync;

namespace MicroNamaa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<MerchantContext>();

        services.AddScoped<StockLedger>();

        services.AddScoped<BalanceLedger>();

        services.AddScoped<SalesFigures>();

        services.AddScoped<ImageUploads>();

        services.AddScoped<IMerchantSettingsService, MerchantSettingsService>();

        services.AddScoped<IBranchService, BranchService>();

        services.AddScoped<IPromotionService, PromotionService>();

        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<IWarehouseService, WarehouseService>();

        services.AddScoped<IInventoryService, InventoryService>();

        services.AddScoped<ICustomerService, CustomerService>();

        services.AddScoped<ISupplierService, SupplierService>();

        services.AddScoped<IPosService, PosService>();

        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();

        services.AddScoped<ISalesReturnService, SalesReturnService>();

        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<IAdminMerchantService, AdminMerchantService>();

        services.AddScoped<IMerchantApplicationService, MerchantApplicationService>();

        services.AddScoped<IImageService, ImageService>();

        services.AddScoped<ISyncService, SyncService>();

        services.AddScoped<ITicketService, TicketService>();

        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddScoped<IStaffAccessService, StaffAccessService>();

        services.AddSingleton<ILookupService, LookupService>();

        services.AddScoped<IMaintenanceService, MaintenanceService>();

        return services;
    }
}
