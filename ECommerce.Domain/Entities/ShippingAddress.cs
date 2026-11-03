namespace ECommerce.Domain.Entities;

public sealed class ShippingAddress
{
    public string RecipientName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string AddressLine { get; private set; } = string.Empty;

    private ShippingAddress() { }

    public ShippingAddress(string recipientName, string phone, string countryCode, string city, string postalCode, string addressLine)
    {
        RecipientName = Normalize(recipientName, 100, nameof(recipientName));
        Phone = Normalize(phone, 30, nameof(phone));
        CountryCode = Normalize(countryCode, 2, nameof(countryCode)).ToUpperInvariant();
        if (CountryCode.Length != 2 || !CountryCode.All(char.IsAsciiLetter))
            throw new ArgumentException("Country code must contain two ASCII letters.", nameof(countryCode));
        City = Normalize(city, 100, nameof(city));
        PostalCode = Normalize(postalCode, 20, nameof(postalCode));
        AddressLine = Normalize(addressLine, 500, nameof(addressLine));
    }

    private static string Normalize(string value, int maxLength, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        value = value.Trim();
        if (value.Length > maxLength)
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameter);
        return value;
    }
}
