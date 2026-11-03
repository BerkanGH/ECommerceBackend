using System.Data;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Products;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class ProductRepository(ECommerceDbContext context) : IProductRepository
{
    public async Task<Product?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken)
            : null;
        IQueryable<Product> query = context.Products;
        if (!tracking)
            query = query.AsNoTrackingWithIdentityResolution();
        var product = await query.Include(product => product.Options).ThenInclude(option => option.Values)
            .Include(product => product.Variants).ThenInclude(variant => variant.Prices)
            .Include(product => product.Variants).ThenInclude(variant => variant.OptionValues)
            .AsSplitQuery()
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return product;
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken)
    {
        var normalized = EscapeLike(sku.Trim());
        return context.ProductVariants.AnyAsync(
            variant => EF.Functions.ILike(variant.Sku, normalized, "\\"), cancellationToken);
    }

    public void Add(Product product) => context.Products.Add(product);

    public async Task<PagedResult<ProductSummaryDto>> ListAsync(
        Guid? categoryId, string? search, int page, int pageSize, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = context.Products.AsNoTracking();
        if (!includeInactive)
            query = query.Where(product => product.IsActive);
        if (categoryId.HasValue)
            query = query.Where(product => product.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = "%" + EscapeLike(search.Trim()) + "%";
            query = query.Where(product => EF.Functions.ILike(product.Name, term, "\\"));
        }

        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(product => product.Name).ThenBy(product => product.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(product => new ProductSummaryDto(product.Id, product.CategoryId, product.Name, product.Description, product.IsActive))
            .ToArrayAsync(cancellationToken);
        return new PagedResult<ProductSummaryDto>(items, count, page, pageSize);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
