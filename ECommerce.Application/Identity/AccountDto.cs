namespace ECommerce.Application.Identity;

public record AccountDto(Guid Id, string Email, IReadOnlyList<string> Roles);
