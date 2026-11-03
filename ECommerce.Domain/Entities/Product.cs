using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities;

public class Product
{
    private readonly List<ProductOption> _options = [];
    private readonly List<ProductVariant> _variants = [];

    public IReadOnlyCollection<ProductOption> Options => _options.AsReadOnly();
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 1000;

    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void SetAvailability(bool isActive) => IsActive = isActive;

    private Product() { }

    public ProductOption AddOption(string name)
    {
        EnsureOptionsEditable();
        if (_options.Count >= 10)
            throw new DomainRuleException("A product can have at most 10 options.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_options.Any(option => string.Equals(option.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainRuleException("Option name already exists.");

        var option = new ProductOption(Id, name);
        _options.Add(option);
        return option;
    }

    public ProductOptionValue AddOptionValue(Guid optionId, string value)
    {
        EnsureOptionsEditable();
        var option = _options.SingleOrDefault(option => option.Id == optionId)
            ?? throw new ArgumentException("Option does not belong to this product.", nameof(optionId));
        return option.AddValue(value);
    }

    public ProductVariant AddVariant(string sku, int stockQuantity, IEnumerable<Guid> optionValueIds)
    {
        ArgumentNullException.ThrowIfNull(optionValueIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        var selectedIds = optionValueIds.ToArray();
        if (selectedIds.Distinct().Count() != selectedIds.Length ||
            selectedIds.Length != _options.Count ||
            _options.Any(option => option.Values.Count(value => selectedIds.Contains(value.Id)) != 1))
            throw new ArgumentException("Select exactly one value from each product option.", nameof(optionValueIds));

        if (_variants.Any(variant => string.Equals(variant.Sku, sku.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainRuleException("SKU already exists in this product.");
        if (_variants.Any(variant => variant.OptionValues.Select(value => value.ProductOptionValueId).ToHashSet().SetEquals(selectedIds)))
            throw new DomainRuleException("Variant combination already exists.");

        var variant = new ProductVariant(Id, sku, stockQuantity, selectedIds);
        _variants.Add(variant);
        return variant;
    }

    private void EnsureOptionsEditable()
    {
        if (_variants.Count > 0)
            throw new DomainRuleException("Options cannot change after variants have been created.");
    }

    public Product(Guid categoryId, string name, string? description = null)
    {
        ChangeCategory(categoryId);
        UpdateDetails(name, description);
        Id = Guid.NewGuid();
    }

    public void ChangeCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));
        }

        CategoryId = categoryId;
    }

    public void UpdateDetails(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Product name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        if (normalizedDescription?.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Product description cannot exceed {MaxDescriptionLength} characters.", nameof(description));
        }

        Name = normalizedName;
        Description = normalizedDescription;
    }
}
