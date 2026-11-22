using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Inventory;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Inventory;

public class InventoryService(IUnitOfWork unitOfWork, MerchantContext context, StockLedger stockLedger) : IInventoryService
{
    public async Task<Result<PagedResult<StockRowDto>>> GetStockAsync(StockQuery query, CancellationToken cancellationToken = default)
    {
        var threshold = (await context.GetMerchantAsync(cancellationToken)).DefaultLowStockThreshold;

        var stock = unitOfWork.Repository<StockItem>().Query();

        if (query.WarehouseId is { } warehouseId)
        {
            stock = stock.Where(item => item.WarehouseId == warehouseId);
        }

        if (query.ProductId is { } productId)
        {
            stock = stock.Where(item => item.ProductId == productId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var matching = unitOfWork.Repository<Product>().Query().WhereMatchesSearch(query.Search).Select(product => product.Id);

            stock = stock.Where(item => matching.Contains(item.ProductId));
        }

        stock = query.StockState switch
        {
            StockState.OutOfStock => stock.Where(item => item.Quantity <= 0),

            StockState.LowStock => stock.Where(item => item.Quantity > 0 && item.Quantity <= (item.Product.LowStockThreshold ?? threshold)),

            StockState.InStock => stock.Where(item => item.Quantity > (item.Product.LowStockThreshold ?? threshold)),

            _ => stock
        };

        var page = await stock
            .OrderBy(item => item.Product.Name)
            .ThenBy(item => item.WarehouseId)
            .ToPagedResultAsync(
                pageQuery => pageQuery.Select(item => new StockRowDto(
                    item.ProductId,
                    item.Product.Name,
                    item.Product.Sku,
                    item.Product.ImagePath == null ? null : FileUrls.Product(item.ProductId, item.Product.ImagePath, item.Product.UpdatedAt ?? item.Product.CreatedAt),
                    item.WarehouseId,
                    item.Warehouse.Name,
                    item.Quantity,
                    item.Product.Cost,
                    item.Quantity > 0 ? item.Quantity * item.Product.Cost : 0,
                    item.Quantity <= 0 ? StockState.OutOfStock : item.Quantity <= (item.Product.LowStockThreshold ?? threshold) ? StockState.LowStock : StockState.InStock)),
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result<PagedResult<StockRowDto>>.Ok(page);
    }

    public async Task<Result<PagedResult<StockMovementDto>>> GetMovementsAsync(StockMovementQuery query, CancellationToken cancellationToken = default)
    {
        var movements = unitOfWork.Repository<StockMovement>().Query();

        if (query.ProductId is { } productId)
        {
            movements = movements.Where(movement => movement.ProductId == productId);
        }

        if (query.WarehouseId is { } warehouseId)
        {
            movements = movements.Where(movement => movement.WarehouseId == warehouseId);
        }

        if (query.Type is { } type)
        {
            movements = movements.Where(movement => movement.Type == type);
        }

        if (query.From is not null || query.To is not null)
        {
            var range = DateRange.FromLocalDates(query.From, query.To);

            movements = movements.Where(movement => movement.CreatedAt >= range.From && movement.CreatedAt < range.To);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            movements = movements.Where(movement => movement.ReferenceCode == search || movement.Product.Name.Contains(search) || movement.Product.Sku == search);
        }

        var users = unitOfWork.Repository<User>().Query();

        var page = await movements
            .OrderByDescending(movement => movement.Id)
            .ToPagedResultAsync(
                pageQuery => pageQuery.Select(movement => new StockMovementDto(
                    movement.Id,
                    movement.ProductId,
                    movement.Product.Name,
                    movement.Product.Sku,
                    movement.WarehouseId,
                    movement.Warehouse.Name,
                    movement.Type,
                    movement.Quantity,
                    movement.BalanceAfter,
                    movement.UnitCost,
                    movement.ReferenceType,
                    movement.ReferenceId,
                    movement.ReferenceCode,
                    movement.Note,
                    users.Where(user => user.Id == movement.CreatedBy).Select(user => user.FullName).FirstOrDefault(),
                    movement.CreatedAt)),
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result<PagedResult<StockMovementDto>>.Ok(page);
    }

    public async Task<Result<StockOperationResultDto>> AdjustAsync(StockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await unitOfWork.Repository<Warehouse>().AnyAsync(warehouse => warehouse.Id == request.WarehouseId, cancellationToken))
        {
            return Result<StockOperationResultDto>.NotFound("Warehouse was not found.");
        }

        if (request.Items.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
        {
            return Result<StockOperationResultDto>.BadRequest("Each product can appear once in a count.");
        }

        var productIds = request.Items.Select(item => item.ProductId).ToList();

        var products = await unitOfWork.Repository<Product>().Query()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new { product.Id, product.Cost, Quantity = product.Stock.Where(item => item.WarehouseId == request.WarehouseId).Sum(item => (int?)item.Quantity) ?? 0 })
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return Result<StockOperationResultDto>.NotFound("Product was not found.");
        }

        var reference = $"ADJ-{LibyaTime.Now:yyyyMMddHHmmss}";

        var changes = request.Items
            .Select(item => new StockChange(request.WarehouseId, item.ProductId, item.CountedQuantity - products[item.ProductId].Quantity, StockMovementType.Adjustment, products[item.ProductId].Cost, "StockAdjustment", null, reference, Clean(request.Note) ?? "Stock count"))
            .Where(change => change.Quantity != 0)
            .ToList();

        var applied = await stockLedger.ApplyAsync(changes, allowNegative: true, cancellationToken);

        if (!applied.Succeeded)
        {
            return Result<StockOperationResultDto>.From(applied);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StockOperationResultDto>.Ok(new StockOperationResultDto(reference, changes.Count, changes.Sum(change => Math.Abs(change.Quantity))), changes.Count == 0 ? "Nothing changed: the counts match the stock." : "Stock adjusted.");
    }

    public async Task<Result<StockOperationResultDto>> TransferAsync(StockTransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            return Result<StockOperationResultDto>.BadRequest("Choose two different warehouses.");
        }

        var warehouses = await unitOfWork.Repository<Warehouse>().Query()
            .Where(warehouse => warehouse.Id == request.FromWarehouseId || warehouse.Id == request.ToWarehouseId)
            .ToListAsync(cancellationToken);

        if (warehouses.Count != 2)
        {
            return Result<StockOperationResultDto>.NotFound("Warehouse was not found.");
        }

        if (warehouses.First(warehouse => warehouse.Id == request.ToWarehouseId).Status != RecordStatus.Active)
        {
            return Result<StockOperationResultDto>.Conflict("The receiving warehouse is not active.");
        }

        var lines = request.Items.GroupBy(item => item.ProductId).Select(group => (ProductId: group.Key, Quantity: group.Sum(item => item.Quantity))).ToList();

        var productIds = lines.Select(line => line.ProductId).ToList();

        var costs = await unitOfWork.Repository<Product>().Query().Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id, product => product.Cost, cancellationToken);

        if (costs.Count != productIds.Count)
        {
            return Result<StockOperationResultDto>.NotFound("Product was not found.");
        }

        var reference = $"TRF-{LibyaTime.Now:yyyyMMddHHmmss}";

        var note = Clean(request.Note);

        var fromName = warehouses.First(warehouse => warehouse.Id == request.FromWarehouseId).Name;

        var toName = warehouses.First(warehouse => warehouse.Id == request.ToWarehouseId).Name;

        var changes = lines
            .SelectMany(line => new[]
            {
                new StockChange(request.FromWarehouseId, line.ProductId, -line.Quantity, StockMovementType.TransferOut, costs[line.ProductId], "StockTransfer", null, reference, note ?? $"To {toName}"),

                new StockChange(request.ToWarehouseId, line.ProductId, line.Quantity, StockMovementType.TransferIn, costs[line.ProductId], "StockTransfer", null, reference, note ?? $"From {fromName}")
            })
            .ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var applied = await stockLedger.ApplyAsync(changes, allowNegative: false, cancellationToken);

        if (!applied.Succeeded)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<StockOperationResultDto>.From(applied);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<StockOperationResultDto>.Ok(new StockOperationResultDto(reference, lines.Count, lines.Sum(line => line.Quantity)), "Stock transferred.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
