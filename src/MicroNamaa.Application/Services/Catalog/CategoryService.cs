using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Catalog;

using MicroNamaa.Application.Services.Common;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Catalog;

public class CategoryService(IUnitOfWork unitOfWork, ImageUploads images) : ICategoryService
{
    private const string NotFound = "Category was not found.";

    public async Task<Result<PagedResult<CategoryDto>>> GetAllAsync(CategoryQuery query, CancellationToken cancellationToken = default)
    {
        var page = await Filter(query)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToPagedResultAsync(Project, query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<CategoryDto>>.Ok(page);
    }

    public async Task<Result<CategorySummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var categories = unitOfWork.Repository<Category>().Query();

        var byStatus = await categories.GroupBy(category => category.Status).Select(group => new { group.Key, Count = group.Count() }).ToListAsync(cancellationToken);

        var products = await unitOfWork.Repository<Product>().CountAsync(cancellationToken: cancellationToken);

        var stockValue = await unitOfWork.Repository<StockItem>().Query().Where(item => item.Quantity > 0).SumAsync(item => (decimal?)(item.Quantity * item.Product.Cost), cancellationToken) ?? 0;

        var tabs = new List<SummaryCount> { new("all", byStatus.Sum(row => row.Count)) };

        tabs.AddRange(Enum.GetValues<RecordStatus>().Select(status => new SummaryCount(status.ToString().ToLowerInvariant(), byStatus.FirstOrDefault(row => row.Key == status)?.Count ?? 0)));

        return Result<CategorySummaryDto>.Ok(new CategorySummaryDto(byStatus.Sum(row => row.Count), products, Money.Round(stockValue), tabs));
    }

    public async Task<Result<CategoryDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await LoadAsync(id, cancellationToken);

        return category is null ? Result<CategoryDto>.NotFound(NotFound) : Result<CategoryDto>.Ok(category);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await unitOfWork.Repository<Category>().AnyAsync(category => category.Name == name, cancellationToken))
        {
            return Result<CategoryDto>.Conflict("A category with this name already exists.");
        }

        var category = new Category { Name = name, Description = Clean(request.Description), Status = request.Status, SortOrder = request.SortOrder };

        await unitOfWork.Repository<Category>().AddAsync(category, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryDto>.Created((await LoadAsync(category.Id, cancellationToken))!, "Category created.");
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Repository<Category>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return Result<CategoryDto>.NotFound(NotFound);
        }

        var name = request.Name.Trim();

        if (await unitOfWork.Repository<Category>().AnyAsync(item => item.Id != id && item.Name == name, cancellationToken))
        {
            return Result<CategoryDto>.Conflict("A category with this name already exists.");
        }

        category.Name = name;

        category.Description = Clean(request.Description);

        category.Status = request.Status;

        category.SortOrder = request.SortOrder;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryDto>.Ok((await LoadAsync(id, cancellationToken))!, "Category updated.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Repository<Category>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return Result.NotFound(NotFound);
        }

        var products = await unitOfWork.Repository<Product>().Query(asNoTracking: false).Where(product => product.CategoryId == id).ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CategoryId = null;
        }

        unitOfWork.Repository<Category>().Remove(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Category deleted.");
    }

    public async Task<Result<CategoryDto>> SetImageAsync(int id, FileUpload? file, CancellationToken cancellationToken = default)
    {
        var category = await unitOfWork.Repository<Category>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return Result<CategoryDto>.NotFound(NotFound);
        }

        if (file is null)
        {
            await images.DeleteAsync(category.ImagePath, cancellationToken);

            category.ImagePath = null;
        }

        else
        {
            var saved = await images.ReplaceAsync(category.ImagePath, file, "categories", "category image", cancellationToken);

            if (!saved.Succeeded)
            {
                return Result<CategoryDto>.From(saved);
            }

            category.ImagePath = saved.Data;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryDto>.Ok((await LoadAsync(id, cancellationToken))!);
    }

    public async Task<Result<FileDownload>> ExportAsync(CategoryQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await Project(Filter(query).OrderBy(category => category.SortOrder).ThenBy(category => category.Name)).ToListAsync(cancellationToken);

        var csv = new CsvFile("Name", "Description", "Status", "Products", "Units in stock", "Stock value (LYD)", "Created");

        foreach (var row in rows)
        {
            csv.Row(row.Name, row.Description, row.Status, row.ProductsCount, row.StockUnits, row.StockValue, row.CreatedAt);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("categories"));
    }

    private IQueryable<Category> Filter(CategoryQuery query)
    {
        var categories = unitOfWork.Repository<Category>().Query();

        if (query.Status is { } status)
        {
            categories = categories.Where(category => category.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            categories = categories.WhereMatchesSearch(query.Search);
        }

        return categories;
    }

    private Task<CategoryDto?> LoadAsync(int id, CancellationToken cancellationToken) =>
        Project(unitOfWork.Repository<Category>().Query().Where(category => category.Id == id)).FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<CategoryDto> Project(IQueryable<Category> categories) => categories.Select(category => new CategoryDto(
        category.Id,
        category.Name,
        category.Description,
        category.ImagePath == null ? null : FileUrls.Category(category.Id, category.ImagePath, category.UpdatedAt ?? category.CreatedAt),
        category.Status,
        category.SortOrder,
        category.Products.Count(),
        category.Products.SelectMany(product => product.Stock).Sum(item => (int?)item.Quantity) ?? 0,
        category.Products.SelectMany(product => product.Stock).Where(item => item.Quantity > 0).Sum(item => (decimal?)(item.Quantity * item.Product.Cost)) ?? 0,
        category.CreatedAt));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
