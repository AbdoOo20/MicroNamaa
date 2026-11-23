using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Catalog;

namespace MicroNamaa.Application.Services.Catalog;

public interface ICategoryService
{
    Task<Result<PagedResult<CategoryDto>>> GetAllAsync(CategoryQuery query, CancellationToken cancellationToken = default);

    Task<Result<CategorySummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<Result<CategoryDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<CategoryDto>> CreateAsync(CategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<CategoryDto>> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes the category; its products stay, without a category.</summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<CategoryDto>> SetImageAsync(int id, FileUpload? file, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(CategoryQuery query, CancellationToken cancellationToken = default);
}
