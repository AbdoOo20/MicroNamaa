using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Application.Services.Sales;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Merchants;

public class PromotionService(IUnitOfWork unitOfWork) : IPromotionService
{
    private const string NotFound = "Promotion was not found.";

    public async Task<Result<PagedResult<PromotionDto>>> GetAllAsync(PromotionQuery query, CancellationToken cancellationToken = default)
    {
        var promotions = unitOfWork.Repository<Promotion>().Query();

        if (query.Scope is { } scope)
        {
            promotions = promotions.Where(promotion => promotion.Scope == scope);
        }

        if (query.RunningOnly)
        {
            promotions = promotions.Where(Promotions.IsRunning(DateTime.UtcNow));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            promotions = promotions.Where(promotion => promotion.Name.Contains(search));
        }

        var page = await Project(promotions.OrderByDescending(promotion => promotion.IsActive).ThenByDescending(promotion => promotion.Id)).ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<PromotionDto>>.Ok(page);
    }

    public async Task<Result<PromotionDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await LoadAsync(id, cancellationToken);

        return promotion is null ? Result<PromotionDto>.NotFound(NotFound) : Result<PromotionDto>.Ok(promotion);
    }

    public async Task<Result<PromotionDto>> CreateAsync(PromotionRequest request, CancellationToken cancellationToken = default)
    {
        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return Result<PromotionDto>.From(invalid);
        }

        var promotion = new Promotion();

        Apply(promotion, request);

        await unitOfWork.Repository<Promotion>().AddAsync(promotion, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PromotionDto>.Created((await LoadAsync(promotion.Id, cancellationToken))!, "Promotion created.");
    }

    public async Task<Result<PromotionDto>> UpdateAsync(int id, PromotionRequest request, CancellationToken cancellationToken = default)
    {
        var promotion = await unitOfWork.Repository<Promotion>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (promotion is null)
        {
            return Result<PromotionDto>.NotFound(NotFound);
        }

        if (await ValidateAsync(request, cancellationToken) is { } invalid)
        {
            return Result<PromotionDto>.From(invalid);
        }

        Apply(promotion, request);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PromotionDto>.Ok((await LoadAsync(id, cancellationToken))!, "Promotion updated.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await unitOfWork.Repository<Promotion>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (promotion is null)
        {
            return Result.NotFound(NotFound);
        }

        unitOfWork.Repository<Promotion>().Remove(promotion);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Promotion deleted.");
    }

    private async Task<Result?> ValidateAsync(PromotionRequest request, CancellationToken cancellationToken)
    {
        if (request.Type == DiscountType.Percentage && request.Value > 100)
        {
            return Result.BadRequest("A percentage cannot be more than 100.");
        }

        if (request.StartsAt is { } starts && request.EndsAt is { } ends && ends <= starts)
        {
            return Result.BadRequest("The end date must be after the start date.");
        }

        switch (request.Scope)
        {
            case PromotionScope.Product when request.ProductId is not { } productId || !await unitOfWork.Repository<Product>().AnyAsync(product => product.Id == productId, cancellationToken):
                return Result.BadRequest("Choose the product of this promotion.");

            case PromotionScope.Category when request.CategoryId is not { } categoryId || !await unitOfWork.Repository<Category>().AnyAsync(category => category.Id == categoryId, cancellationToken):
                return Result.BadRequest("Choose the category of this promotion.");

            case PromotionScope.Order or PromotionScope.Product or PromotionScope.Category:
                return null;

            default:
                return Result.BadRequest("Unknown promotion scope.");
        }
    }

    private static void Apply(Promotion promotion, PromotionRequest request)
    {
        promotion.Name = request.Name.Trim();

        promotion.Type = request.Type;

        promotion.Value = Money.Round(request.Value);

        promotion.Scope = request.Scope;

        promotion.ProductId = request.Scope == PromotionScope.Product ? request.ProductId : null;

        promotion.CategoryId = request.Scope == PromotionScope.Category ? request.CategoryId : null;

        promotion.MinOrderAmount = request.Scope == PromotionScope.Order ? request.MinOrderAmount : null;

        promotion.StartsAt = request.StartsAt;

        promotion.EndsAt = request.EndsAt;

        promotion.IsActive = request.IsActive;
    }

    private Task<PromotionDto?> LoadAsync(int id, CancellationToken cancellationToken) =>
        Project(unitOfWork.Repository<Promotion>().Query().Where(promotion => promotion.Id == id)).FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<PromotionDto> Project(IQueryable<Promotion> promotions)
    {
        var now = DateTime.UtcNow;

        return promotions.Select(promotion => new PromotionDto(
            promotion.Id,
            promotion.Name,
            promotion.Type,
            promotion.Value,
            promotion.Scope,
            promotion.ProductId,
            promotion.Product == null ? null : promotion.Product.Name,
            promotion.CategoryId,
            promotion.Category == null ? null : promotion.Category.Name,
            promotion.MinOrderAmount,
            promotion.StartsAt,
            promotion.EndsAt,
            promotion.IsActive,
            promotion.IsActive && (promotion.StartsAt == null || promotion.StartsAt <= now) && (promotion.EndsAt == null || promotion.EndsAt > now)));
    }
}
