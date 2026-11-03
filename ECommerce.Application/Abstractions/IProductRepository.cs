using ECommerce.Application.Common;
using ECommerce.Application.Products;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken);
    Task<PagedResult<ProductSummaryDto>> ListAsync(Guid? categoryId, string? search, int page, int pageSize, CancellationToken cancellationToken);
    void Add(Product product);
}
