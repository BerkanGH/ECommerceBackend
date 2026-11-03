using System.Data;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Carts;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class CartCatalogReader(ECommerceDbContext context) : ICartCatalogReader
{
    public async Task<IReadOnlyDictionary<Guid, CartVariantSnapshot>> GetVariantsAsync(
        IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
            return new Dictionary<Guid, CartVariantSnapshot>();

        var ids = variantIds.Distinct().ToArray();
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken)
            : null;

        var variants = await (
            from variant in context.ProductVariants.AsNoTracking()
            join product in context.Products.AsNoTracking() on variant.ProductId equals product.Id
            where ids.Contains(variant.Id)
            select new { variant.Id, variant.ProductId, ProductName = product.Name, variant.Sku, variant.StockQuantity }
        ).ToArrayAsync(cancellationToken);

        var prices = await context.Set<ProductVariantPrice>().AsNoTracking()
            .Where(price => ids.Contains(price.ProductVariantId))
            .Select(price => new { price.ProductVariantId, price.Currency, price.Amount })
            .ToArrayAsync(cancellationToken);

        var selections = await (
            from selection in context.Set<ProductVariantOptionValue>().AsNoTracking()
            join value in context.Set<ProductOptionValue>().AsNoTracking() on selection.ProductOptionValueId equals value.Id
            join option in context.Set<ProductOption>().AsNoTracking() on value.ProductOptionId equals option.Id
            where ids.Contains(selection.ProductVariantId)
            select new { selection.ProductVariantId, option.Name, value.Value }
        ).ToArrayAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        var pricesByVariant = prices.ToLookup(price => price.ProductVariantId);
        var optionsByVariant = selections.ToLookup(selection => selection.ProductVariantId);
        return variants.ToDictionary(variant => variant.Id, variant => new CartVariantSnapshot(
            variant.Id, variant.ProductId, variant.ProductName, variant.Sku, variant.StockQuantity,
            optionsByVariant[variant.Id].OrderBy(option => option.Name)
                .Select(option => new CartSelectionDto(option.Name, option.Value)).ToArray(),
            pricesByVariant[variant.Id].OrderBy(price => price.Currency)
                .Select(price => new CartPriceSnapshot(price.Currency, price.Amount)).ToArray()));
    }
}
