using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class Cart
{
    public const int MaxItems = 100;
    private readonly List<CartItem> _items = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Currency { get; private set; } = CurrencyCode.Try;
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private Cart() { }

    public Cart(Guid userId, string currency)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        Id = Guid.NewGuid();
        UserId = userId;
        Currency = CurrencyCode.Normalize(currency);
    }

    public void ChangeCurrency(string currency)
    {
        var normalized = CurrencyCode.Normalize(currency);
        if (_items.Count > 0 && Currency != normalized)
            throw new DomainRuleException("Clear the cart before changing its currency.");
        Currency = normalized;
    }

    public CartItem AddItem(Guid productVariantId, int quantity)
    {
        CartItem.ValidateQuantity(quantity);
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("Variant ID cannot be empty.", nameof(productVariantId));

        var existing = _items.SingleOrDefault(item => item.ProductVariantId == productVariantId);
        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity + quantity);
            return existing;
        }

        if (_items.Count >= MaxItems)
            throw new DomainRuleException("A cart can have at most 100 different variants.");
        var item = new CartItem(Id, productVariantId, quantity);
        _items.Add(item);
        return item;
    }

    public CartItem SetQuantity(Guid productVariantId, int quantity)
    {
        CartItem.ValidateQuantity(quantity);
        var item = _items.SingleOrDefault(item => item.ProductVariantId == productVariantId)
            ?? throw new DomainRuleException("Variant is not in the cart.");
        item.UpdateQuantity(quantity);
        return item;
    }

    public void RemoveItem(Guid productVariantId) =>
        _items.RemoveAll(item => item.ProductVariantId == productVariantId);

    public void Clear() => _items.Clear();
}
