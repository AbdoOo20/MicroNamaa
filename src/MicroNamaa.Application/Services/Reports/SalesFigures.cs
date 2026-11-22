using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Reports;

/// <summary>
/// The sales queries shared by the dashboard and the reports. A sale counts on the day it was completed, a return on
/// the day it was taken, so a period's revenue is its completed invoices minus its returns.
/// </summary>
public class SalesFigures(IUnitOfWork unitOfWork)
{
    public record Totals(int Orders, decimal Sales, decimal Tax, decimal Discounts, decimal Cost, decimal Unpaid, decimal Returns, decimal ReturnsWithoutTax, decimal ReturnsCost, int Units)
    {
        public decimal Revenue => Money.Round(Sales - Returns);

        public decimal GrossProfit => Money.Round(Sales - Tax - Cost - (ReturnsWithoutTax - ReturnsCost));
    }

    public IQueryable<SalesInvoice> Completed(DateRange range, int? branchId)
    {
        var invoices = unitOfWork.Repository<SalesInvoice>().Query().Where(invoice => invoice.Status == SalesInvoiceStatus.Completed && invoice.CompletedAt >= range.From && invoice.CompletedAt < range.To);

        return branchId is { } branch ? invoices.Where(invoice => invoice.BranchId == branch) : invoices;
    }

    public IQueryable<SalesReturn> Returns(DateRange range, int? branchId)
    {
        var returns = unitOfWork.Repository<SalesReturn>().Query().Where(item => item.CreatedAt >= range.From && item.CreatedAt < range.To);

        return branchId is { } branch ? returns.Where(item => item.BranchId == branch) : returns;
    }

    public async Task<Totals> TotalsAsync(DateRange range, int? branchId, CancellationToken cancellationToken)
    {
        var sales = (await Completed(range, branchId)
            .GroupBy(invoice => 1)
            .Select(group => new
            {
                Orders = group.Count(),

                Sales = group.Sum(invoice => invoice.Total),

                Tax = group.Sum(invoice => invoice.TaxAmount),

                Discounts = group.Sum(invoice => invoice.ItemsDiscount + invoice.OrderDiscount + invoice.PointsDiscount),

                Cost = group.Sum(invoice => invoice.TotalCost),

                Unpaid = group.Sum(invoice => invoice.DueAmount)
            })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        var units = await unitOfWork.Repository<SalesInvoiceItem>().Query()
            .Where(item => item.SalesInvoice.Status == SalesInvoiceStatus.Completed && item.SalesInvoice.CompletedAt >= range.From && item.SalesInvoice.CompletedAt < range.To && (branchId == null || item.SalesInvoice.BranchId == branchId))
            .SumAsync(item => (int?)(item.Quantity - item.ReturnedQuantity), cancellationToken) ?? 0;

        var returns = (await Returns(range, branchId)
            .GroupBy(item => 1)
            .Select(group => new
            {
                Total = group.Sum(item => item.Total),

                WithoutTax = group.Sum(item => item.SalesInvoice.Total == 0 ? 0 : item.Total * (item.SalesInvoice.Total - item.SalesInvoice.TaxAmount) / item.SalesInvoice.Total),

                Cost = group.Sum(item => item.TotalCost)
            })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        return new Totals(sales?.Orders ?? 0, sales?.Sales ?? 0, sales?.Tax ?? 0, sales?.Discounts ?? 0, sales?.Cost ?? 0, sales?.Unpaid ?? 0, returns?.Total ?? 0, returns?.WithoutTax ?? 0, returns?.Cost ?? 0, units);
    }

    /// <summary>Net revenue and orders per Libyan day or month, with empty periods filled in.</summary>
    public async Task<IReadOnlyList<(DateOnly Start, decimal Revenue, int Orders)>> TrendAsync(DateRange range, int? branchId, bool byMonth, CancellationToken cancellationToken)
    {
        const int offset = LibyaTime.OffsetHours;

        List<(DateOnly Day, decimal Amount, int Orders)> sales;

        List<(DateOnly Day, decimal Amount)> returns;

        if (byMonth)
        {
            sales = (await Completed(range, branchId)
                    .GroupBy(invoice => new { invoice.CompletedAt!.Value.AddHours(offset).Year, invoice.CompletedAt!.Value.AddHours(offset).Month })
                    .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(invoice => invoice.Total), Orders = group.Count() })
                    .ToListAsync(cancellationToken))
                .Select(row => (new DateOnly(row.Year, row.Month, 1), row.Amount, row.Orders))
                .ToList();

            returns = (await Returns(range, branchId)
                    .GroupBy(item => new { item.CreatedAt.AddHours(offset).Year, item.CreatedAt.AddHours(offset).Month })
                    .Select(group => new { group.Key.Year, group.Key.Month, Amount = group.Sum(item => item.Total) })
                    .ToListAsync(cancellationToken))
                .Select(row => (new DateOnly(row.Year, row.Month, 1), row.Amount))
                .ToList();
        }

        else
        {
            sales = (await Completed(range, branchId)
                    .GroupBy(invoice => invoice.CompletedAt!.Value.AddHours(offset).Date)
                    .Select(group => new { Day = group.Key, Amount = group.Sum(invoice => invoice.Total), Orders = group.Count() })
                    .ToListAsync(cancellationToken))
                .Select(row => (DateOnly.FromDateTime(row.Day), row.Amount, row.Orders))
                .ToList();

            returns = (await Returns(range, branchId)
                    .GroupBy(item => item.CreatedAt.AddHours(offset).Date)
                    .Select(group => new { Day = group.Key, Amount = group.Sum(item => item.Total) })
                    .ToListAsync(cancellationToken))
                .Select(row => (DateOnly.FromDateTime(row.Day), row.Amount))
                .ToList();
        }

        var first = DateOnly.FromDateTime(LibyaTime.ToLocal(range.From));

        var last = DateOnly.FromDateTime(LibyaTime.ToLocal(range.To).AddTicks(-1));

        var periods = new List<DateOnly>();

        for (var period = byMonth ? new DateOnly(first.Year, first.Month, 1) : first; period <= last; period = byMonth ? period.AddMonths(1) : period.AddDays(1))
        {
            periods.Add(period);
        }

        return periods
            .Select(period => (period, Money.Round(sales.Where(row => row.Day == period).Sum(row => row.Amount) - returns.Where(row => row.Day == period).Sum(row => row.Amount)), sales.Where(row => row.Day == period).Sum(row => row.Orders)))
            .ToList();
    }

    public async Task<IReadOnlyList<(int ProductId, int Units, decimal Revenue)>> TopProductsAsync(DateRange range, int? branchId, int limit, CancellationToken cancellationToken)
    {
        var rows = await unitOfWork.Repository<SalesInvoiceItem>().Query()
            .Where(item => item.SalesInvoice.Status == SalesInvoiceStatus.Completed && item.SalesInvoice.CompletedAt >= range.From && item.SalesInvoice.CompletedAt < range.To && (branchId == null || item.SalesInvoice.BranchId == branchId))
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,

                Units = group.Sum(item => item.Quantity - item.ReturnedQuantity),

                Revenue = group.Sum(item => item.Quantity == 0 ? 0 : item.LineTotal * (item.Quantity - item.ReturnedQuantity) / item.Quantity)
            })
            .Where(row => row.Units > 0)
            .OrderByDescending(row => row.Units)
            .ThenByDescending(row => row.Revenue)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(row => (row.ProductId, row.Units, Money.Round(row.Revenue))).ToList();
    }
}
