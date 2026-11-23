using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Sales;

using MicroNamaa.Application.Services.Inventory;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Application.Services.Parties;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Sales;

public class SalesInvoiceService(IUnitOfWork unitOfWork, MerchantContext context, IUserAppService userAppService, IDocumentNumberService numbers, StockLedger stockLedger, BalanceLedger balanceLedger) : ISalesInvoiceService
{
    private const string NotFound = "Invoice was not found.";

    /// <summary>The cart loaded and priced, ready to be saved.</summary>
    private sealed record Cart(Branch Branch, Warehouse Warehouse, int? RegisterId, Customer? Customer, Merchant Merchant, PricingResult Pricing, Dictionary<int, Product> Products, Dictionary<int, int> Available);

    public async Task<Result<PagedResult<SalesInvoiceListItemDto>>> GetAllAsync(SalesInvoiceQuery query, CancellationToken cancellationToken = default)
    {
        var page = await Filter(query)
            .OrderByDescending(invoice => invoice.Id)
            .ToPagedResultAsync(
                pageQuery => pageQuery.Select(invoice => new SalesInvoiceListItemDto(
                    invoice.Id,
                    SalesInvoice.FormatCode(invoice.Number),
                    invoice.Status,
                    invoice.Branch.Name,
                    invoice.CustomerId,
                    invoice.Customer == null ? null : invoice.Customer.Name,
                    invoice.Cashier.FullName,
                    invoice.Items.Count(),
                    invoice.Items.Sum(item => (int?)item.Quantity) ?? 0,
                    invoice.Total,
                    invoice.PaidAmount,
                    invoice.DueAmount,
                    invoice.ReturnedAmount,
                    invoice.PaymentMethod,
                    invoice.CreatedAt,
                    invoice.CompletedAt)),
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result<PagedResult<SalesInvoiceListItemDto>>.Ok(page);
    }

    public async Task<Result<SalesInvoiceSummaryDto>> GetSummaryAsync(SalesInvoiceQuery query, CancellationToken cancellationToken = default)
    {
        var all = Filter(query, ignoreStatus: true);

        var byStatus = await all.GroupBy(invoice => invoice.Status).Select(group => new { group.Key, Count = group.Count() }).ToListAsync(cancellationToken);

        var totals = (await all
            .Where(invoice => invoice.Status == SalesInvoiceStatus.Completed)
            .GroupBy(invoice => 1)
            .Select(group => new { Count = group.Count(), Sales = group.Sum(invoice => invoice.Total), Returns = group.Sum(invoice => invoice.ReturnedAmount), Unpaid = group.Sum(invoice => invoice.DueAmount) })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        var tabs = new List<SummaryCount> { new("all", byStatus.Sum(row => row.Count)) };

        tabs.AddRange(Enum.GetValues<SalesInvoiceStatus>().Select(status => new SummaryCount(status.ToString().ToLowerInvariant(), byStatus.FirstOrDefault(row => row.Key == status)?.Count ?? 0)));

        var count = totals?.Count ?? 0;

        var net = (totals?.Sales ?? 0) - (totals?.Returns ?? 0);

        return Result<SalesInvoiceSummaryDto>.Ok(new SalesInvoiceSummaryDto(count, Money.Round(net), Money.Round(totals?.Returns ?? 0), Money.Round(totals?.Unpaid ?? 0), count == 0 ? 0 : Money.Round(net / count), tabs));
    }

    public async Task<Result<SalesInvoiceDto>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadAsync(id, cancellationToken);

        return invoice is null ? Result<SalesInvoiceDto>.NotFound(NotFound) : Result<SalesInvoiceDto>.Ok(invoice);
    }

    public async Task<Result<InvoicePreviewDto>> PreviewAsync(SaveSalesInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await BuildCartAsync(request, cancellationToken);

        if (!cart.Succeeded)
        {
            return Result<InvoicePreviewDto>.From(cart);
        }

        var data = cart.Data!;

        var pricing = data.Pricing;

        var warnings = pricing.Lines
            .Where(line => line.Quantity > data.Available.GetValueOrDefault(line.ProductId))
            .Select(line => $"Only {Math.Max(data.Available.GetValueOrDefault(line.ProductId), 0)} of \"{line.Name}\" left in stock.")
            .ToList();

        if (data.Customer is { CreditLimit: { } limit } customer && request.Payment is { Method: PaymentMethod.Deferred } payment && customer.Balance + pricing.Total - (payment.PaidAmount ?? 0) > limit)
        {
            warnings.Add($"This sale takes the customer over their credit limit of {limit:0.###} LYD.");
        }

        var lines = pricing.Lines
            .Select(line => new InvoicePreviewLineDto(line.ProductId, line.Name, line.Sku, FileUrls.Product(line.ProductId, data.Products[line.ProductId].ImagePath, data.Products[line.ProductId].UpdatedAt ?? data.Products[line.ProductId].CreatedAt), line.Quantity, line.UnitPrice, line.Discount, line.PromotionName, line.LineTotal, data.Available.GetValueOrDefault(line.ProductId)))
            .ToList();

        return Result<InvoicePreviewDto>.Ok(new InvoicePreviewDto(
            lines,
            lines.Count,
            lines.Sum(line => line.Quantity),
            pricing.Subtotal,
            pricing.ItemsDiscount,
            pricing.PromotionName,
            pricing.OrderDiscount,
            pricing.PointsRedeemed,
            pricing.PointsDiscount,
            pricing.TaxRate,
            pricing.TaxAmount,
            pricing.Total,
            pricing.PointsEarned,
            data.Customer?.LoyaltyPoints,
            data.Customer?.Balance,
            warnings));
    }

    public Task<Result<SalesInvoiceDto>> CreateAsync(SaveSalesInvoiceRequest request, CancellationToken cancellationToken = default) => SaveAsync(null, request, cancellationToken);

    public Task<Result<SalesInvoiceDto>> CreateOfflineAsync(SaveSalesInvoiceRequest request, OfflineSale offline, CancellationToken cancellationToken = default) => SaveAsync(null, request, cancellationToken, offline);

    public async Task<Result<SalesInvoiceDto>> UpdateAsync(long id, SaveSalesInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await Visible().Where(item => item.Id == id).Select(item => new { item.Status, item.BranchId }).FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return Result<SalesInvoiceDto>.NotFound(NotFound);
        }

        if (invoice.Status != SalesInvoiceStatus.Draft)
        {
            return Result<SalesInvoiceDto>.Conflict("Only a draft can be changed.");
        }

        return await SaveAsync(id, request with { BranchId = invoice.BranchId }, cancellationToken);
    }

    public async Task<Result<SalesInvoiceDto>> CompleteAsync(long id, CompleteSalesInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var draft = await Visible()
            .Where(invoice => invoice.Id == id)
            .Select(invoice => new
            {
                invoice.Status,

                invoice.BranchId,

                invoice.RegisterId,

                invoice.CustomerId,

                invoice.PromotionId,

                invoice.ManualDiscountType,

                invoice.ManualDiscountValue,

                invoice.PointsRedeemed,

                invoice.Note,

                Items = invoice.Items.Select(item => new SalesInvoiceItemRequest { ProductId = item.ProductId, Quantity = item.Quantity }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (draft is null)
        {
            return Result<SalesInvoiceDto>.NotFound(NotFound);
        }

        if (draft.Status != SalesInvoiceStatus.Draft)
        {
            return Result<SalesInvoiceDto>.Conflict("This invoice is already completed or cancelled.");
        }

        return await SaveAsync(id, new SaveSalesInvoiceRequest
        {
            BranchId = draft.BranchId,

            RegisterId = draft.RegisterId,

            CustomerId = draft.CustomerId,

            Items = draft.Items,

            PromotionId = draft.PromotionId,

            DiscountType = draft.ManualDiscountType,

            DiscountValue = draft.ManualDiscountValue,

            PointsToRedeem = draft.PointsRedeemed,

            Note = draft.Note,

            Complete = true,

            Payment = request.Payment
        }, cancellationToken);
    }

    public async Task<Result<SalesInvoiceDto>> CancelAsync(long id, ReasonRequest request, CancellationToken cancellationToken = default)
    {
        if (!userAppService.IsManager)
        {
            return Result<SalesInvoiceDto>.Forbidden("Only the owner or a manager can cancel an invoice.");
        }

        var invoice = await unitOfWork.Repository<SalesInvoice>().Query(asNoTracking: false, item => item.Items).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (invoice is null)
        {
            return Result<SalesInvoiceDto>.NotFound(NotFound);
        }

        if (invoice.Status != SalesInvoiceStatus.Completed)
        {
            return Result<SalesInvoiceDto>.Conflict("Only a completed invoice can be cancelled. Delete a draft instead.");
        }

        if (invoice.ReturnedAmount > 0 || invoice.Items.Any(item => item.ReturnedQuantity > 0))
        {
            return Result<SalesInvoiceDto>.Conflict("This invoice has returns, so it cannot be cancelled. Return the remaining items instead.");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var changes = invoice.Items.Select(item => new StockChange(invoice.WarehouseId, item.ProductId, item.Quantity, StockMovementType.SaleCancelled, item.UnitCost, RelatedTypes.SalesInvoice, invoice.Id, invoice.Code, request.Reason.Trim())).ToList();

        var restocked = await stockLedger.ApplyAsync(changes, allowNegative: true, cancellationToken);

        if (!restocked.Succeeded)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<SalesInvoiceDto>.From(restocked);
        }

        if (invoice.CustomerId is { } customerId)
        {
            var customer = await unitOfWork.Repository<Customer>().Query(asNoTracking: false).FirstAsync(item => item.Id == customerId, cancellationToken);

            if (invoice.DueAmount > 0)
            {
                await balanceLedger.CustomerAsync(customer, BalanceEntryType.Cancellation, -invoice.DueAmount, invoice.Code, request.Reason.Trim(), invoiceId: invoice.Id, cancellationToken: cancellationToken);
            }

            customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints - invoice.PointsEarned + invoice.PointsRedeemed);
        }

        invoice.Status = SalesInvoiceStatus.Cancelled;

        invoice.CancelledAt = DateTime.UtcNow;

        invoice.CancelledBy = userAppService.RequiredUserId;

        invoice.CancelReason = request.Reason.Trim();

        await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForMerchant(invoice.MerchantId, NotificationTypes.SalesInvoiceCancelled, $"اتلغت الفاتورة {invoice.Code}", $"{invoice.Total:0.###} د.ل — {invoice.CancelReason}", RelatedTypes.SalesInvoice, invoice.Id), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<SalesInvoiceDto>.Ok((await LoadAsync(id, cancellationToken))!, "Invoice cancelled.");
    }

    public async Task<Result> DeleteDraftAsync(long id, CancellationToken cancellationToken = default)
    {
        var invoice = await Visible(asNoTracking: false, includeItems: true).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (invoice is null)
        {
            return Result.NotFound(NotFound);
        }

        if (invoice.Status != SalesInvoiceStatus.Draft)
        {
            return Result.Conflict("Only a draft can be deleted.");
        }

        unitOfWork.Repository<SalesInvoiceItem>().RemoveRange(invoice.Items);

        unitOfWork.Repository<SalesInvoice>().Remove(invoice);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Draft deleted.");
    }

    public async Task<Result<FileDownload>> ExportAsync(SalesInvoiceQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await Filter(query)
            .OrderByDescending(invoice => invoice.Id)
            .Take(10000)
            .Select(invoice => new
            {
                invoice.Number,

                invoice.Status,

                Branch = invoice.Branch.Name,

                Customer = invoice.Customer == null ? null : invoice.Customer.Name,

                Cashier = invoice.Cashier.FullName,

                invoice.PaymentMethod,

                invoice.Subtotal,

                Discounts = invoice.ItemsDiscount + invoice.OrderDiscount + invoice.PointsDiscount,

                invoice.TaxAmount,

                invoice.Total,

                invoice.PaidAmount,

                invoice.DueAmount,

                invoice.ReturnedAmount,

                invoice.CreatedAt,

                invoice.CompletedAt
            })
            .ToListAsync(cancellationToken);

        var csv = new CsvFile("Invoice", "Status", "Branch", "Customer", "Cashier", "Payment", "Subtotal", "Discounts", "Tax", "Total", "Paid", "Due", "Returned", "Created", "Completed");

        foreach (var row in rows)
        {
            csv.Row(SalesInvoice.FormatCode(row.Number), row.Status, row.Branch, row.Customer, row.Cashier, row.PaymentMethod, row.Subtotal, row.Discounts, row.TaxAmount, row.Total, row.PaidAmount, row.DueAmount, row.ReturnedAmount, row.CreatedAt, row.CompletedAt);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("sales-invoices"));
    }

    private async Task<Result<SalesInvoiceDto>> SaveAsync(long? id, SaveSalesInvoiceRequest request, CancellationToken cancellationToken, OfflineSale? offline = null)
    {
        if (request.Complete && request.Payment is null)
        {
            return Result<SalesInvoiceDto>.BadRequest("Choose how the customer pays.");
        }

        var built = await BuildCartAsync(request, cancellationToken, offline);

        if (!built.Succeeded)
        {
            return Result<SalesInvoiceDto>.From(built);
        }

        var cart = built.Data!;

        var pricing = cart.Pricing;

        Payment? payment = null;

        if (request.Complete)
        {
            var checkedPayment = CheckPayment(request.Payment!, pricing.Total, cart.Customer, offline is not null);

            if (!checkedPayment.Succeeded)
            {
                return Result<SalesInvoiceDto>.From(checkedPayment);
            }

            payment = checkedPayment.Data;
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            SalesInvoice invoice;

            if (id is { } existingId)
            {
                invoice = await unitOfWork.Repository<SalesInvoice>().Query(asNoTracking: false, item => item.Items).FirstAsync(item => item.Id == existingId, cancellationToken);

                if (invoice.Status != SalesInvoiceStatus.Draft)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<SalesInvoiceDto>.Conflict("This invoice is already completed or cancelled.");
                }

                unitOfWork.Repository<SalesInvoiceItem>().RemoveRange(invoice.Items);

                invoice.Items = [];
            }

            else
            {
                invoice = new SalesInvoice
                {
                    ClientKey = offline?.ClientKey,

                    Number = await numbers.NextAsync(cart.Merchant.Id, DocumentType.SalesInvoice, cancellationToken),

                    CashierId = userAppService.RequiredUserId
                };

                await unitOfWork.Repository<SalesInvoice>().AddAsync(invoice, cancellationToken);
            }

            Fill(invoice, request, cart);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (payment is not null)
            {
                var completed = await CompleteCoreAsync(invoice, cart, payment, offline, cancellationToken);

                if (!completed.Succeeded)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Result<SalesInvoiceDto>.From(completed);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            var dto = (await LoadAsync(invoice.Id, cancellationToken))!;

            var message = payment is null ? "Invoice saved as a draft." : "Invoice completed.";

            return id is null ? Result<SalesInvoiceDto>.Created(dto, message) : Result<SalesInvoiceDto>.Ok(dto, message);
        }

        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            throw;
        }
    }

    private sealed record Payment(PaymentMethod Method, decimal Paid, decimal? Tendered, decimal Change, decimal Due);

    /// <summary>An offline sale already happened in the shop, so its cash and credit limit are not checked again.</summary>
    private static Result<Payment> CheckPayment(InvoicePaymentRequest request, decimal total, Customer? customer, bool offline)
    {
        switch (request.Method)
        {
            case PaymentMethod.Cash:
                var tendered = request.TenderedAmount is { } given ? Money.Round(given) : (decimal?)null;

                if (tendered < total && !offline)
                {
                    return Result<Payment>.BadRequest("The cash handed over is less than the total.");
                }

                return Result<Payment>.Ok(new Payment(PaymentMethod.Cash, total, tendered, tendered is null ? 0 : Money.Round(Math.Max(tendered.Value - total, 0)), 0));

            case PaymentMethod.Card or PaymentMethod.BankTransfer:
                return Result<Payment>.Ok(new Payment(request.Method, total, null, 0, 0));

            case PaymentMethod.Deferred:
                if (customer is null)
                {
                    return Result<Payment>.BadRequest("Choose the customer for a deferred sale.");
                }

                var paid = Money.Round(request.PaidAmount ?? 0);

                if (paid < 0 || paid > total)
                {
                    return Result<Payment>.BadRequest("The amount paid now must be between zero and the total.");
                }

                var due = Money.Round(total - paid);

                if (!offline && customer.CreditLimit is { } limit && customer.Balance + due > limit)
                {
                    return Result<Payment>.Conflict($"This sale takes the customer over their credit limit of {limit:0.###} LYD (they owe {customer.Balance:0.###} LYD).");
                }

                return Result<Payment>.Ok(new Payment(PaymentMethod.Deferred, paid, null, 0, due));

            default:
                return Result<Payment>.BadRequest("Unknown payment method.");
        }
    }

    private async Task<Result> CompleteCoreAsync(SalesInvoice invoice, Cart cart, Payment payment, OfflineSale? offline, CancellationToken cancellationToken)
    {
        var changes = invoice.Items.Select(item => new StockChange(invoice.WarehouseId, item.ProductId, -item.Quantity, StockMovementType.Sale, item.UnitCost, RelatedTypes.SalesInvoice, invoice.Id, invoice.Code)).ToList();

        // An offline sale is never refused for stock: the goods already left the shop.
        var stocked = await stockLedger.ApplyAsync(changes, cart.Merchant.AllowNegativeStock || offline is not null, cancellationToken);

        if (!stocked.Succeeded)
        {
            return stocked;
        }

        invoice.PaymentMethod = payment.Method;

        invoice.PaidAmount = payment.Paid;

        invoice.TenderedAmount = payment.Tendered;

        invoice.ChangeAmount = payment.Change;

        invoice.DueAmount = payment.Due;

        invoice.Status = SalesInvoiceStatus.Completed;

        invoice.CompletedAt = offline?.At ?? DateTime.UtcNow;

        if (cart.Customer is { } snapshot)
        {
            var customer = await unitOfWork.Repository<Customer>().Query(asNoTracking: false).FirstAsync(item => item.Id == snapshot.Id, cancellationToken);

            if (invoice.PointsRedeemed > customer.LoyaltyPoints && offline is null)
            {
                return Result.Conflict("The customer no longer has enough points.");
            }

            customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints + invoice.PointsEarned - invoice.PointsRedeemed);

            if (payment.Due > 0)
            {
                await balanceLedger.CustomerAsync(customer, BalanceEntryType.Invoice, payment.Due, invoice.Code, null, PaymentMethod.Deferred, invoice.Id, cancellationToken: cancellationToken);
            }
        }

        return Result.Ok();
    }

    private static void Fill(SalesInvoice invoice, SaveSalesInvoiceRequest request, Cart cart)
    {
        var pricing = cart.Pricing;

        invoice.BranchId = cart.Branch.Id;

        invoice.RegisterId = cart.RegisterId;

        invoice.WarehouseId = cart.Warehouse.Id;

        invoice.CustomerId = cart.Customer?.Id;

        invoice.Subtotal = pricing.Subtotal;

        invoice.ItemsDiscount = pricing.ItemsDiscount;

        invoice.PromotionId = request.PromotionId;

        invoice.PromotionName = pricing.PromotionName;

        invoice.ManualDiscountType = request.DiscountValue is > 0 ? request.DiscountType : null;

        invoice.ManualDiscountValue = request.DiscountValue is > 0 && request.DiscountType is not null ? request.DiscountValue : null;

        invoice.OrderDiscount = pricing.OrderDiscount;

        invoice.PointsRedeemed = pricing.PointsRedeemed;

        invoice.PointsDiscount = pricing.PointsDiscount;

        invoice.TaxRate = pricing.TaxRate;

        invoice.TaxAmount = pricing.TaxAmount;

        invoice.Total = pricing.Total;

        invoice.PointsEarned = cart.Customer is null ? 0 : pricing.PointsEarned;

        invoice.TotalCost = Money.Round(pricing.Lines.Sum(line => line.UnitCost * line.Quantity));

        invoice.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        foreach (var line in pricing.Lines)
        {
            invoice.Items.Add(new SalesInvoiceItem
            {
                MerchantId = cart.Merchant.Id,

                ProductId = line.ProductId,

                ProductName = line.Name,

                Sku = line.Sku,

                Quantity = line.Quantity,

                UnitPrice = line.UnitPrice,

                UnitCost = line.UnitCost,

                DiscountAmount = line.Discount,

                PromotionId = line.PromotionId,

                PromotionName = line.PromotionName,

                LineTotal = line.LineTotal
            });
        }
    }

    private async Task<Result<Cart>> BuildCartAsync(SaveSalesInvoiceRequest request, CancellationToken cancellationToken, OfflineSale? offline = null)
    {
        var branch = await context.ResolveBranchAsync(request.BranchId, cancellationToken);

        if (!branch.Succeeded)
        {
            return Result<Cart>.From(branch);
        }

        var warehouse = await context.GetSellingWarehouseAsync(branch.Data!, cancellationToken);

        if (warehouse is null)
        {
            return Result<Cart>.Conflict("This branch has no active warehouse to sell from. Set one in the branch settings.");
        }

        if (request.RegisterId is { } registerId && !await unitOfWork.Repository<Register>().AnyAsync(register => register.Id == registerId && register.BranchId == branch.Data!.Id && register.IsActive, cancellationToken))
        {
            return Result<Cart>.BadRequest("The register was not found in this branch.");
        }

        var merchant = await context.GetMerchantAsync(cancellationToken);

        var lines = request.Items.GroupBy(item => item.ProductId).Select(group => (ProductId: group.Key, Quantity: group.Sum(item => item.Quantity))).ToList();

        var productIds = lines.Select(line => line.ProductId).ToList();

        var products = await unitOfWork.Repository<Product>().Query().Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id, cancellationToken);

        if (lines.FirstOrDefault(line => !products.ContainsKey(line.ProductId)) is { ProductId: > 0 } missing)
        {
            return Result<Cart>.NotFound($"Product {missing.ProductId} was not found.");
        }

        if (offline is not null)
        {
            // Sold offline at the price the customer paid (the products are read without tracking, so nothing is saved).
            foreach (var item in request.Items.Where(item => item.UnitPrice is not null))
            {
                products[item.ProductId].Price = Money.Round(item.UnitPrice!.Value);
            }
        }

        else if (products.Values.FirstOrDefault(product => product.Status != RecordStatus.Active) is { } inactive)
        {
            return Result<Cart>.Conflict($"\"{inactive.Name}\" is not for sale (draft or archived).");
        }

        var available = await unitOfWork.Repository<StockItem>().Query()
            .Where(item => item.WarehouseId == warehouse.Id && productIds.Contains(item.ProductId))
            .ToDictionaryAsync(item => item.ProductId, item => item.Quantity, cancellationToken);

        Customer? customer = null;

        if (request.CustomerId is { } customerId)
        {
            customer = await unitOfWork.Repository<Customer>().Query().FirstOrDefaultAsync(item => item.Id == customerId, cancellationToken);

            if (customer is null)
            {
                return Result<Cart>.NotFound("Customer was not found.");
            }

            if (customer.Status == RecordStatus.Archived)
            {
                return Result<Cart>.Conflict("This customer is archived.");
            }
        }

        // Offline sales use the promotions that were running when the sale was made.
        var now = offline?.At ?? DateTime.UtcNow;

        var itemPromotions = await unitOfWork.Repository<Promotion>().Query()
            .Where(promotion => promotion.Scope != PromotionScope.Order)
            .Where(Promotions.IsRunning(now))
            .ToListAsync(cancellationToken);

        Promotion? orderPromotion = null;

        if (request.PromotionId is { } promotionId)
        {
            orderPromotion = await unitOfWork.Repository<Promotion>().Query().Where(Promotions.IsRunning(now)).FirstOrDefaultAsync(promotion => promotion.Id == promotionId && promotion.Scope == PromotionScope.Order, cancellationToken);

            if (orderPromotion is null)
            {
                return Result<Cart>.BadRequest("This promotion is not available now.");
            }
        }

        if (request.DiscountType == DiscountType.Percentage && request.DiscountValue > 100)
        {
            return Result<Cart>.BadRequest("A percentage cannot be more than 100.");
        }

        if (request.PointsToRedeem > 0)
        {
            if (!merchant.LoyaltyEnabled || merchant.PointValue <= 0)
            {
                return Result<Cart>.BadRequest("Loyalty points are turned off for this store.");
            }

            if (customer is null)
            {
                return Result<Cart>.BadRequest("Choose the customer whose points are used.");
            }

            if (request.PointsToRedeem > customer.LoyaltyPoints && offline is null)
            {
                return Result<Cart>.BadRequest($"The customer has only {customer.LoyaltyPoints} points.");
            }

            if (request.PointsToRedeem < merchant.MinPointsToRedeem && offline is null)
            {
                return Result<Cart>.BadRequest($"At least {merchant.MinPointsToRedeem} points must be used at once.");
            }
        }

        var pricing = SalesPricing.Calculate(new PricingInput(
            lines.Select(line => (products[line.ProductId], line.Quantity)).ToList(),
            itemPromotions,
            orderPromotion,
            request.DiscountType,
            request.DiscountValue,
            request.PointsToRedeem,
            merchant.PointValue,
            merchant.TaxRate,
            merchant.PointsPerCurrencyUnit,
            merchant.LoyaltyEnabled && customer is not null));

        return Result<Cart>.Ok(new Cart(branch.Data!, warehouse, request.RegisterId, customer, merchant, pricing, products, available));
    }

    /// <summary>Owners and managers see every invoice; a cashier sees their own.</summary>
    private IQueryable<SalesInvoice> Visible(bool asNoTracking = true, bool includeItems = false)
    {
        var invoices = includeItems ? unitOfWork.Repository<SalesInvoice>().Query(asNoTracking, invoice => invoice.Items) : unitOfWork.Repository<SalesInvoice>().Query(asNoTracking);

        if (userAppService.IsManager)
        {
            return invoices;
        }

        var userId = userAppService.RequiredUserId;

        return invoices.Where(invoice => invoice.CashierId == userId);
    }

    private IQueryable<SalesInvoice> Filter(SalesInvoiceQuery query, bool ignoreStatus = false)
    {
        var invoices = Visible();

        if (!ignoreStatus && query.Status is { } status)
        {
            invoices = invoices.Where(invoice => invoice.Status == status);
        }

        if (query.BranchId is { } branchId)
        {
            invoices = invoices.Where(invoice => invoice.BranchId == branchId);
        }

        if (query.CustomerId is { } customerId)
        {
            invoices = invoices.Where(invoice => invoice.CustomerId == customerId);
        }

        if (query.CashierId is { } cashierId)
        {
            invoices = invoices.Where(invoice => invoice.CashierId == cashierId);
        }

        if (query.PaymentMethod is { } method)
        {
            invoices = invoices.Where(invoice => invoice.PaymentMethod == method);
        }

        if (query.UnpaidOnly)
        {
            invoices = invoices.Where(invoice => invoice.DueAmount > 0);
        }

        if (query.From is not null || query.To is not null)
        {
            var range = DateRange.FromLocalDates(query.From, query.To);

            invoices = invoices.Where(invoice => invoice.CreatedAt >= range.From && invoice.CreatedAt < range.To);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            var digits = new string(search.Where(char.IsAsciiDigit).ToArray());

            var number = long.TryParse(digits, out var parsed) ? parsed : -1;

            invoices = invoices.Where(invoice => invoice.Number == number || (invoice.Customer != null && (invoice.Customer.Name.Contains(search) || invoice.Customer.Phone == search)));
        }

        return invoices;
    }

    private async Task<SalesInvoiceDto?> LoadAsync(long id, CancellationToken cancellationToken)
    {
        var row = await Visible()
            .Where(invoice => invoice.Id == id)
            .Select(invoice => new
            {
                Invoice = invoice,

                BranchName = invoice.Branch.Name,

                RegisterName = invoice.Register == null ? null : invoice.Register.Name,

                WarehouseName = invoice.Warehouse.Name,

                invoice.Customer,

                CashierName = invoice.Cashier.FullName,

                Items = invoice.Items.OrderBy(item => item.Id).Select(item => new { Item = item, item.Product.ImagePath, item.Product.UpdatedAt, item.Product.CreatedAt }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var invoice = row.Invoice;

        var customer = row.Customer is { } c ? new InvoiceCustomerDto(c.Id, c.Code, c.Name, c.Phone, FileUrls.CustomerAvatar(c.Id, c.AvatarPath, c.UpdatedAt ?? c.CreatedAt), c.Balance, c.LoyaltyPoints) : null;

        var items = row.Items
            .Select(line => new SalesInvoiceItemDto(line.Item.Id, line.Item.ProductId, line.Item.ProductName, line.Item.Sku, FileUrls.Product(line.Item.ProductId, line.ImagePath, line.UpdatedAt ?? line.CreatedAt), line.Item.Quantity, line.Item.UnitPrice, line.Item.DiscountAmount, line.Item.PromotionName, line.Item.LineTotal, line.Item.ReturnedQuantity))
            .ToList();

        var completed = invoice.Status == SalesInvoiceStatus.Completed;

        return new SalesInvoiceDto(
            invoice.Id,
            invoice.Code,
            invoice.Status,
            invoice.BranchId,
            row.BranchName,
            invoice.RegisterId,
            row.RegisterName,
            invoice.WarehouseId,
            row.WarehouseName,
            customer,
            invoice.CashierId,
            row.CashierName,
            invoice.PaymentMethod,
            invoice.Subtotal,
            invoice.ItemsDiscount,
            invoice.PromotionId,
            invoice.PromotionName,
            invoice.ManualDiscountType,
            invoice.ManualDiscountValue,
            invoice.OrderDiscount,
            invoice.PointsRedeemed,
            invoice.PointsDiscount,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.Total,
            invoice.PaidAmount,
            invoice.TenderedAmount,
            invoice.ChangeAmount,
            invoice.DueAmount,
            invoice.PointsEarned,
            invoice.ReturnedAmount,
            invoice.Note,
            items,
            completed && userAppService.IsManager && invoice.ReturnedAmount == 0 && items.All(item => item.ReturnedQuantity == 0),
            completed && items.Any(item => item.ReturnedQuantity < item.Quantity),
            invoice.CreatedAt,
            invoice.CompletedAt,
            invoice.CancelledAt,
            invoice.CancelReason);
    }
}
