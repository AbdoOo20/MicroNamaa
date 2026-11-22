using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Application.DTOs.Sales;

using MicroNamaa.Application.Services.Inventory;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Sales;

public class PosService(IUnitOfWork unitOfWork, MerchantContext context, IUserAppService userAppService) : IPosService
{
    public async Task<Result<PosContextDto>> GetContextAsync(int? branchId, CancellationToken cancellationToken = default)
    {
        var branch = await context.ResolveBranchAsync(branchId, cancellationToken);

        if (!branch.Succeeded)
        {
            return Result<PosContextDto>.From(branch);
        }

        var warehouse = await context.GetSellingWarehouseAsync(branch.Data!, cancellationToken);

        if (warehouse is null)
        {
            return Result<PosContextDto>.Conflict("This branch has no active warehouse to sell from. Set one in the branch settings.");
        }

        var merchant = await context.GetMerchantAsync(cancellationToken);

        var userId = userAppService.RequiredUserId;

        var cashier = await unitOfWork.Repository<User>().Query().Where(user => user.Id == userId).Select(user => new { user.Id, user.FullName, user.AvatarPath, user.UpdatedAt, user.CreatedAt }).FirstAsync(cancellationToken);

        var branches = userAppService.IsCashier
            ? [new IdNameDto(branch.Data!.Id, branch.Data.Name)]
            : await unitOfWork.Repository<Branch>().Query().Where(item => item.IsActive).OrderBy(item => item.Id).Select(item => new IdNameDto(item.Id, item.Name)).ToListAsync(cancellationToken);

        var registers = await unitOfWork.Repository<Register>().Query()
            .Where(register => register.BranchId == branch.Data!.Id && register.IsActive)
            .OrderBy(register => register.Name)
            .Select(register => new RegisterDto(register.Id, register.BranchId, register.Name, register.IsActive))
            .ToListAsync(cancellationToken);

        var counts = await unitOfWork.Repository<Product>().Query()
            .Where(product => product.Status == RecordStatus.Active)
            .GroupBy(product => product.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var categories = await unitOfWork.Repository<Category>().Query()
            .Where(category => category.Status == RecordStatus.Active)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new { category.Id, category.Name })
            .ToListAsync(cancellationToken);

        var chips = new List<PosCategoryDto> { new(null, "All Items", counts.Sum(row => row.Count)) };

        chips.AddRange(categories.Select(category => new PosCategoryDto(category.Id, category.Name, counts.FirstOrDefault(row => row.CategoryId == category.Id)?.Count ?? 0)));

        var now = DateTime.UtcNow;

        var promotions = await unitOfWork.Repository<Promotion>().Query()
            .Where(promotion => promotion.Scope == PromotionScope.Order)
            .Where(Promotions.IsRunning(now))
            .OrderBy(promotion => promotion.Name)
            .Select(promotion => new PromotionDto(promotion.Id, promotion.Name, promotion.Type, promotion.Value, promotion.Scope, null, null, null, null, promotion.MinOrderAmount, promotion.StartsAt, promotion.EndsAt, promotion.IsActive, true))
            .ToListAsync(cancellationToken);

        return Result<PosContextDto>.Ok(new PosContextDto(
            merchant.Name,
            FileUrls.MerchantLogo(merchant.Id, merchant.LogoPath, merchant.UpdatedAt),
            new IdNameDto(branch.Data!.Id, branch.Data.Name),
            branches,
            registers,
            new IdNameDto(warehouse.Id, warehouse.Name),
            new IdNameDto(cashier.Id, cashier.FullName),
            FileUrls.UserAvatar(cashier.Id, cashier.AvatarPath, cashier.UpdatedAt ?? cashier.CreatedAt),
            merchant.TaxRate,
            Money.Currency,
            new PosLoyaltyDto(merchant.LoyaltyEnabled, merchant.PointsPerCurrencyUnit, merchant.PointValue, merchant.MinPointsToRedeem),
            chips,
            promotions,
            merchant.ReceiptFooter));
    }

    public async Task<Result<PagedResult<PosProductDto>>> GetProductsAsync(PosProductQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(query.BranchId, cancellationToken);

        if (!scope.Succeeded)
        {
            return Result<PagedResult<PosProductDto>>.From(scope);
        }

        var (warehouseId, threshold) = scope.Data;

        var products = unitOfWork.Repository<Product>().Query().Where(product => product.Status == RecordStatus.Active);

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            products = products.WhereMatchesSearch(query.Search);
        }

        if (query.InStockOnly)
        {
            products = products.Where(product => product.Stock.Where(item => item.WarehouseId == warehouseId).Sum(item => (int?)item.Quantity) > 0);
        }

        var promotions = await RunningItemPromotionsAsync(cancellationToken);

        var page = await products
            .OrderByDescending(product => product.Stock.Where(item => item.WarehouseId == warehouseId).Sum(item => (int?)item.Quantity) > 0)
            .ThenBy(product => product.Name)
            .ToPagedResultAsync(pageQuery => pageQuery.Select(product => new { Product = product, Available = product.Stock.Where(item => item.WarehouseId == warehouseId).Sum(item => (int?)item.Quantity) ?? 0 }), query.Page, query.PageSize, cancellationToken);

        var items = page.Items.Select(row => ToDto(row.Product, row.Available, threshold, promotions)).ToList();

        return Result<PagedResult<PosProductDto>>.Ok(new PagedResult<PosProductDto>(items, page.Page, page.PageSize, page.TotalCount));
    }

    public async Task<Result<PosProductDto>> LookupAsync(string code, int? branchId, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync(branchId, cancellationToken);

        if (!scope.Succeeded)
        {
            return Result<PosProductDto>.From(scope);
        }

        var (warehouseId, threshold) = scope.Data;

        var value = code.Trim();

        var row = await unitOfWork.Repository<Product>().Query()
            .Where(product => product.Status == RecordStatus.Active && (product.Barcode == value || product.Sku == value))
            .OrderBy(product => product.Barcode == value ? 0 : 1)
            .Select(product => new { Product = product, Available = product.Stock.Where(item => item.WarehouseId == warehouseId).Sum(item => (int?)item.Quantity) ?? 0 })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return Result<PosProductDto>.NotFound("No active product has this barcode or SKU.");
        }

        return Result<PosProductDto>.Ok(ToDto(row.Product, row.Available, threshold, await RunningItemPromotionsAsync(cancellationToken)));
    }

    private async Task<Result<(int WarehouseId, int Threshold)>> ScopeAsync(int? branchId, CancellationToken cancellationToken)
    {
        var branch = await context.ResolveBranchAsync(branchId, cancellationToken);

        if (!branch.Succeeded)
        {
            return Result<(int, int)>.From(branch);
        }

        var warehouse = await context.GetSellingWarehouseAsync(branch.Data!, cancellationToken);

        if (warehouse is null)
        {
            return Result<(int, int)>.Conflict("This branch has no active warehouse to sell from. Set one in the branch settings.");
        }

        return Result<(int, int)>.Ok((warehouse.Id, (await context.GetMerchantAsync(cancellationToken)).DefaultLowStockThreshold));
    }

    private async Task<IReadOnlyList<Promotion>> RunningItemPromotionsAsync(CancellationToken cancellationToken) =>
        await unitOfWork.Repository<Promotion>().Query()
            .Where(promotion => promotion.Scope != PromotionScope.Order)
            .Where(Promotions.IsRunning(DateTime.UtcNow))
            .ToListAsync(cancellationToken);

    private static PosProductDto ToDto(Product product, int available, int defaultThreshold, IReadOnlyList<Promotion> promotions)
    {
        var priced = SalesPricing.Calculate(new PricingInput([(product, 1)], promotions, null, null, null, 0, 0, 0, 0, false)).Lines[0];

        return new PosProductDto(
            product.Id,
            product.Name,
            product.Sku,
            product.Barcode,
            FileUrls.Product(product.Id, product.ImagePath, product.UpdatedAt ?? product.CreatedAt),
            product.CategoryId,
            product.Price,
            available,
            StockStates.Of(available, product.LowStockThreshold ?? defaultThreshold),
            priced.PromotionName,
            priced.Discount > 0 ? priced.LineTotal : null);
    }
}
