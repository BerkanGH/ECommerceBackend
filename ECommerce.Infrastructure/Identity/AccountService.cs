using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Identity;
using ECommerce.Domain.Exceptions;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity;

public sealed class AccountService(UserManager<ApplicationUser> users, ECommerceDbContext context) : IAccountService
{
    public async Task<AccountDto> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        email = email.Trim();
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("A valid email address is required.", nameof(email));
        if (password.Length > 128)
            throw new ArgumentException("Password cannot exceed 128 characters.", nameof(password));
        if (await users.FindByEmailAsync(email) is not null)
            throw new DomainRuleException("An account with this email already exists.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email };
        IdentityValidation.EnsureSuccess(await users.CreateAsync(user, password));
        IdentityValidation.EnsureSuccess(await users.AddToRoleAsync(user, AccountRoles.Customer));
        await transaction.CommitAsync(cancellationToken);
        return new AccountDto(user.Id, user.Email!, [AccountRoles.Customer]);
    }

    public async Task<AccountDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await RequireUserAsync(userId);
        var roles = await users.GetRolesAsync(user);
        return new AccountDto(user.Id, user.Email!, roles.Order().ToArray());
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);
        if (newPassword.Length > 128)
            throw new ArgumentException("Password cannot exceed 128 characters.", nameof(newPassword));
        var user = await RequireUserAsync(userId);
        IdentityValidation.EnsureSuccess(await users.ChangePasswordAsync(user, currentPassword, newPassword));
    }

    public async Task RevokeSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await RequireUserAsync(userId);
        IdentityValidation.EnsureSuccess(await users.UpdateSecurityStampAsync(user));
    }

    private async Task<ApplicationUser> RequireUserAsync(Guid userId) =>
        await users.FindByIdAsync(userId.ToString()) ?? throw new UnauthorizedAccessException();
}
