using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class ProductVariant
{
    private readonly List<ProductVariantPrice> _prices = [];
    private readonly List<ProductVariantOptionValue> _optionValues = [];
    public const int MaxSkuLength = 100;

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string CombinationKey { get; private set; } = string.Empty;
    public IReadOnlyCollection<ProductVariantPrice> Prices => _prices.AsReadOnly();
    public IReadOnlyCollection<ProductVariantOptionValue> OptionValues => _optionValues.AsReadOnly();
    public int StockQuantity { get; private set; }

    private ProductVariant() { }

    internal ProductVariant(Guid productId, string sku, int stockQuantity, IEnumerable<Guid> optionValueIds)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        }

        UpdateSku(sku);
        UpdateStockQuantity(stockQuantity);
        ProductId = productId;
        Id = Guid.NewGuid();
        var selectedIds = optionValueIds.Order().ToArray();
        CombinationKey = string.Join(",", selectedIds.Select(id => id.ToString("N")));
        foreach (var optionValueId in selectedIds)
            _optionValues.Add(new ProductVariantOptionValue(Id, optionValueId));
    }

    internal void UpdateSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        var normalizedSku = sku.Trim();

        if (normalizedSku.Length > MaxSkuLength)
        {
            throw new ArgumentException($"SKU cannot exceed {MaxSkuLength} characters.", nameof(sku));
        }

        Sku = normalizedSku;
    }

    public void SetPrice(string currency, decimal amount)
    {
        var candidate = new ProductVariantPrice(Id, currency, amount);
        var existing = _prices.SingleOrDefault(price => price.Currency == candidate.Currency);
        if (existing is null)
            _prices.Add(candidate);
        else
            existing.UpdateAmount(amount);
    }

    public void UpdateStockQuantity(int stockQuantity)
    {
        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");
        }

        StockQuantity = stockQuantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (StockQuantity < quantity)
            throw new DomainRuleException("Insufficient stock.");
        StockQuantity -= quantity;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity < 1)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (StockQuantity > int.MaxValue - quantity)
            throw new DomainRuleException("Stock capacity would be exceeded.");
        StockQuantity += quantity;
    }
}
