using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Inventory;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Inventory;

public class WarehouseService(IUnitOfWork unitOfWork) : IWarehouseService
{
    private const string NotFound = "Warehouse was not found.";

    public async Task<Result<PagedResult<WarehouseDto>>> GetAllAsync(WarehouseQuery query, CancellationToken cancellationToken = default)
    {
        var page = await Filter(query).OrderBy(warehouse => warehouse.Status).ThenBy(warehouse => warehouse.Id).ToPagedResultAsync(Project, query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<WarehouseDto>>.Ok(page);
    }

    public async Task<Result<WarehouseSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var rows = await unitOfWork.Repository<Warehouse>().Query()
            .Select(warehouse => new { warehouse.Status, warehouse.Capacity, Units = warehouse.Stock.Sum(item => (int?)item.Quantity) ?? 0 })
            .ToListAsync(cancellationToken);

        var products = await unitOfWork.Repository<StockItem>().Query().Where(item => item.Quantity > 0).Select(item => item.ProductId).Distinct().CountAsync(cancellationToken);

        var withCapacity = rows.Where(row => row.Status == RecordStatus.Active && row.Capacity > 0).ToList();

        decimal? average = withCapacity.Count == 0 ? null : Math.Round(withCapacity.Average(row => Math.Max(row.Units, 0) * 100m / row.Capacity!.Value), 1);

        var tabs = new List<SummaryCount> { new("all", rows.Count) };

        tabs.AddRange(Enum.GetValues<RecordStatus>().Select(status => new SummaryCount(status.ToString().ToLowerInvariant(), rows.Count(row => row.Status == status))));

        return Result<WarehouseSummaryDto>.Ok(new WarehouseSummaryDto(rows.Count(row => row.Status == RecordStatus.Active), products, rows.Sum(row => Math.Max(row.Units, 0)), average, tabs));
    }

    public async Task<Result<WarehouseDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await LoadAsync(id, cancellationToken);

        return warehouse is null ? Result<WarehouseDto>.NotFound(NotFound) : Result<WarehouseDto>.Ok(warehouse);
    }

    public async Task<Result<WarehouseDto>> CreateAsync(WarehouseRequest request, CancellationToken cancellationToken = default)
    {
        if (await ValidateAsync(null, request, cancellationToken) is { } invalid)
        {
            return Result<WarehouseDto>.From(invalid);
        }

        var warehouse = new Warehouse();

        Apply(warehouse, request);

        await unitOfWork.Repository<Warehouse>().AddAsync(warehouse, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<WarehouseDto>.Created((await LoadAsync(warehouse.Id, cancellationToken))!, "Warehouse created.");
    }

    public async Task<Result<WarehouseDto>> UpdateAsync(int id, WarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await unitOfWork.Repository<Warehouse>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (warehouse is null)
        {
            return Result<WarehouseDto>.NotFound(NotFound);
        }

        if (await ValidateAsync(id, request, cancellationToken) is { } invalid)
        {
            return Result<WarehouseDto>.From(invalid);
        }

        if (request.Status != RecordStatus.Active && await unitOfWork.Repository<Branch>().AnyAsync(branch => branch.DefaultWarehouseId == id && branch.IsActive, cancellationToken))
        {
            return Result<WarehouseDto>.Conflict("A branch sells from this warehouse. Choose another selling warehouse for the branch first.");
        }

        Apply(warehouse, request);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<WarehouseDto>.Ok((await LoadAsync(id, cancellationToken))!, "Warehouse updated.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await unitOfWork.Repository<Warehouse>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (warehouse is null)
        {
            return Result.NotFound(NotFound);
        }

        if (await unitOfWork.Repository<StockItem>().AnyAsync(item => item.WarehouseId == id && item.Quantity != 0, cancellationToken))
        {
            return Result.Conflict("This warehouse still has stock. Transfer it or archive the warehouse instead.");
        }

        if (await unitOfWork.Repository<Branch>().AnyAsync(branch => branch.DefaultWarehouseId == id, cancellationToken))
        {
            return Result.Conflict("A branch sells from this warehouse. Choose another selling warehouse for the branch first.");
        }

        if (await unitOfWork.Repository<SalesInvoice>().AnyAsync(invoice => invoice.WarehouseId == id, cancellationToken) || await unitOfWork.Repository<PurchaseOrder>().AnyAsync(order => order.WarehouseId == id, cancellationToken))
        {
            return Result.Conflict("This warehouse has sales or purchases. Archive it instead of deleting it.");
        }

        unitOfWork.Repository<Warehouse>().Remove(warehouse);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Warehouse deleted.");
    }

    public async Task<Result<FileDownload>> ExportAsync(WarehouseQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await Project(Filter(query).OrderBy(warehouse => warehouse.Id)).ToListAsync(cancellationToken);

        var csv = new CsvFile("Name", "Location", "Branch", "Status", "Products", "Units", "Capacity", "Capacity used %", "Stock value (LYD)");

        foreach (var row in rows)
        {
            csv.Row(row.Name, row.Location, row.BranchName, row.Status, row.ProductsCount, row.TotalUnits, row.Capacity, row.CapacityPercent, row.StockValue);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("warehouses"));
    }

    private IQueryable<Warehouse> Filter(WarehouseQuery query)
    {
        var warehouses = unitOfWork.Repository<Warehouse>().Query();

        if (query.Status is { } status)
        {
            warehouses = warehouses.Where(warehouse => warehouse.Status == status);
        }

        if (query.BranchId is { } branchId)
        {
            warehouses = warehouses.Where(warehouse => warehouse.BranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            warehouses = warehouses.WhereMatchesSearch(query.Search);
        }

        return warehouses;
    }

    private async Task<Result?> ValidateAsync(int? id, WarehouseRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await unitOfWork.Repository<Warehouse>().AnyAsync(warehouse => warehouse.Id != id && warehouse.Name == name, cancellationToken))
        {
            return Result.Conflict("A warehouse with this name already exists.");
        }

        if (request.BranchId is { } branchId && !await unitOfWork.Repository<Branch>().AnyAsync(branch => branch.Id == branchId, cancellationToken))
        {
            return Result.BadRequest("Branch was not found.");
        }

        return null;
    }

    private static void Apply(Warehouse warehouse, WarehouseRequest request)
    {
        warehouse.Name = request.Name.Trim();

        warehouse.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();

        warehouse.BranchId = request.BranchId;

        warehouse.Capacity = request.Capacity;

        warehouse.Status = request.Status;
    }

    private Task<WarehouseDto?> LoadAsync(int id, CancellationToken cancellationToken) =>
        Project(unitOfWork.Repository<Warehouse>().Query().Where(warehouse => warehouse.Id == id)).FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<WarehouseDto> Project(IQueryable<Warehouse> warehouses) => warehouses
        .Select(warehouse => new
        {
            Warehouse = warehouse,

            BranchName = warehouse.Branch == null ? null : warehouse.Branch.Name,

            Products = warehouse.Stock.Count(item => item.Quantity > 0),

            Units = warehouse.Stock.Sum(item => (int?)item.Quantity) ?? 0,

            Value = warehouse.Stock.Where(item => item.Quantity > 0).Sum(item => (decimal?)(item.Quantity * item.Product.Cost)) ?? 0
        })
        .Select(row => new WarehouseDto(
            row.Warehouse.Id,
            row.Warehouse.Name,
            row.Warehouse.Location,
            row.Warehouse.BranchId,
            row.BranchName,
            row.Warehouse.Capacity,
            row.Warehouse.Status,
            row.Products,
            row.Units,
            row.Warehouse.Capacity > 0 ? Math.Round((decimal)(row.Units < 0 ? 0 : row.Units) * 100m / row.Warehouse.Capacity.Value, 1) : null,
            row.Value,
            row.Warehouse.CreatedAt));
}
