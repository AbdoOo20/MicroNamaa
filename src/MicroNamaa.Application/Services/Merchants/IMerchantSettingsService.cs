using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

namespace MicroNamaa.Application.Services.Merchants;

/// <summary>The store's own profile and settings (tax, stock, loyalty). Only the owner changes them.</summary>
public interface IMerchantSettingsService
{
    Task<Result<MerchantSettingsDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<Result<MerchantSettingsDto>> UpdateAsync(UpdateMerchantSettingsRequest request, CancellationToken cancellationToken = default);

    Task<Result<MerchantSettingsDto>> SetLogoAsync(FileUpload? file, CancellationToken cancellationToken = default);

    Task<Result<MerchantSettingsDto>> RemoveLogoAsync(CancellationToken cancellationToken = default);
}
