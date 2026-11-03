using ECommerce.Domain.Exceptions;

namespace ECommerce.Application.Carts;

internal static class CartAvailability
{
    public static void EnsureAvailable(CartVariantSnapshot variant, string currency, int quantity)
    {
        if (!variant.Prices.Any(price => price.Currency == currency))
            throw new DomainRuleException("Variant has no price in the cart currency.");
        if (variant.StockQuantity < quantity)
            throw new DomainRuleException("Requested quantity exceeds available stock.");
    }
}
