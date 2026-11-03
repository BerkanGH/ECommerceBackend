using ECommerce.Application.Abstractions;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Carts;

public sealed class CartViewBuilder(ICartCatalogReader catalog)
{
    public async Task<CartDto> BuildAsync(Cart? cart, CancellationToken cancellationToken)
    {
        if (cart is null)
            return new CartDto(null, CurrencyCode.Try, [], 0m, false);
        if (cart.Items.Count == 0)
            return new CartDto(cart.Id, cart.Currency, [], 0m, false);

        var variants = await catalog.GetVariantsAsync(
            cart.Items.Select(item => item.ProductVariantId).ToArray(), cancellationToken);
        var items = cart.Items.Select(item =>
        {
            if (!variants.TryGetValue(item.ProductVariantId, out var variant))
                return new CartItemDto(item.ProductVariantId, null, null, null, item.Quantity,
                    null, null, 0, [], false, "variant_unavailable");

            var price = variant.Prices.SingleOrDefault(price => price.Currency == cart.Currency);
            var issue = price is null ? "price_unavailable"
                : item.Quantity > variant.StockQuantity ? "insufficient_stock" : null;
            return new CartItemDto(variant.Id, variant.ProductId, variant.ProductName, variant.Sku,
                item.Quantity, price?.Amount, price?.Amount * item.Quantity, variant.StockQuantity,
                variant.Options, issue is null, issue);
        }).OrderBy(item => item.Sku).ThenBy(item => item.ProductVariantId).ToArray();
        decimal? total = items.All(item => item.LineTotal.HasValue)
            ? items.Sum(item => item.LineTotal!.Value) : null;
        return new CartDto(cart.Id, cart.Currency, items, total, items.All(item => item.IsAvailable));
    }
}
