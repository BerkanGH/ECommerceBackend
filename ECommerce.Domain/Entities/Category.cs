namespace ECommerce.Domain.Entities;

public class Category
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 1000;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public Category(string name, string? description = null)
    {
        UpdateDetails(name, description);
        Id = Guid.NewGuid();
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
            throw new ArgumentException($"Category name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        if (normalizedDescription?.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Category description cannot exceed {MaxDescriptionLength} characters.", nameof(description));
        }

        Name = normalizedName;
        Description = normalizedDescription;
    }
}
