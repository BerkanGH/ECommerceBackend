namespace ECommerce.Application.Identity;

public sealed class AccountValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Account validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
