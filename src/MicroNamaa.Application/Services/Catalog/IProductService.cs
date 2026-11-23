using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Catalog;

namespace MicroNamaa.Application.Services.Catalog;

public interface IProductService
{
    Task<Result<PagedResult<ProductListItemDto>>> GetAllAsync(ProductQuery query, CancellationToken cancellationToken = default);

    Task<Result<ProductSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>The product with its stock per warehouse and its sales performance (units, revenue, demand share, margin).</summary>
    Task<Result<ProductDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<ProductDetailDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>Changes the product. Stock is changed through inventory adjustments, purchases and sales only.</summary>
    Task<Result<ProductDetailDto>> UpdateAsync(int id, ProductRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Sets the product photo, or removes it when <paramref name="file" /> is null.</summary>
    Task<Result<ProductDetailDto>> SetImageAsync(int id, FileUpload? file, CancellationToken cancellationToken = default);

    Task<Result<FileDownload>> ExportAsync(ProductQuery query, CancellationToken cancellationToken = default);
}
