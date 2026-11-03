using ECommerce.Application.Identity;

namespace ECommerce.Application.Abstractions;

public interface IAccountService
{
    Task<AccountDto> RegisterAsync(string email, string password, CancellationToken cancellationToken);
    Task<AccountDto> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task RevokeSessionsAsync(Guid userId, CancellationToken cancellationToken);
}
