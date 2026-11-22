using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Application.Services.Inventory;

/// <summary>
/// The one place that changes stock. Every change writes a <see cref="StockMovement" /> with the balance after it,
/// refuses to go below zero unless the merchant allows it, and tells the managers when a product runs low or out.
/// The caller saves the changes (inside its transaction).
/// </summary>
public class StockLedger(IUnitOfWork unitOfWork)
{
    public async Task<Result> ApplyAsync(IReadOnlyList<StockChange> changes, bool allowNegative, CancellationToken cancellationToken = default)
    {
        if (changes.Count == 0)
        {
            return Result.Ok();
        }

        var productIds = changes.Select(change => change.ProductId).Distinct().ToList();

        var warehouseIds = changes.Select(change => change.WarehouseId).Distinct().ToList();

        var items = await unitOfWork.Repository<StockItem>().Query(asNoTracking: false)
            .Where(item => productIds.Contains(item.ProductId) && warehouseIds.Contains(item.WarehouseId))
            .ToListAsync(cancellationToken);

        var products = await unitOfWork.Repository<Product>().Query()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new { product.Id, product.MerchantId, product.Name, product.LowStockThreshold })
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return Result.NotFound("Product was not found.");
        }

        if (!allowNegative)
        {
            var shortages = changes
                .GroupBy(change => (change.WarehouseId, change.ProductId))
                .Select(group => new { group.Key.ProductId, Available = Quantity(items, group.Key.WarehouseId, group.Key.ProductId), Change = group.Sum(change => change.Quantity) })
                .Where(line => line.Change < 0 && line.Available + line.Change < 0)
                .Select(line => $"Only {Math.Max(line.Available, 0)} of \"{products[line.ProductId].Name}\" left in stock.")
                .ToList();

            if (shortages.Count > 0)
            {
                return Result.Conflict(string.Join(" ", shortages));
            }
        }

        var totalsBefore = await unitOfWork.Repository<StockItem>().Query()
            .Where(item => productIds.Contains(item.ProductId))
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToDictionaryAsync(row => row.ProductId, row => row.Quantity, cancellationToken);

        foreach (var change in changes)
        {
            var item = items.FirstOrDefault(row => row.WarehouseId == change.WarehouseId && row.ProductId == change.ProductId);

            if (item is null)
            {
                item = new StockItem { MerchantId = products[change.ProductId].MerchantId, WarehouseId = change.WarehouseId, ProductId = change.ProductId };

                items.Add(item);

                await unitOfWork.Repository<StockItem>().AddAsync(item, cancellationToken);
            }

            item.Quantity += change.Quantity;

            await unitOfWork.Repository<StockMovement>().AddAsync(new StockMovement
            {
                MerchantId = item.MerchantId,

                WarehouseId = change.WarehouseId,

                ProductId = change.ProductId,

                Type = change.Type,

                Quantity = change.Quantity,

                BalanceAfter = item.Quantity,

                UnitCost = Money.Round(change.UnitCost),

                ReferenceType = change.ReferenceType,

                ReferenceId = change.ReferenceId,

                ReferenceCode = change.ReferenceCode,

                Note = change.Note
            }, cancellationToken);
        }

        await AlertAsync(changes, products.ToDictionary(pair => pair.Key, pair => (pair.Value.MerchantId, pair.Value.Name, pair.Value.LowStockThreshold)), totalsBefore, cancellationToken);

        return Result.Ok();
    }

    private async Task AlertAsync(IReadOnlyList<StockChange> changes, Dictionary<int, (int MerchantId, string Name, int? Threshold)> products, Dictionary<int, int> totalsBefore, CancellationToken cancellationToken)
    {
        var merchantIds = products.Values.Select(product => product.MerchantId).Distinct().ToList();

        var defaults = await unitOfWork.Repository<Merchant>().Query()
            .Where(merchant => merchantIds.Contains(merchant.Id))
            .ToDictionaryAsync(merchant => merchant.Id, merchant => merchant.DefaultLowStockThreshold, cancellationToken);

        foreach (var group in changes.GroupBy(change => change.ProductId))
        {
            var product = products[group.Key];

            var before = totalsBefore.GetValueOrDefault(group.Key);

            var after = before + group.Sum(change => change.Quantity);

            var threshold = product.Threshold ?? defaults.GetValueOrDefault(product.MerchantId);

            Notification? alert = null;

            if (after <= 0 && before > 0)
            {
                alert = NotificationFactory.ForMerchant(product.MerchantId, NotificationTypes.OutOfStock, $"نفد المخزون: {product.Name}", "الكمية المتاحة وصلت صفر، اعمل طلب شراء من المورد.", RelatedTypes.Product, group.Key);
            }

            else if (after > 0 && after <= threshold && before > threshold)
            {
                alert = NotificationFactory.ForMerchant(product.MerchantId, NotificationTypes.LowStock, $"مخزون قليل: {product.Name}", $"باقي {after} قطعة بس.", RelatedTypes.Product, group.Key);
            }

            if (alert is not null)
            {
                await unitOfWork.Repository<Notification>().AddAsync(alert, cancellationToken);
            }
        }
    }

    private static int Quantity(List<StockItem> items, int warehouseId, int productId) =>
        items.FirstOrDefault(item => item.WarehouseId == warehouseId && item.ProductId == productId)?.Quantity ?? 0;
}
