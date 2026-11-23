using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Reports;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Reports;

public class ReportService(IUnitOfWork unitOfWork, MerchantContext context, SalesFigures figures) : IReportService
{
    public async Task<Result<ReportOverviewDto>> GetOverviewAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        var range = DateRange.FromLocalDates(query.From, query.To);

        var current = await figures.TotalsAsync(range, query.BranchId, cancellationToken);

        var previous = await figures.TotalsAsync(range.Previous, query.BranchId, cancellationToken);

        var average = current.Orders == 0 ? 0 : Money.Round(current.Revenue / current.Orders);

        var previousAverage = previous.Orders == 0 ? 0 : Money.Round(previous.Revenue / previous.Orders);

        var withoutTax = current.Sales - current.Tax - current.ReturnsWithoutTax;

        var newCustomers = await unitOfWork.Repository<Customer>().CountAsync(customer => customer.CreatedAt >= range.From && customer.CreatedAt < range.To, cancellationToken);

        return Result<ReportOverviewDto>.Ok(new ReportOverviewDto(
            range.From,
            range.To,
            current.Revenue,
            Money.Growth(current.Revenue, previous.Revenue),
            current.Orders,
            Money.Growth(current.Orders, previous.Orders),
            average,
            Money.Growth(average, previousAverage),
            current.Units,
            Money.Round(current.Returns),
            Money.Round(current.Discounts),
            Money.Round(current.Tax),
            current.GrossProfit,
            Money.Growth(current.GrossProfit, previous.GrossProfit),
            withoutTax <= 0 ? null : Math.Round(current.GrossProfit / withoutTax * 100m, 1),
            Money.Round(current.Unpaid),
            newCustomers));
    }

    public async Task<Result<IReadOnlyList<RevenuePointDto>>> GetSalesTrendAsync(SalesTrendQuery query, CancellationToken cancellationToken = default)
    {
        var byMonth = query.GroupBy?.ToLowerInvariant() == "month";

        var range = byMonth && query.From is null
            ? DateRange.FromLocalDates(new DateOnly(LibyaTime.Now.Year, LibyaTime.Now.Month, 1).AddMonths(-5), query.To)
            : DateRange.FromLocalDates(query.From, query.To);

        if (!byMonth && range.To - range.From > TimeSpan.FromDays(400))
        {
            return Result<IReadOnlyList<RevenuePointDto>>.BadRequest("Group by month for periods longer than a year.");
        }

        var trend = await figures.TrendAsync(range, query.BranchId, byMonth, cancellationToken);

        var points = trend
            .Select(point => new RevenuePointDto(point.Start.ToString(byMonth ? "yyyy-MM" : "yyyy-MM-dd"), point.Start.ToString(byMonth ? "MMM yyyy" : "dd MMM"), point.Revenue, point.Orders))
            .ToList();

        return Result<IReadOnlyList<RevenuePointDto>>.Ok(points);
    }

    public async Task<Result<IReadOnlyList<TopProductDto>>> GetTopProductsAsync(ReportQuery query, int limit, CancellationToken cancellationToken = default)
    {
        var top = await figures.TopProductsAsync(DateRange.FromLocalDates(query.From, query.To), query.BranchId, Math.Clamp(limit, 1, 100), cancellationToken);

        return Result<IReadOnlyList<TopProductDto>>.Ok(await DescribeAsync(unitOfWork, top, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<CategorySalesDto>>> GetCategoriesAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        var range = DateRange.FromLocalDates(query.From, query.To);

        var rows = await unitOfWork.Repository<SalesInvoiceItem>().Query()
            .Where(item => item.SalesInvoice.Status == SalesInvoiceStatus.Completed && item.SalesInvoice.CompletedAt >= range.From && item.SalesInvoice.CompletedAt < range.To && (query.BranchId == null || item.SalesInvoice.BranchId == query.BranchId))
            .GroupBy(item => item.Product.CategoryId)
            .Select(group => new
            {
                CategoryId = group.Key,

                Units = group.Sum(item => item.Quantity - item.ReturnedQuantity),

                Revenue = group.Sum(item => item.Quantity == 0 ? 0 : item.LineTotal * (item.Quantity - item.ReturnedQuantity) / item.Quantity)
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Where(row => row.CategoryId != null).Select(row => row.CategoryId!.Value).ToList();

        var names = await unitOfWork.Repository<Category>().Query().Where(category => ids.Contains(category.Id)).ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);

        var total = rows.Sum(row => row.Revenue);

        var result = rows
            .OrderByDescending(row => row.Revenue)
            .Select(row => new CategorySalesDto(row.CategoryId, row.CategoryId is { } id ? names.GetValueOrDefault(id, "—") : "Uncategorized", row.Units, Money.Round(row.Revenue), total == 0 ? null : Math.Round(row.Revenue * 100m / total, 1)))
            .ToList();

        return Result<IReadOnlyList<CategorySalesDto>>.Ok(result);
    }

    public async Task<Result<IReadOnlyList<EmployeeSalesDto>>> GetEmployeesAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        var range = DateRange.FromLocalDates(query.From, query.To);

        var monthStart = LibyaTime.MonthStartUtc;

        var inRange = await figures.Completed(range, query.BranchId)
            .GroupBy(invoice => invoice.CashierId)
            .Select(group => new { CashierId = group.Key, Orders = group.Count(), Revenue = group.Sum(invoice => invoice.Total - invoice.ReturnedAmount) })
            .ToListAsync(cancellationToken);

        var thisMonth = await figures.Completed(new DateRange(monthStart, DateTime.UtcNow), null)
            .GroupBy(invoice => invoice.CashierId)
            .Select(group => new { CashierId = group.Key, Revenue = group.Sum(invoice => invoice.Total - invoice.ReturnedAmount) })
            .ToDictionaryAsync(row => row.CashierId, row => row.Revenue, cancellationToken);

        var merchantId = context.MerchantId;

        var users = await unitOfWork.Repository<User>().Query()
            .Where(user => user.MerchantId == merchantId && (query.BranchId == null || user.BranchId == query.BranchId || inRange.Select(row => row.CashierId).Contains(user.Id)))
            .Select(user => new { user.Id, user.FullName, user.AvatarPath, user.UpdatedAt, user.CreatedAt, user.MonthlyTarget, BranchName = user.Branch == null ? null : user.Branch.Name })
            .ToListAsync(cancellationToken);

        var result = users
            .Select(user =>
            {
                var sales = inRange.FirstOrDefault(row => row.CashierId == user.Id);

                var month = Money.Round(thisMonth.GetValueOrDefault(user.Id));

                return new EmployeeSalesDto(
                    user.Id,
                    user.FullName,
                    FileUrls.UserAvatar(user.Id, user.AvatarPath, user.UpdatedAt ?? user.CreatedAt),
                    user.BranchName,
                    sales?.Orders ?? 0,
                    Money.Round(sales?.Revenue ?? 0),
                    month,
                    user.MonthlyTarget,
                    user.MonthlyTarget is > 0 ? Math.Round(month * 100m / user.MonthlyTarget.Value, 1) : null);
            })
            .OrderByDescending(row => row.Revenue)
            .ThenBy(row => row.FullName)
            .ToList();

        return Result<IReadOnlyList<EmployeeSalesDto>>.Ok(result);
    }

    public async Task<Result<IReadOnlyList<PaymentMethodSalesDto>>> GetPaymentMethodsAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await figures.Completed(DateRange.FromLocalDates(query.From, query.To), query.BranchId)
            .Where(invoice => invoice.PaymentMethod != null)
            .GroupBy(invoice => invoice.PaymentMethod!.Value)
            .Select(group => new { Method = group.Key, Orders = group.Count(), Amount = group.Sum(invoice => invoice.Total), Paid = group.Sum(invoice => invoice.PaidAmount), Due = group.Sum(invoice => invoice.DueAmount) })
            .ToListAsync(cancellationToken);

        var result = rows.OrderByDescending(row => row.Amount).Select(row => new PaymentMethodSalesDto(row.Method, row.Orders, Money.Round(row.Amount), Money.Round(row.Paid), Money.Round(row.Due))).ToList();

        return Result<IReadOnlyList<PaymentMethodSalesDto>>.Ok(result);
    }

    public async Task<Result<IReadOnlyList<BranchSalesDto>>> GetBranchesAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await figures.Completed(DateRange.FromLocalDates(query.From, query.To), null)
            .GroupBy(invoice => invoice.BranchId)
            .Select(group => new { BranchId = group.Key, Orders = group.Count(), Revenue = group.Sum(invoice => invoice.Total - invoice.ReturnedAmount) })
            .ToListAsync(cancellationToken);

        var branches = await unitOfWork.Repository<Branch>().Query().OrderBy(branch => branch.Id).Select(branch => new { branch.Id, branch.Name }).ToListAsync(cancellationToken);

        var result = branches
            .Select(branch => new BranchSalesDto(branch.Id, branch.Name, rows.FirstOrDefault(row => row.BranchId == branch.Id)?.Orders ?? 0, Money.Round(rows.FirstOrDefault(row => row.BranchId == branch.Id)?.Revenue ?? 0)))
            .OrderByDescending(row => row.Revenue)
            .ToList();

        return Result<IReadOnlyList<BranchSalesDto>>.Ok(result);
    }

    public async Task<Result<IReadOnlyList<WarehouseStockDto>>> GetWarehousesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await unitOfWork.Repository<Warehouse>().Query()
            .Where(warehouse => warehouse.Status != RecordStatus.Archived)
            .OrderBy(warehouse => warehouse.Id)
            .Select(warehouse => new
            {
                warehouse.Id,

                warehouse.Name,

                warehouse.Location,

                warehouse.Capacity,

                Products = warehouse.Stock.Count(item => item.Quantity > 0),

                Units = warehouse.Stock.Sum(item => (int?)item.Quantity) ?? 0,

                Cost = warehouse.Stock.Where(item => item.Quantity > 0).Sum(item => (decimal?)(item.Quantity * item.Product.Cost)) ?? 0,

                Retail = warehouse.Stock.Where(item => item.Quantity > 0).Sum(item => (decimal?)(item.Quantity * item.Product.Price)) ?? 0
            })
            .ToListAsync(cancellationToken);

        var result = rows
            .Select(row => new WarehouseStockDto(row.Id, row.Name, row.Location, row.Products, row.Units, Money.Round(row.Cost), Money.Round(row.Retail), row.Capacity > 0 ? Math.Round(Math.Max(row.Units, 0) * 100m / row.Capacity.Value, 1) : null))
            .ToList();

        return Result<IReadOnlyList<WarehouseStockDto>>.Ok(result);
    }

    public async Task<Result<FileDownload>> ExportAsync(ReportQuery query, CancellationToken cancellationToken = default)
    {
        var range = DateRange.FromLocalDates(query.From, query.To);

        var trend = await figures.TrendAsync(range, query.BranchId, byMonth: false, cancellationToken);

        var csv = new CsvFile("Date", "Orders", "Net revenue (LYD)");

        foreach (var point in trend)
        {
            csv.Row(point.Start.ToString("yyyy-MM-dd"), point.Orders, point.Revenue);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("sales-report"));
    }

    /// <summary>Adds the names, SKUs and images to (product, units, revenue) rows.</summary>
    internal static async Task<IReadOnlyList<TopProductDto>> DescribeAsync(IUnitOfWork unitOfWork, IReadOnlyList<(int ProductId, int Units, decimal Revenue)> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Select(row => row.ProductId).ToList();

        var products = await unitOfWork.Repository<Product>().Query()
            .IgnoreQueryFilters()
            .Where(product => ids.Contains(product.Id))
            .Select(product => new { product.Id, product.Name, product.Sku, product.ImagePath, product.UpdatedAt, product.CreatedAt })
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        return rows
            .Where(row => products.ContainsKey(row.ProductId))
            .Select(row =>
            {
                var product = products[row.ProductId];

                return new TopProductDto(product.Id, product.Name, product.Sku, FileUrls.Product(product.Id, product.ImagePath, product.UpdatedAt ?? product.CreatedAt), row.Units, row.Revenue);
            })
            .ToList();
    }
}
