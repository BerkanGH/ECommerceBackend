using ECommerce.Application.Categories;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<CategoryDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    void Add(Category category);
}
