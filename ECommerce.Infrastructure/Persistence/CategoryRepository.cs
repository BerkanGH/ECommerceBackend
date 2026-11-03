using ECommerce.Application.Abstractions;
using ECommerce.Application.Categories;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class CategoryRepository(ECommerceDbContext context) : ICategoryRepository
{
    public Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Categories.AnyAsync(category => category.Id == id, cancellationToken);

    public void Add(Category category) => context.Categories.Add(category);

    public async Task<PagedResult<CategoryDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Categories.AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(category => category.Name).ThenBy(category => category.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(category => new CategoryDto(category.Id, category.Name, category.Description))
            .ToArrayAsync(cancellationToken);
        return new PagedResult<CategoryDto>(items, count, page, pageSize);
    }
}
