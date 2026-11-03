namespace ECommerce.Domain.Entities;

public class CartItem
{
    public const int MaxQuantity = 999;

    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public int Quantity { get; private set; }

    private CartItem() { }

    internal CartItem(Guid cartId, Guid productVariantId, int quantity)
    {
        Id = Guid.NewGuid();
        CartId = cartId;
        ProductVariantId = productVariantId;
        UpdateQuantity(quantity);
    }

    internal void UpdateQuantity(int quantity)
    {
        ValidateQuantity(quantity);
        Quantity = quantity;
    }

    internal static void ValidateQuantity(int quantity)
    {
        if (quantity is < 1 or > MaxQuantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 999.");
    }
}
