namespace ECommerce.Domain.Entities;

public class ProductOptionValue
{
    public const int MaxValueLength = 100;

    public Guid Id { get; private set; }
    public Guid ProductOptionId { get; private set; }
    public string Value { get; private set; } = string.Empty;

    private ProductOptionValue() { }

    internal ProductOptionValue(Guid productOptionId, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        if (value.Length > MaxValueLength)
            throw new ArgumentException("Option value is too long.", nameof(value));

        Id = Guid.NewGuid();
        ProductOptionId = productOptionId;
        Value = value;
    }
}
