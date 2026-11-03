namespace ECommerce.Domain.Entities;

public class ProductVariantPrice
{
    public Guid Id { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }

    private ProductVariantPrice() { }

    internal ProductVariantPrice(Guid productVariantId, string currency, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        currency = currency.Trim().ToUpperInvariant();
        if (currency is not ("TRY" or "USD"))
            throw new ArgumentException("Only TRY and USD are supported.", nameof(currency));

        Id = Guid.NewGuid();
        ProductVariantId = productVariantId;
        Currency = currency;
        UpdateAmount(amount);
    }

    internal void UpdateAmount(decimal amount)
    {
        if (amount < 0 || amount > 9999999999999999.99m || decimal.Round(amount, 2) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Price must fit numeric(18,2) and be nonnegative.");

        Amount = amount;
    }
}
