using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Application.Services.Common;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Application.Services.Merchants;

public class MerchantSettingsService(IUnitOfWork unitOfWork, MerchantContext context, ImageUploads images, IMerchantAccessService merchantAccess) : IMerchantSettingsService
{
    public async Task<Result<MerchantSettingsDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var merchant = await context.GetMerchantAsync(cancellationToken);

        return Result<MerchantSettingsDto>.Ok(ToDto(merchant));
    }

    public async Task<Result<MerchantSettingsDto>> UpdateAsync(UpdateMerchantSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var phone = LibyanPhone.Normalize(request.Phone);

        if (phone is null)
        {
            return Result<MerchantSettingsDto>.BadRequest(LibyanPhone.InvalidMessage);
        }

        var merchant = await LoadTrackedAsync(cancellationToken);

        merchant.Name = request.Name.Trim();

        merchant.OwnerName = request.OwnerName.Trim();

        merchant.Phone = phone;

        merchant.Email = Clean(request.Email);

        merchant.City = Clean(request.City);

        merchant.Address = Clean(request.Address);

        merchant.TaxNumber = Clean(request.TaxNumber);

        merchant.TaxRate = request.TaxRate;

        merchant.AllowNegativeStock = request.AllowNegativeStock;

        merchant.DefaultLowStockThreshold = request.DefaultLowStockThreshold;

        merchant.LoyaltyEnabled = request.LoyaltyEnabled;

        merchant.PointsPerCurrencyUnit = request.PointsPerCurrencyUnit;

        merchant.PointValue = request.PointValue;

        merchant.MinPointsToRedeem = request.MinPointsToRedeem;

        merchant.ReceiptFooter = Clean(request.ReceiptFooter);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<MerchantSettingsDto>.Ok(ToDto(merchant), "Settings saved.");
    }

    public async Task<Result<MerchantSettingsDto>> SetLogoAsync(FileUpload? file, CancellationToken cancellationToken = default)
    {
        var merchant = await LoadTrackedAsync(cancellationToken);

        var saved = await images.ReplaceAsync(merchant.LogoPath, file, "merchants", "logo", cancellationToken);

        if (!saved.Succeeded)
        {
            return Result<MerchantSettingsDto>.From(saved);
        }

        merchant.LogoPath = saved.Data;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<MerchantSettingsDto>.Ok(ToDto(merchant));
    }

    public async Task<Result<MerchantSettingsDto>> RemoveLogoAsync(CancellationToken cancellationToken = default)
    {
        var merchant = await LoadTrackedAsync(cancellationToken);

        await images.DeleteAsync(merchant.LogoPath, cancellationToken);

        merchant.LogoPath = null;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<MerchantSettingsDto>.Ok(ToDto(merchant));
    }

    public static MerchantSettingsDto ToDto(Merchant merchant) => new(
        merchant.Id,
        merchant.Code,
        merchant.Name,
        merchant.OwnerName,
        merchant.Phone,
        merchant.Email,
        merchant.City,
        merchant.Address,
        merchant.TaxNumber,
        FileUrls.MerchantLogo(merchant.Id, merchant.LogoPath, merchant.UpdatedAt),
        merchant.Status,
        merchant.SubscriptionEndsAt,
        merchant.MaxBranches,
        merchant.MaxUsers,
        merchant.TaxRate,
        merchant.AllowNegativeStock,
        merchant.DefaultLowStockThreshold,
        merchant.LoyaltyEnabled,
        merchant.PointsPerCurrencyUnit,
        merchant.PointValue,
        merchant.MinPointsToRedeem,
        merchant.ReceiptFooter,
        Money.Currency);

    private async Task<Merchant> LoadTrackedAsync(CancellationToken cancellationToken)
    {
        var merchant = await unitOfWork.Repository<Merchant>().Query(asNoTracking: false).FirstAsync(item => item.Id == context.MerchantId, cancellationToken);

        merchantAccess.Forget(merchant.Id);

        return merchant;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
