namespace ECommerce.Domain.Entities;

public class ProductVariantOptionValue
{
    public Guid ProductVariantId { get; private set; }
    public Guid ProductOptionValueId { get; private set; }

    private ProductVariantOptionValue() { }

    internal ProductVariantOptionValue(Guid productVariantId, Guid productOptionValueId)
    {
        ProductVariantId = productVariantId;
        ProductOptionValueId = productOptionValueId;
    }
}
