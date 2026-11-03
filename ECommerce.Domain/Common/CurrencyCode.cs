namespace ECommerce.Domain.Common;

public static class CurrencyCode
{
    public const string Try = "TRY";
    public const string Usd = "USD";

    public static string Normalize(string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized is not (Try or Usd))
            throw new ArgumentException("Only TRY and USD are supported.", nameof(currency));
        return normalized;
    }
}
