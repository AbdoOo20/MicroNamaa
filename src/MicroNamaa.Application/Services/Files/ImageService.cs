using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Application.Services.Files;

public class ImageService(IUnitOfWork unitOfWork, IFileStorage storage) : IImageService
{
    public async Task<Result<FileDownload>> GetAsync(string area, int id, CancellationToken cancellationToken = default)
    {
        var path = area.ToLowerInvariant() switch
        {
            "products" => await unitOfWork.Repository<Product>().Query().IgnoreQueryFilters().Where(item => item.Id == id).Select(item => item.ImagePath).FirstOrDefaultAsync(cancellationToken),

            "categories" => await unitOfWork.Repository<Category>().Query().Where(item => item.Id == id).Select(item => item.ImagePath).FirstOrDefaultAsync(cancellationToken),

            "users" => await unitOfWork.Repository<User>().Query().Where(item => item.Id == id).Select(item => item.AvatarPath).FirstOrDefaultAsync(cancellationToken),

            "customers" => await unitOfWork.Repository<Customer>().Query().Where(item => item.Id == id).Select(item => item.AvatarPath).FirstOrDefaultAsync(cancellationToken),

            "suppliers" => await unitOfWork.Repository<Supplier>().Query().Where(item => item.Id == id).Select(item => item.LogoPath).FirstOrDefaultAsync(cancellationToken),

            "merchants" => await unitOfWork.Repository<Merchant>().Query().Where(item => item.Id == id).Select(item => item.LogoPath).FirstOrDefaultAsync(cancellationToken),

            _ => null
        };

        if (string.IsNullOrEmpty(path) || await storage.OpenAsync(path, cancellationToken) is not { } file)
        {
            return Result<FileDownload>.NotFound("Image was not found.");
        }

        return Result<FileDownload>.Ok(file);
    }
}
