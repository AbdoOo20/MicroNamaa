using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Catalog;

using MicroNamaa.Application.Services.Common;

using MicroNamaa.Application.Services.Inventory;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Catalog;

public class ProductService(IUnitOfWork unitOfWork, MerchantContext context, StockLedger stockLedger, ImageUploads images) : IProductService
{
    private const string NotFound = "Product was not found.";

    /// <summary>A product with its quantity, built with an object initializer so EF can keep filtering and sorting on it.</summary>
    private sealed class Row
    {
        public Product Product { get; init; } = null!;

        public int Quantity { get; init; }
    }

    public async Task<Result<PagedResult<ProductListItemDto>>> GetAllAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var threshold = (await context.GetMerchantAsync(cancellationToken)).DefaultLowStockThreshold;

        var rows = Sort(Filter(query, threshold), query.Sort);

        var page = await rows.ToPagedResultAsync(pageQuery => pageQuery.Select(ToListItem(threshold)), query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<ProductListItemDto>>.Ok(page);
    }

    public async Task<Result<ProductSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var threshold = (await context.GetMerchantAsync(cancellationToken)).DefaultLowStockThreshold;

        var rows = await unitOfWork.Repository<Product>().Query()
            .Select(product => new
            {
                product.Status,

                product.Cost,

                product.Price,

                Threshold = product.LowStockThreshold ?? threshold,

                Quantity = product.Stock.Sum(item => (int?)item.Quantity) ?? 0
            })
            .GroupBy(row => new { row.Status, Out = row.Quantity <= 0, Low = row.Quantity > 0 && row.Quantity <= row.Threshold })
            .Select(group => new
            {
                group.Key.Status,

                group.Key.Out,

                group.Key.Low,

                Count = group.Count(),

                Cost = group.Sum(row => row.Quantity > 0 ? row.Quantity * row.Cost : 0),

                Retail = group.Sum(row => row.Quantity > 0 ? row.Quantity * row.Price : 0)
            })
            .ToListAsync(cancellationToken);

        var total = rows.Sum(row => row.Count);

        var low = rows.Where(row => row.Low).Sum(row => row.Count);

        var outOfStock = rows.Where(row => row.Out).Sum(row => row.Count);

        var tabs = new List<SummaryCount> { new("all", total) };

        tabs.AddRange(Enum.GetValues<RecordStatus>().Select(status => new SummaryCount(status.ToString().ToLowerInvariant(), rows.Where(row => row.Status == status).Sum(row => row.Count))));

        tabs.Add(new SummaryCount("lowStock", low));

        tabs.Add(new SummaryCount("outOfStock", outOfStock));

        return Result<ProductSummaryDto>.Ok(new ProductSummaryDto(total, Money.Round(rows.Sum(row => row.Cost)), Money.Round(rows.Sum(row => row.Retail)), low, outOfStock, tabs));
    }

    public async Task<Result<ProductDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await LoadAsync(id, cancellationToken);

        return product is null ? Result<ProductDetailDto>.NotFound(NotFound) : Result<ProductDetailDto>.Ok(product);
    }

    public async Task<Result<ProductDetailDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (await ValidateAsync(null, request, cancellationToken) is { } invalid)
        {
            return Result<ProductDetailDto>.From(invalid);
        }

        Warehouse? warehouse = null;

        if (request.OpeningQuantity > 0)
        {
            warehouse = request.WarehouseId is { } warehouseId
                ? await unitOfWork.Repository<Warehouse>().Query().FirstOrDefaultAsync(item => item.Id == warehouseId && item.Status == RecordStatus.Active, cancellationToken)
                : await MainWarehouseAsync(cancellationToken);

            if (warehouse is null)
            {
                return Result<ProductDetailDto>.BadRequest("Choose an active warehouse for the opening stock.");
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var product = new Product();

        Apply(product, request);

        if (string.IsNullOrEmpty(product.Sku))
        {
            product.Sku = $"TMP-{Guid.NewGuid():N}";
        }

        await unitOfWork.Repository<Product>().AddAsync(product, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (product.Sku.StartsWith("TMP-", StringComparison.Ordinal))
        {
            product.Sku = $"SKU-{product.Id:00000}";
        }

        if (warehouse is not null)
        {
            var stocked = await stockLedger.ApplyAsync([new StockChange(warehouse.Id, product.Id, request.OpeningQuantity, StockMovementType.Opening, product.Cost, RelatedTypes.Product, product.Id, product.Sku, "Opening stock")], allowNegative: false, cancellationToken);

            if (!stocked.Succeeded)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);

                return Result<ProductDetailDto>.From(stocked);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<ProductDetailDto>.Created((await LoadAsync(product.Id, cancellationToken))!, "Product created.");
    }

    public async Task<Result<ProductDetailDto>> UpdateAsync(int id, ProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Repository<Product>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return Result<ProductDetailDto>.NotFound(NotFound);
        }

        if (await ValidateAsync(id, request, cancellationToken) is { } invalid)
        {
            return Result<ProductDetailDto>.From(invalid);
        }

        var sku = product.Sku;

        Apply(product, request);

        if (string.IsNullOrEmpty(product.Sku))
        {
            product.Sku = sku;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductDetailDto>.Ok((await LoadAsync(id, cancellationToken))!, "Product updated.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Repository<Product>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return Result.NotFound(NotFound);
        }

        // Invoices and purchase orders show the product, so a product with history is archived instead.
        if (await unitOfWork.Repository<SalesInvoiceItem>().AnyAsync(item => item.ProductId == id, cancellationToken) || await unitOfWork.Repository<PurchaseOrderItem>().AnyAsync(item => item.ProductId == id, cancellationToken))
        {
            return Result.Conflict("This product has sales or purchases. Archive it instead of deleting it.");
        }

        var stock = await unitOfWork.Repository<StockItem>().Query(asNoTracking: false).Where(item => item.ProductId == id).ToListAsync(cancellationToken);

        if (stock.Sum(item => item.Quantity) > 0)
        {
            return Result.Conflict("This product still has stock. Archive it, or adjust its stock to zero before deleting it.");
        }

        var promotions = await unitOfWork.Repository<Promotion>().Query(asNoTracking: false).Where(promotion => promotion.ProductId == id).ToListAsync(cancellationToken);

        unitOfWork.Repository<Promotion>().RemoveRange(promotions);

        unitOfWork.Repository<StockItem>().RemoveRange(stock);

        unitOfWork.Repository<Product>().Remove(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Product deleted.");
    }

    public async Task<Result<ProductDetailDto>> SetImageAsync(int id, FileUpload? file, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Repository<Product>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return Result<ProductDetailDto>.NotFound(NotFound);
        }

        if (file is null)
        {
            await images.DeleteAsync(product.ImagePath, cancellationToken);

            product.ImagePath = null;
        }

        else
        {
            var saved = await images.ReplaceAsync(product.ImagePath, file, "products", "product image", cancellationToken);

            if (!saved.Succeeded)
            {
                return Result<ProductDetailDto>.From(saved);
            }

            product.ImagePath = saved.Data;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductDetailDto>.Ok((await LoadAsync(id, cancellationToken))!);
    }

    public async Task<Result<FileDownload>> ExportAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var threshold = (await context.GetMerchantAsync(cancellationToken)).DefaultLowStockThreshold;

        var rows = await Sort(Filter(query, threshold), query.Sort).Select(ToListItem(threshold)).ToListAsync(cancellationToken);

        var csv = new CsvFile("Name", "SKU", "Barcode", "Category", "Quantity", "Cost (LYD)", "Price (LYD)", "Status", "Stock", "Created");

        foreach (var row in rows)
        {
            csv.Row(row.Name, row.Sku, row.Barcode, row.CategoryName, row.Quantity, row.Cost, row.Price, row.Status, row.StockState, row.CreatedAt);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("products"));
    }

    private IQueryable<Row> Filter(ProductQuery query, int defaultThreshold)
    {
        var products = unitOfWork.Repository<Product>().Query();

        if (query.Status is { } status)
        {
            products = products.Where(product => product.Status == status);
        }

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            products = products.WhereMatchesSearch(query.Search);
        }

        var warehouseId = query.WarehouseId;

        var rows = products.Select(product => new Row { Product = product, Quantity = product.Stock.Where(item => warehouseId == null || item.WarehouseId == warehouseId).Sum(item => (int?)item.Quantity) ?? 0 });

        return query.StockState switch
        {
            StockState.OutOfStock => rows.Where(row => row.Quantity <= 0),

            StockState.LowStock => rows.Where(row => row.Quantity > 0 && row.Quantity <= (row.Product.LowStockThreshold ?? defaultThreshold)),

            StockState.InStock => rows.Where(row => row.Quantity > (row.Product.LowStockThreshold ?? defaultThreshold)),

            _ => rows
        };
    }

    private static IQueryable<Row> Sort(IQueryable<Row> rows, string? sort) => sort?.ToLowerInvariant() switch
    {
        "name" => rows.OrderBy(row => row.Product.Name),

        "price" => rows.OrderByDescending(row => row.Product.Price),

        "quantity" => rows.OrderBy(row => row.Quantity),

        _ => rows.OrderByDescending(row => row.Product.Id)
    };

    private static Expression<Func<Row, ProductListItemDto>> ToListItem(int defaultThreshold) => row => new ProductListItemDto(
        row.Product.Id,
        row.Product.Name,
        row.Product.Sku,
        row.Product.Barcode,
        row.Product.ImagePath == null ? null : FileUrls.Product(row.Product.Id, row.Product.ImagePath, row.Product.UpdatedAt ?? row.Product.CreatedAt),
        row.Product.CategoryId,
        row.Product.Category == null ? null : row.Product.Category.Name,
        row.Quantity,
        row.Product.Cost,
        row.Product.Price,
        row.Product.Status,
        row.Quantity <= 0 ? StockState.OutOfStock : row.Quantity <= (row.Product.LowStockThreshold ?? defaultThreshold) ? StockState.LowStock : StockState.InStock,
        row.Product.LowStockThreshold ?? defaultThreshold,
        row.Product.CreatedAt);

    private async Task<Result?> ValidateAsync(int? id, ProductRequest request, CancellationToken cancellationToken)
    {
        var products = unitOfWork.Repository<Product>().Query().Where(product => product.Id != id);

        var sku = request.Sku?.Trim();

        if (!string.IsNullOrEmpty(sku) && await products.AnyAsync(product => product.Sku == sku, cancellationToken))
        {
            return Result.Conflict("Another product already uses this SKU.");
        }

        var barcode = request.Barcode?.Trim();

        if (!string.IsNullOrEmpty(barcode) && await products.AnyAsync(product => product.Barcode == barcode, cancellationToken))
        {
            return Result.Conflict("Another product already uses this barcode.");
        }

        if (request.CategoryId is { } categoryId && !await unitOfWork.Repository<Category>().AnyAsync(category => category.Id == categoryId, cancellationToken))
        {
            return Result.BadRequest(NotFoundCategory);
        }

        return null;
    }

    private const string NotFoundCategory = "Category was not found.";

    private static void Apply(Product product, ProductRequest request)
    {
        product.Name = request.Name.Trim();

        product.Sku = request.Sku?.Trim() ?? string.Empty;

        product.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

        product.CategoryId = request.CategoryId;

        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        product.Cost = Money.Round(request.Cost);

        product.Price = Money.Round(request.Price);

        product.LowStockThreshold = request.LowStockThreshold;

        product.Status = request.Status;
    }

    private async Task<Warehouse?> MainWarehouseAsync(CancellationToken cancellationToken)
    {
        var branch = await unitOfWork.Repository<Branch>().Query(asNoTracking: true, item => item.DefaultWarehouse!).Where(item => item.IsActive).OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);

        return branch is null
            ? await unitOfWork.Repository<Warehouse>().Query().Where(item => item.Status == RecordStatus.Active).OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken)
            : await context.GetSellingWarehouseAsync(branch, cancellationToken);
    }

    private async Task<ProductDetailDto?> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var merchant = await context.GetMerchantAsync(cancellationToken);

        var product = await unitOfWork.Repository<Product>().Query()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                Product = item,

                CategoryName = item.Category == null ? null : item.Category.Name,

                Stock = item.Stock.Select(stock => new ProductStockDto(stock.WarehouseId, stock.Warehouse.Name, stock.Quantity)).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return null;
        }

        var entity = product.Product;

        var quantity = product.Stock.Sum(stock => stock.Quantity);

        var threshold = entity.LowStockThreshold ?? merchant.DefaultLowStockThreshold;

        var performance = await PerformanceAsync(entity, cancellationToken);

        return new ProductDetailDto(
            entity.Id,
            entity.Name,
            entity.Sku,
            entity.Barcode,
            entity.Description,
            FileUrls.Product(entity.Id, entity.ImagePath, entity.UpdatedAt ?? entity.CreatedAt),
            entity.CategoryId,
            product.CategoryName,
            entity.Cost,
            entity.Price,
            entity.LowStockThreshold,
            threshold,
            entity.Status,
            quantity,
            StockStates.Of(quantity, threshold),
            product.Stock.OrderByDescending(stock => stock.Quantity).ToList(),
            performance,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private async Task<ProductPerformanceDto> PerformanceAsync(Product product, CancellationToken cancellationToken)
    {
        var now = LibyaTime.Now;

        var trendStart = LibyaTime.MonthStartUtcOf(now.AddMonths(-5));

        var sold = unitOfWork.Repository<SalesInvoiceItem>().Query()
            .Where(item => item.SalesInvoice.Status == SalesInvoiceStatus.Completed && item.SalesInvoice.CompletedAt >= trendStart);

        var monthly = await sold
            .Where(item => item.ProductId == product.Id)
            .GroupBy(item => new { item.SalesInvoice.CompletedAt!.Value.AddHours(LibyaTime.OffsetHours).Year, item.SalesInvoice.CompletedAt!.Value.AddHours(LibyaTime.OffsetHours).Month })
            .Select(group => new
            {
                group.Key.Year,

                group.Key.Month,

                Units = group.Sum(item => item.Quantity - item.ReturnedQuantity),

                Revenue = group.Sum(item => item.Quantity == 0 ? 0 : item.LineTotal * (item.Quantity - item.ReturnedQuantity) / item.Quantity),

                Cost = group.Sum(item => item.UnitCost * (item.Quantity - item.ReturnedQuantity))
            })
            .ToListAsync(cancellationToken);

        var categoryMonthly = product.CategoryId is { } categoryId
            ? await sold
                .Where(item => item.Product.CategoryId == categoryId)
                .GroupBy(item => new { item.SalesInvoice.CompletedAt!.Value.AddHours(LibyaTime.OffsetHours).Year, item.SalesInvoice.CompletedAt!.Value.AddHours(LibyaTime.OffsetHours).Month })
                .Select(group => new { group.Key.Year, group.Key.Month, Units = group.Sum(item => item.Quantity - item.ReturnedQuantity) })
                .ToListAsync(cancellationToken)
            : [];

        var months = Enumerable.Range(0, 6).Select(offset => now.AddMonths(offset - 5)).Select(date => (date.Year, date.Month)).ToList();

        var trend = months
            .Select(month =>
            {
                var row = monthly.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month);

                return new MonthlyUnitsDto(month.Year, month.Month, row?.Units ?? 0, Money.Round(row?.Revenue ?? 0));
            })
            .ToList();

        var share = months.TakeLast(3)
            .Select(month =>
            {
                var units = monthly.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month)?.Units ?? 0;

                var categoryUnits = categoryMonthly.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month)?.Units ?? 0;

                return new DemandShareDto(month.Year, month.Month, units, categoryUnits, categoryUnits == 0 ? null : Math.Round(units * 100m / categoryUnits, 1));
            })
            .ToList();

        var current = monthly.FirstOrDefault(item => item.Year == now.Year && item.Month == now.Month);

        var margin = product.Price > 0 ? Math.Round((product.Price - product.Cost) / product.Price * 100m, 2) : (decimal?)null;

        return new ProductPerformanceDto(
            current?.Units ?? 0,
            Money.Round(current?.Revenue ?? 0),
            share[^1].SharePercent,
            share[^1].CategoryUnits,
            margin,
            Money.Round((current?.Revenue ?? 0) - (current?.Cost ?? 0)),
            trend,
            share);
    }
}
