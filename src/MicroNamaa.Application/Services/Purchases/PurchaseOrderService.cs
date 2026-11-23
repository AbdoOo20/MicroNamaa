using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Purchases;

using MicroNamaa.Application.Services.Inventory;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Application.Services.Parties;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Purchases;

public class PurchaseOrderService(IUnitOfWork unitOfWork, MerchantContext context, IDocumentNumberService numbers, StockLedger stockLedger, BalanceLedger balanceLedger) : IPurchaseOrderService
{
    private const string NotFound = "Purchase order was not found.";

    public async Task<Result<PagedResult<PurchaseOrderListItemDto>>> GetAllAsync(PurchaseOrderQuery query, CancellationToken cancellationToken = default)
    {
        var page = await Filter(query)
            .OrderByDescending(order => order.Id)
            .ToPagedResultAsync(
                pageQuery => pageQuery.Select(order => new PurchaseOrderListItemDto(
                    order.Id,
                    PurchaseOrder.FormatCode(order.Number),
                    order.SupplierId,
                    order.Supplier.Name,
                    order.Supplier.LogoPath == null ? null : FileUrls.SupplierLogo(order.SupplierId, order.Supplier.LogoPath, order.Supplier.UpdatedAt ?? order.Supplier.CreatedAt),
                    order.Warehouse.Name,
                    order.OrderDate,
                    order.Status,
                    order.Items.Count(),
                    order.Items.Sum(item => (int?)item.Quantity) ?? 0,
                    order.Total,
                    order.PaidAmount,
                    order.PaymentTerms,
                    order.DueDate,
                    order.CreatedAt)),
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result<PagedResult<PurchaseOrderListItemDto>>.Ok(page);
    }

    public async Task<Result<PurchaseSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var byStatus = await unitOfWork.Repository<PurchaseOrder>().Query()
            .GroupBy(order => order.Status)
            .Select(group => new { group.Key, Count = group.Count(), Total = group.Sum(order => order.Total) })
            .ToListAsync(cancellationToken);

        var placed = byStatus.Where(row => row.Key != PurchaseOrderStatus.Draft && row.Key != PurchaseOrderStatus.Cancelled).ToList();

        var suppliers = await unitOfWork.Repository<Supplier>().CountAsync(cancellationToken: cancellationToken);

        var awaiting = byStatus.Where(row => row.Key is PurchaseOrderStatus.Pending or PurchaseOrderStatus.InTransit).Sum(row => row.Total);

        var tabs = new List<SummaryCount> { new("all", byStatus.Sum(row => row.Count)) };

        tabs.AddRange(Enum.GetValues<PurchaseOrderStatus>().Select(status => new SummaryCount(status.ToString().ToLowerInvariant(), byStatus.FirstOrDefault(row => row.Key == status)?.Count ?? 0)));

        return Result<PurchaseSummaryDto>.Ok(new PurchaseSummaryDto(Money.Round(placed.Sum(row => row.Total)), suppliers, placed.Sum(row => row.Count), Money.Round(awaiting), tabs));
    }

    public async Task<Result<PurchaseOrderDto>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(id, cancellationToken);

        return order is null ? Result<PurchaseOrderDto>.NotFound(NotFound) : Result<PurchaseOrderDto>.Ok(order);
    }

    public async Task<Result<PurchaseOrderDto>> CreateAsync(SavePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var products = await ValidateAsync(request, cancellationToken);

        if (!products.Succeeded)
        {
            return Result<PurchaseOrderDto>.From(products);
        }

        var merchant = await context.GetMerchantAsync(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var order = new PurchaseOrder { Number = await numbers.NextAsync(merchant.Id, DocumentType.PurchaseOrder, cancellationToken) };

        Apply(order, request, products.Data!, merchant);

        await unitOfWork.Repository<PurchaseOrder>().AddAsync(order, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            await NotifyCreatedAsync(order, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<PurchaseOrderDto>.Created((await LoadAsync(order.Id, cancellationToken))!, order.Status == PurchaseOrderStatus.Draft ? "Purchase order saved as a draft." : "Purchase order created.");
    }

    public async Task<Result<PurchaseOrderDto>> UpdateAsync(long id, SavePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await unitOfWork.Repository<PurchaseOrder>().Query(asNoTracking: false, item => item.Items).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (order is null)
        {
            return Result<PurchaseOrderDto>.NotFound(NotFound);
        }

        if (order.Status is PurchaseOrderStatus.Delivered or PurchaseOrderStatus.Cancelled)
        {
            return Result<PurchaseOrderDto>.Conflict("A delivered or cancelled purchase order cannot be changed.");
        }

        var products = await ValidateAsync(request, cancellationToken);

        if (!products.Succeeded)
        {
            return Result<PurchaseOrderDto>.From(products);
        }

        var wasDraft = order.Status == PurchaseOrderStatus.Draft;

        unitOfWork.Repository<PurchaseOrderItem>().RemoveRange(order.Items);

        order.Items = [];

        Apply(order, request, products.Data!, await context.GetMerchantAsync(cancellationToken));

        if (wasDraft && order.Status != PurchaseOrderStatus.Draft)
        {
            await NotifyCreatedAsync(order, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PurchaseOrderDto>.Ok((await LoadAsync(id, cancellationToken))!, "Purchase order updated.");
    }

    public async Task<Result<PurchaseOrderDto>> UpdateStatusAsync(long id, UpdatePurchaseStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Status is not (PurchaseOrderStatus.Pending or PurchaseOrderStatus.InTransit))
        {
            return Result<PurchaseOrderDto>.BadRequest("Use deliver or cancel for those steps; the status can only be Pending or InTransit here.");
        }

        var order = await unitOfWork.Repository<PurchaseOrder>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (order is null)
        {
            return Result<PurchaseOrderDto>.NotFound(NotFound);
        }

        if (order.Status is PurchaseOrderStatus.Delivered or PurchaseOrderStatus.Cancelled)
        {
            return Result<PurchaseOrderDto>.Conflict("This purchase order is already delivered or cancelled.");
        }

        if (order.Status == PurchaseOrderStatus.Draft)
        {
            await NotifyCreatedAsync(order, cancellationToken);
        }

        order.Status = request.Status;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PurchaseOrderDto>.Ok((await LoadAsync(id, cancellationToken))!);
    }

    public async Task<Result<PurchaseOrderDto>> DeliverAsync(long id, DeliverPurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await unitOfWork.Repository<PurchaseOrder>().Query(asNoTracking: false, item => item.Items).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (order is null)
        {
            return Result<PurchaseOrderDto>.NotFound(NotFound);
        }

        if (order.Status is PurchaseOrderStatus.Delivered or PurchaseOrderStatus.Cancelled)
        {
            return Result<PurchaseOrderDto>.Conflict("This purchase order is already delivered or cancelled.");
        }

        var paid = Money.Round(request.PaidAmount);

        if (paid > order.Total)
        {
            return Result<PurchaseOrderDto>.BadRequest("The amount paid cannot be more than the order total.");
        }

        if (paid > 0 && request.PaymentMethod is null or PaymentMethod.Deferred)
        {
            return Result<PurchaseOrderDto>.BadRequest("Choose how the supplier was paid (cash, card or bank transfer).");
        }

        if (!await unitOfWork.Repository<Warehouse>().AnyAsync(warehouse => warehouse.Id == order.WarehouseId && warehouse.Status == RecordStatus.Active, cancellationToken))
        {
            return Result<PurchaseOrderDto>.Conflict("The receiving warehouse is not active. Change the warehouse of the order first.");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        // Discount and shipping are shared over the units so the cost of each unit is what it really cost (tax is left out).
        var landed = order.Subtotal == 0 ? 1 : (order.Subtotal - order.Discount + order.ShippingCost) / order.Subtotal;

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();

        var products = await unitOfWork.Repository<Product>().Query(asNoTracking: false)
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var onHand = await unitOfWork.Repository<StockItem>().Query()
            .Where(item => productIds.Contains(item.ProductId))
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToDictionaryAsync(row => row.ProductId, row => row.Quantity, cancellationToken);

        var changes = new List<StockChange>();

        foreach (var group in order.Items.GroupBy(item => item.ProductId))
        {
            var units = group.Sum(item => item.Quantity);

            var unitCost = Money.Round(group.Sum(item => item.Total) * landed / units);

            if (products.TryGetValue(group.Key, out var product))
            {
                // Moving average: what is on the shelf keeps its cost, the new units bring theirs.
                var existing = Math.Max(onHand.GetValueOrDefault(group.Key), 0);

                product.Cost = existing == 0 ? unitCost : Money.Round((existing * product.Cost + units * unitCost) / (existing + units));
            }

            changes.Add(new StockChange(order.WarehouseId, group.Key, units, StockMovementType.Purchase, unitCost, RelatedTypes.PurchaseOrder, order.Id, order.Code));
        }

        var stocked = await stockLedger.ApplyAsync(changes, allowNegative: true, cancellationToken);

        if (!stocked.Succeeded)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<PurchaseOrderDto>.From(stocked);
        }

        var supplier = await unitOfWork.Repository<Supplier>().Query(asNoTracking: false).FirstAsync(item => item.Id == order.SupplierId, cancellationToken);

        await balanceLedger.SupplierAsync(supplier, BalanceEntryType.Invoice, order.Total, order.Code, "Goods received", purchaseOrderId: order.Id, cancellationToken: cancellationToken);

        if (paid > 0)
        {
            await balanceLedger.SupplierAsync(supplier, BalanceEntryType.Payment, -paid, order.Code, "Paid on delivery", request.PaymentMethod, order.Id, cancellationToken);
        }

        order.Status = PurchaseOrderStatus.Delivered;

        order.DeliveredAt = request.DeliveredAt ?? DateTime.UtcNow;

        order.PaidAmount = paid;

        order.PaymentMethod = paid > 0 ? request.PaymentMethod : null;

        await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForMerchant(order.MerchantId, NotificationTypes.PurchaseOrderDelivered, $"وصل طلب الشراء {order.Code}", $"من {supplier.Name} — {order.Items.Sum(item => item.Quantity)} قطعة دخلت المخزن.", RelatedTypes.PurchaseOrder, order.Id), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<PurchaseOrderDto>.Ok((await LoadAsync(id, cancellationToken))!, "Goods received into the warehouse.");
    }

    public async Task<Result<PurchaseOrderDto>> CancelAsync(long id, ReasonRequest request, CancellationToken cancellationToken = default)
    {
        var order = await unitOfWork.Repository<PurchaseOrder>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (order is null)
        {
            return Result<PurchaseOrderDto>.NotFound(NotFound);
        }

        if (order.Status is PurchaseOrderStatus.Delivered or PurchaseOrderStatus.Cancelled)
        {
            return Result<PurchaseOrderDto>.Conflict("A delivered or cancelled purchase order cannot be cancelled.");
        }

        order.Status = PurchaseOrderStatus.Cancelled;

        order.CancelledAt = DateTime.UtcNow;

        order.CancelReason = request.Reason.Trim();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PurchaseOrderDto>.Ok((await LoadAsync(id, cancellationToken))!, "Purchase order cancelled.");
    }

    public async Task<Result> DeleteDraftAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await unitOfWork.Repository<PurchaseOrder>().Query(asNoTracking: false, item => item.Items).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (order is null)
        {
            return Result.NotFound(NotFound);
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return Result.Conflict("Only a draft can be deleted. Cancel the purchase order instead.");
        }

        unitOfWork.Repository<PurchaseOrderItem>().RemoveRange(order.Items);

        unitOfWork.Repository<PurchaseOrder>().Remove(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Draft deleted.");
    }

    public async Task<Result<FileDownload>> ExportAsync(PurchaseOrderQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await Filter(query)
            .OrderByDescending(order => order.Id)
            .Take(10000)
            .Select(order => new { order.Number, Supplier = order.Supplier.Name, Warehouse = order.Warehouse.Name, order.OrderDate, order.Status, Units = order.Items.Sum(item => (int?)item.Quantity) ?? 0, order.Subtotal, order.Discount, order.TaxAmount, order.ShippingCost, order.Total, order.PaidAmount, order.PaymentTerms, order.DueDate, order.DeliveredAt })
            .ToListAsync(cancellationToken);

        var csv = new CsvFile("PO", "Supplier", "Warehouse", "Date", "Status", "Units", "Subtotal", "Discount", "Tax", "Shipping", "Total", "Paid", "Terms", "Due", "Delivered");

        foreach (var row in rows)
        {
            csv.Row(PurchaseOrder.FormatCode(row.Number), row.Supplier, row.Warehouse, row.OrderDate, row.Status, row.Units, row.Subtotal, row.Discount, row.TaxAmount, row.ShippingCost, row.Total, row.PaidAmount, row.PaymentTerms, row.DueDate, row.DeliveredAt);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("purchases"));
    }

    private async Task<Result<Dictionary<int, Product>>> ValidateAsync(SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Pending or PurchaseOrderStatus.InTransit))
        {
            return Result<Dictionary<int, Product>>.BadRequest("A purchase order is saved as Draft, Pending or InTransit; delivery is its own step.");
        }

        if (!await unitOfWork.Repository<Supplier>().AnyAsync(supplier => supplier.Id == request.SupplierId && supplier.Status != RecordStatus.Archived, cancellationToken))
        {
            return Result<Dictionary<int, Product>>.BadRequest("Supplier was not found or is archived.");
        }

        if (!await unitOfWork.Repository<Warehouse>().AnyAsync(warehouse => warehouse.Id == request.WarehouseId && warehouse.Status == RecordStatus.Active, cancellationToken))
        {
            return Result<Dictionary<int, Product>>.BadRequest("Choose an active warehouse to receive the goods.");
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();

        var products = await unitOfWork.Repository<Product>().Query().Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return Result<Dictionary<int, Product>>.NotFound("Product was not found.");
        }

        var subtotal = request.Items.Sum(item => item.Quantity * item.UnitCost);

        if (request.Discount > subtotal)
        {
            return Result<Dictionary<int, Product>>.BadRequest("The discount cannot be more than the subtotal.");
        }

        return Result<Dictionary<int, Product>>.Ok(products);
    }

    private static void Apply(PurchaseOrder order, SavePurchaseOrderRequest request, Dictionary<int, Product> products, Merchant merchant)
    {
        order.SupplierId = request.SupplierId;

        order.WarehouseId = request.WarehouseId;

        order.OrderDate = LibyaTime.ToUtc((request.OrderDate ?? DateOnly.FromDateTime(LibyaTime.Now)).ToDateTime(TimeOnly.MinValue));

        order.ExpectedAt = request.ExpectedAt is { } expected ? LibyaTime.ToUtc(expected.ToDateTime(TimeOnly.MinValue)) : null;

        order.PaymentTerms = string.IsNullOrWhiteSpace(request.PaymentTerms) ? null : request.PaymentTerms.Trim();

        order.DueDate = request.DueDate is { } due ? LibyaTime.ToUtc(due.ToDateTime(TimeOnly.MinValue)) : null;

        order.Status = request.Status;

        order.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];

            order.Items.Add(new PurchaseOrderItem
            {
                MerchantId = product.MerchantId,

                ProductId = product.Id,

                ProductName = product.Name,

                Sku = product.Sku,

                Quantity = item.Quantity,

                UnitCost = Money.Round(item.UnitCost),

                Total = Money.Round(item.Quantity * item.UnitCost)
            });
        }

        order.Subtotal = Money.Round(order.Items.Sum(item => item.Total));

        order.Discount = Money.Round(request.Discount);

        order.TaxRate = request.TaxRate ?? merchant.TaxRate;

        order.TaxAmount = Money.Percent(order.Subtotal - order.Discount, order.TaxRate);

        order.ShippingCost = Money.Round(request.ShippingCost);

        order.Total = Money.Round(order.Subtotal - order.Discount + order.TaxAmount + order.ShippingCost);
    }

    private async Task NotifyCreatedAsync(PurchaseOrder order, CancellationToken cancellationToken)
    {
        var supplier = await unitOfWork.Repository<Supplier>().Query().Where(item => item.Id == order.SupplierId).Select(item => item.Name).FirstOrDefaultAsync(cancellationToken);

        await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForMerchant(context.MerchantId, NotificationTypes.PurchaseOrderCreated, $"طلب شراء جديد {order.Code}", $"من {supplier} بقيمة {order.Total:0.###} د.ل", RelatedTypes.PurchaseOrder, order.Id), cancellationToken);
    }

    private IQueryable<PurchaseOrder> Filter(PurchaseOrderQuery query)
    {
        var orders = unitOfWork.Repository<PurchaseOrder>().Query();

        if (query.Status is { } status)
        {
            orders = orders.Where(order => order.Status == status);
        }

        if (query.SupplierId is { } supplierId)
        {
            orders = orders.Where(order => order.SupplierId == supplierId);
        }

        if (query.WarehouseId is { } warehouseId)
        {
            orders = orders.Where(order => order.WarehouseId == warehouseId);
        }

        if (query.From is not null || query.To is not null)
        {
            var range = DateRange.FromLocalDates(query.From, query.To);

            orders = orders.Where(order => order.OrderDate >= range.From && order.OrderDate < range.To);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            var digits = new string(search.Where(char.IsAsciiDigit).ToArray());

            var number = long.TryParse(digits, out var parsed) ? parsed : -1;

            orders = orders.Where(order => order.Number == number || order.Supplier.Name.Contains(search));
        }

        return orders;
    }

    private async Task<PurchaseOrderDto?> LoadAsync(long id, CancellationToken cancellationToken)
    {
        var users = unitOfWork.Repository<User>().Query();

        var row = await unitOfWork.Repository<PurchaseOrder>().Query()
            .Where(order => order.Id == id)
            .Select(order => new
            {
                Order = order,

                SupplierName = order.Supplier.Name,

                SupplierLogo = order.Supplier.LogoPath,

                SupplierChanged = order.Supplier.UpdatedAt ?? order.Supplier.CreatedAt,

                WarehouseName = order.Warehouse.Name,

                CreatedBy = users.Where(user => user.Id == order.CreatedBy).Select(user => user.FullName).FirstOrDefault(),

                Items = order.Items.OrderBy(item => item.Id).Select(item => new { Item = item, item.Product.ImagePath, item.Product.UpdatedAt, item.Product.CreatedAt }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var order = row.Order;

        var items = row.Items
            .Select(line => new PurchaseOrderItemDto(line.Item.Id, line.Item.ProductId, line.Item.ProductName, line.Item.Sku, FileUrls.Product(line.Item.ProductId, line.ImagePath, line.UpdatedAt ?? line.CreatedAt), line.Item.Quantity, line.Item.UnitCost, line.Item.Total))
            .ToList();

        return new PurchaseOrderDto(
            order.Id,
            order.Code,
            order.SupplierId,
            row.SupplierName,
            FileUrls.SupplierLogo(order.SupplierId, row.SupplierLogo, row.SupplierChanged),
            order.WarehouseId,
            row.WarehouseName,
            order.OrderDate,
            order.ExpectedAt,
            order.Status,
            order.PaymentTerms,
            order.DueDate,
            order.Subtotal,
            order.Discount,
            order.TaxRate,
            order.TaxAmount,
            order.ShippingCost,
            order.Total,
            order.PaidAmount,
            order.PaymentMethod,
            order.Note,
            items,
            row.CreatedBy,
            order.CreatedAt,
            order.DeliveredAt,
            order.CancelledAt,
            order.CancelReason);
    }
}
