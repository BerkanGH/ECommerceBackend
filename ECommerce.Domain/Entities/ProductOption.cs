using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class ProductOption
{
    public const int MaxNameLength = 100;
    private readonly List<ProductOptionValue> _values = [];

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<ProductOptionValue> Values => _values.AsReadOnly();

    private ProductOption() { }

    internal ProductOption(Guid productId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        name = name.Trim();
        if (name.Length > MaxNameLength)
            throw new ArgumentException("Option name is too long.", nameof(name));

        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
    }

    internal ProductOptionValue AddValue(string value)
    {
        if (_values.Count >= 100)
            throw new DomainRuleException("An option can have at most 100 values.");
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (_values.Any(existing => string.Equals(existing.Value, value.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainRuleException("Option value already exists.");

        var optionValue = new ProductOptionValue(Id, value);
        _values.Add(optionValue);
        return optionValue;
    }
}
