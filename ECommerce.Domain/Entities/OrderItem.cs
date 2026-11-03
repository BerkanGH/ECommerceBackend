namespace ECommerce.Domain.Entities;

public record OrderLineDraft(Guid ProductVariantId, string ProductName, string Sku,
    string OptionsSummary, int Quantity, decimal UnitPrice);

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string OptionsSummary { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    private OrderItem() { }

    internal OrderItem(Guid orderId, OrderLineDraft draft)
    {
        if (draft.ProductVariantId == Guid.Empty)
            throw new ArgumentException("Variant ID cannot be empty.");
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.ProductName);
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.Sku);
        ArgumentNullException.ThrowIfNull(draft.OptionsSummary);
        if (draft.ProductName.Length > Product.MaxNameLength || draft.Sku.Length > ProductVariant.MaxSkuLength ||
            draft.OptionsSummary.Length > 2048)
            throw new ArgumentException("Order snapshot fields exceed their maximum length.");
        CartItem.ValidateQuantity(draft.Quantity);
        if (draft.UnitPrice < 0 || draft.UnitPrice > 9999999999999999.99m || decimal.Round(draft.UnitPrice, 2) != draft.UnitPrice)
            throw new ArgumentOutOfRangeException(nameof(draft), "Invalid unit price.");

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductVariantId = draft.ProductVariantId;
        ProductName = draft.ProductName;
        Sku = draft.Sku;
        OptionsSummary = draft.OptionsSummary;
        Quantity = draft.Quantity;
        UnitPrice = draft.UnitPrice;
    }
}
