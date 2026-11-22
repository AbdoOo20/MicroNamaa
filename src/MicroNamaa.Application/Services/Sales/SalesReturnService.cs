using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Sales;

using MicroNamaa.Application.Services.Inventory;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Application.Services.Parties;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Sales;

public class SalesReturnService(IUnitOfWork unitOfWork, IUserAppService userAppService, IDocumentNumberService numbers, StockLedger stockLedger, BalanceLedger balanceLedger) : ISalesReturnService
{
    private const string NotFound = "Return was not found.";

    public async Task<Result<PagedResult<SalesReturnListItemDto>>> GetAllAsync(SalesReturnQuery query, CancellationToken cancellationToken = default)
    {
        var page = await Filter(query)
            .OrderByDescending(item => item.Id)
            .ToPagedResultAsync(
                pageQuery => pageQuery.Select(item => new
                {
                    item.Id,

                    item.Number,

                    item.SalesInvoiceId,

                    InvoiceNumber = item.SalesInvoice.Number,

                    CustomerName = item.Customer == null ? null : item.Customer.Name,

                    BranchName = item.Branch.Name,

                    item.RefundMethod,

                    Units = item.Items.Sum(line => (int?)line.Quantity) ?? 0,

                    item.Total,

                    item.CreatedAt
                }),
                query.Page,
                query.PageSize,
                cancellationToken);

        var items = page.Items.Select(row => new SalesReturnListItemDto(row.Id, SalesReturn.FormatCode(row.Number), row.SalesInvoiceId, SalesInvoice.FormatCode(row.InvoiceNumber), row.CustomerName, row.BranchName, row.RefundMethod, row.Units, row.Total, row.CreatedAt)).ToList();

        return Result<PagedResult<SalesReturnListItemDto>>.Ok(new PagedResult<SalesReturnListItemDto>(items, page.Page, page.PageSize, page.TotalCount));
    }

    public async Task<Result<SalesReturnDto>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var item = await LoadAsync(id, cancellationToken);

        return item is null ? Result<SalesReturnDto>.NotFound(NotFound) : Result<SalesReturnDto>.Ok(item);
    }

    public async Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await unitOfWork.Repository<SalesInvoice>().Query(asNoTracking: false, item => item.Items).FirstOrDefaultAsync(item => item.Id == request.SalesInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result<SalesReturnDto>.NotFound("Invoice was not found.");
        }

        if (userAppService.IsCashier && invoice.BranchId != userAppService.BranchId)
        {
            return Result<SalesReturnDto>.Forbidden("Cashiers can only take returns for their own branch.");
        }

        if (invoice.Status != SalesInvoiceStatus.Completed)
        {
            return Result<SalesReturnDto>.Conflict("Only a completed invoice can have returns.");
        }

        if (request.RefundMethod == RefundMethod.CustomerBalance && invoice.CustomerId is null)
        {
            return Result<SalesReturnDto>.BadRequest("This invoice has no customer, so the refund cannot go to a balance.");
        }

        var lines = request.Items.GroupBy(item => (item.SalesInvoiceItemId, item.Restock)).Select(group => (group.Key.SalesInvoiceItemId, group.Key.Restock, Quantity: group.Sum(item => item.Quantity))).ToList();

        foreach (var group in lines.GroupBy(line => line.SalesInvoiceItemId))
        {
            var item = invoice.Items.FirstOrDefault(row => row.Id == group.Key);

            if (item is null)
            {
                return Result<SalesReturnDto>.BadRequest("An item does not belong to this invoice.");
            }

            var left = item.Quantity - item.ReturnedQuantity;

            if (group.Sum(line => line.Quantity) > left)
            {
                return Result<SalesReturnDto>.Conflict($"Only {left} of \"{item.ProductName}\" can still be returned.");
            }
        }

        // Every discount and the tax are shared out over the lines, so a unit refunds what the customer really paid for it.
        var linesTotal = invoice.Items.Sum(item => item.LineTotal);

        var factor = linesTotal == 0 ? 0 : invoice.Total / linesTotal;

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var salesReturn = new SalesReturn
        {
            Number = await numbers.NextAsync(invoice.MerchantId, DocumentType.SalesReturn, cancellationToken),

            SalesInvoiceId = invoice.Id,

            CustomerId = invoice.CustomerId,

            BranchId = invoice.BranchId,

            WarehouseId = invoice.WarehouseId,

            RefundMethod = request.RefundMethod,

            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim()
        };

        foreach (var line in lines)
        {
            var item = invoice.Items.First(row => row.Id == line.SalesInvoiceItemId);

            var unitRefund = item.Quantity == 0 ? 0 : Money.Round(item.LineTotal / item.Quantity * factor);

            salesReturn.Items.Add(new SalesReturnItem
            {
                MerchantId = invoice.MerchantId,

                SalesInvoiceItemId = item.Id,

                ProductId = item.ProductId,

                ProductName = item.ProductName,

                Quantity = line.Quantity,

                UnitRefund = unitRefund,

                Total = Money.Round(unitRefund * line.Quantity),

                Restock = line.Restock
            });

            item.ReturnedQuantity += line.Quantity;
        }

        salesReturn.Total = Money.Round(Math.Min(salesReturn.Items.Sum(item => item.Total), invoice.Total - invoice.ReturnedAmount));

        salesReturn.TotalCost = Money.Round(salesReturn.Items.Sum(line => line.Quantity * invoice.Items.First(row => row.Id == line.SalesInvoiceItemId).UnitCost));

        invoice.ReturnedAmount = Money.Round(invoice.ReturnedAmount + salesReturn.Total);

        await unitOfWork.Repository<SalesReturn>().AddAsync(salesReturn, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var changes = salesReturn.Items
            .Where(line => line.Restock)
            .Select(line => new StockChange(salesReturn.WarehouseId, line.ProductId, line.Quantity, StockMovementType.SaleReturn, invoice.Items.First(row => row.Id == line.SalesInvoiceItemId).UnitCost, RelatedTypes.SalesReturn, salesReturn.Id, salesReturn.Code, salesReturn.Reason))
            .ToList();

        var restocked = await stockLedger.ApplyAsync(changes, allowNegative: true, cancellationToken);

        if (!restocked.Succeeded)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<SalesReturnDto>.From(restocked);
        }

        if (invoice.CustomerId is { } customerId)
        {
            var customer = await unitOfWork.Repository<Customer>().Query(asNoTracking: false).FirstAsync(item => item.Id == customerId, cancellationToken);

            if (invoice.PointsEarned > 0 && invoice.Total > 0)
            {
                salesReturn.PointsReversed = Math.Min(customer.LoyaltyPoints, (int)Math.Floor(invoice.PointsEarned * salesReturn.Total / invoice.Total));

                customer.LoyaltyPoints -= salesReturn.PointsReversed;
            }

            if (request.RefundMethod == RefundMethod.CustomerBalance)
            {
                await balanceLedger.CustomerAsync(customer, BalanceEntryType.Return, -salesReturn.Total, salesReturn.Code, $"Return of {invoice.Code}", invoiceId: invoice.Id, returnId: salesReturn.Id, cancellationToken: cancellationToken);
            }
        }

        await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForMerchant(invoice.MerchantId, NotificationTypes.SalesReturnCreated, $"مرتجع جديد {salesReturn.Code}", $"من الفاتورة {invoice.Code} بقيمة {salesReturn.Total:0.###} د.ل", RelatedTypes.SalesReturn, salesReturn.Id), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<SalesReturnDto>.Created((await LoadAsync(salesReturn.Id, cancellationToken))!, "Return recorded.");
    }

    public async Task<Result<FileDownload>> ExportAsync(SalesReturnQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await Filter(query)
            .OrderByDescending(item => item.Id)
            .Take(10000)
            .Select(item => new { item.Number, InvoiceNumber = item.SalesInvoice.Number, Customer = item.Customer == null ? null : item.Customer.Name, Branch = item.Branch.Name, item.RefundMethod, Units = item.Items.Sum(line => (int?)line.Quantity) ?? 0, item.Total, item.Reason, item.CreatedAt })
            .ToListAsync(cancellationToken);

        var csv = new CsvFile("Return", "Invoice", "Customer", "Branch", "Refund", "Units", "Total (LYD)", "Reason", "Date");

        foreach (var row in rows)
        {
            csv.Row(SalesReturn.FormatCode(row.Number), SalesInvoice.FormatCode(row.InvoiceNumber), row.Customer, row.Branch, row.RefundMethod, row.Units, row.Total, row.Reason, row.CreatedAt);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("returns"));
    }

    private IQueryable<SalesReturn> Filter(SalesReturnQuery query)
    {
        var returns = unitOfWork.Repository<SalesReturn>().Query();

        if (userAppService.IsCashier && userAppService.BranchId is { } ownBranch)
        {
            returns = returns.Where(item => item.BranchId == ownBranch);
        }

        if (query.BranchId is { } branchId)
        {
            returns = returns.Where(item => item.BranchId == branchId);
        }

        if (query.CustomerId is { } customerId)
        {
            returns = returns.Where(item => item.CustomerId == customerId);
        }

        if (query.SalesInvoiceId is { } invoiceId)
        {
            returns = returns.Where(item => item.SalesInvoiceId == invoiceId);
        }

        if (query.RefundMethod is { } method)
        {
            returns = returns.Where(item => item.RefundMethod == method);
        }

        if (query.From is not null || query.To is not null)
        {
            var range = DateRange.FromLocalDates(query.From, query.To);

            returns = returns.Where(item => item.CreatedAt >= range.From && item.CreatedAt < range.To);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var digits = new string(query.Search.Where(char.IsAsciiDigit).ToArray());

            var number = long.TryParse(digits, out var parsed) ? parsed : -1;

            returns = returns.Where(item => item.Number == number || item.SalesInvoice.Number == number);
        }

        return returns;
    }

    private async Task<SalesReturnDto?> LoadAsync(long id, CancellationToken cancellationToken)
    {
        var users = unitOfWork.Repository<User>().Query();

        var row = await Filter(new SalesReturnQuery())
            .Where(item => item.Id == id)
            .Select(item => new
            {
                Return = item,

                InvoiceNumber = item.SalesInvoice.Number,

                CustomerName = item.Customer == null ? null : item.Customer.Name,

                BranchName = item.Branch.Name,

                WarehouseName = item.Warehouse.Name,

                CreatedBy = users.Where(user => user.Id == item.CreatedBy).Select(user => user.FullName).FirstOrDefault(),

                Items = item.Items.OrderBy(line => line.Id).Select(line => new SalesReturnItemDto(line.Id, line.SalesInvoiceItemId, line.ProductId, line.ProductName, line.Quantity, line.UnitRefund, line.Total, line.Restock)).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var entity = row.Return;

        return new SalesReturnDto(entity.Id, entity.Code, entity.SalesInvoiceId, SalesInvoice.FormatCode(row.InvoiceNumber), entity.CustomerId, row.CustomerName, entity.BranchId, row.BranchName, row.WarehouseName, entity.RefundMethod, entity.Total, entity.PointsReversed, entity.Reason, row.CreatedBy, row.Items, entity.CreatedAt);
    }
}
