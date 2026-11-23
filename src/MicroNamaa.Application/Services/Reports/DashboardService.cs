using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Reports;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Reports;

public class DashboardService(IUnitOfWork unitOfWork, MerchantContext context, SalesFigures figures) : IDashboardService
{
    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var monthStart = LibyaTime.MonthStartUtc;

        var previousStart = LibyaTime.MonthStartUtcOf(LibyaTime.Now.AddMonths(-1));

        // The same number of days of last month, so the 5th is compared with the 5th.
        var previousEnd = previousStart + (now - monthStart);

        var current = await figures.TotalsAsync(new DateRange(monthStart, now), null, cancellationToken);

        var previous = await figures.TotalsAsync(new DateRange(previousStart, previousEnd < monthStart ? previousEnd : monthStart), null, cancellationToken);

        var threshold = (await context.GetMerchantAsync(cancellationToken)).DefaultLowStockThreshold;

        var products = (await unitOfWork.Repository<Product>().Query()
            .Select(product => new { product.CreatedAt, Threshold = product.LowStockThreshold ?? threshold, Quantity = product.Stock.Sum(item => (int?)item.Quantity) ?? 0 })
            .GroupBy(product => 1)
            .Select(group => new
            {
                Total = group.Count(),

                New = group.Count(product => product.CreatedAt >= monthStart),

                Out = group.Count(product => product.Quantity <= 0),

                Low = group.Count(product => product.Quantity > 0 && product.Quantity <= product.Threshold)
            })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        var customers = await unitOfWork.Repository<Customer>().CountAsync(customer => customer.Status != RecordStatus.Archived, cancellationToken);

        var warehouses = await unitOfWork.Repository<Warehouse>().CountAsync(warehouse => warehouse.Status == RecordStatus.Active, cancellationToken);

        var awaiting = await unitOfWork.Repository<PurchaseOrder>().CountAsync(order => order.Status == PurchaseOrderStatus.Pending || order.Status == PurchaseOrderStatus.InTransit, cancellationToken);

        return Result<DashboardSummaryDto>.Ok(new DashboardSummaryDto(
            current.Revenue,
            Money.Growth(current.Revenue, previous.Revenue),
            current.Orders,
            products?.Total ?? 0,
            products?.New ?? 0,
            customers,
            warehouses,
            products?.Out ?? 0,
            products?.Low ?? 0,
            awaiting,
            Money.Currency));
    }

    public async Task<Result<IReadOnlyList<RevenuePointDto>>> GetRevenueAsync(string? range, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(LibyaTime.Now);

        var byMonth = range?.ToLowerInvariant() == "1y";

        var period = range?.ToLowerInvariant() switch
        {
            "1m" => DateRange.FromLocalDates(today.AddDays(-29), today),

            "1y" => DateRange.FromLocalDates(new DateOnly(today.Year, today.Month, 1).AddMonths(-11), today),

            _ => DateRange.FromLocalDates(today.AddDays(-6), today)
        };

        var trend = await figures.TrendAsync(period, null, byMonth, cancellationToken);

        var points = trend
            .Select(point => byMonth
                ? new RevenuePointDto(point.Start.ToString("yyyy-MM"), point.Start.ToString("MMM"), point.Revenue, point.Orders)
                : new RevenuePointDto(point.Start.ToString("yyyy-MM-dd"), range?.ToLowerInvariant() == "1m" ? point.Start.ToString("dd MMM") : point.Start.DayOfWeek.ToString()[..3], point.Revenue, point.Orders))
            .ToList();

        return Result<IReadOnlyList<RevenuePointDto>>.Ok(points);
    }

    public async Task<Result<IReadOnlyList<TopProductDto>>> GetTopProductsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var top = await figures.TopProductsAsync(new DateRange(LibyaTime.MonthStartUtc, DateTime.UtcNow), null, Math.Clamp(limit, 1, 50), cancellationToken);

        return Result<IReadOnlyList<TopProductDto>>.Ok(await ReportService.DescribeAsync(unitOfWork, top, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<ActivityDto>>> GetActivityAsync(int limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 50);

        var invoices = await unitOfWork.Repository<SalesInvoice>().Query()
            .Where(invoice => invoice.Status != SalesInvoiceStatus.Draft)
            .OrderByDescending(invoice => invoice.Id)
            .Take(take)
            .Select(invoice => new { invoice.Id, invoice.Number, invoice.Status, invoice.Total, Branch = invoice.Branch.Name, At = invoice.CancelledAt ?? invoice.CompletedAt ?? invoice.CreatedAt })
            .ToListAsync(cancellationToken);

        var purchases = await unitOfWork.Repository<PurchaseOrder>().Query()
            .Where(order => order.Status != PurchaseOrderStatus.Draft)
            .OrderByDescending(order => order.Id)
            .Take(take)
            .Select(order => new { order.Id, order.Number, order.Status, Supplier = order.Supplier.Name, At = order.DeliveredAt ?? order.CancelledAt ?? order.CreatedAt })
            .ToListAsync(cancellationToken);

        var returns = await unitOfWork.Repository<SalesReturn>().Query()
            .OrderByDescending(item => item.Id)
            .Take(take)
            .Select(item => new { item.Id, item.Number, item.Total, item.CreatedAt })
            .ToListAsync(cancellationToken);

        var merchantId = context.MerchantId;

        var alerts = await unitOfWork.Repository<Notification>().Query()
            .Where(notification => notification.Audience == NotificationAudience.Merchant && notification.MerchantId == merchantId && (notification.Type == NotificationTypes.LowStock || notification.Type == NotificationTypes.OutOfStock))
            .OrderByDescending(notification => notification.Id)
            .Take(take)
            .Select(notification => new { notification.Type, notification.Title, notification.RelatedId, notification.CreatedAt })
            .ToListAsync(cancellationToken);

        var sku = await unitOfWork.Repository<Product>().Query()
            .Where(product => alerts.Select(alert => alert.RelatedId).Contains(product.Id))
            .ToDictionaryAsync(product => (long)product.Id, product => product.Sku, cancellationToken);

        var activity = new List<ActivityDto>();

        activity.AddRange(invoices.Select(invoice => invoice.Status == SalesInvoiceStatus.Cancelled
            ? new ActivityDto("SalesInvoiceCancelled", $"Invoice cancelled at {invoice.Branch}", SalesInvoice.FormatCode(invoice.Number), "Cancelled", RelatedTypes.SalesInvoice, invoice.Id, invoice.At)
            : new ActivityDto("SalesInvoice", $"New invoice for {invoice.Total:0.###} LYD at {invoice.Branch}", SalesInvoice.FormatCode(invoice.Number), "Sent", RelatedTypes.SalesInvoice, invoice.Id, invoice.At)));

        activity.AddRange(purchases.Select(order => order.Status switch
        {
            PurchaseOrderStatus.Delivered => new ActivityDto("PurchaseOrderDelivered", $"Purchase order received from {order.Supplier}", PurchaseOrder.FormatCode(order.Number), "Delivered", RelatedTypes.PurchaseOrder, order.Id, order.At),

            PurchaseOrderStatus.Cancelled => new ActivityDto("PurchaseOrderCancelled", $"Purchase order cancelled ({order.Supplier})", PurchaseOrder.FormatCode(order.Number), "Cancelled", RelatedTypes.PurchaseOrder, order.Id, order.At),

            _ => new ActivityDto("PurchaseOrderCreated", $"New purchase order created ({order.Supplier})", PurchaseOrder.FormatCode(order.Number), "New", RelatedTypes.PurchaseOrder, order.Id, order.At)
        }));

        activity.AddRange(returns.Select(item => new ActivityDto("SalesReturn", $"Return of {item.Total:0.###} LYD", SalesReturn.FormatCode(item.Number), "Return", RelatedTypes.SalesReturn, item.Id, item.CreatedAt)));

        activity.AddRange(alerts.Select(alert => new ActivityDto(alert.Type, alert.Title, alert.RelatedId is { } id ? sku.GetValueOrDefault(id) : null, alert.Type == NotificationTypes.OutOfStock ? "Out" : "Low", RelatedTypes.Product, alert.RelatedId, alert.CreatedAt)));

        return Result<IReadOnlyList<ActivityDto>>.Ok(activity.OrderByDescending(item => item.At).Take(take).ToList());
    }
}
