using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Identity;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity;

public sealed class AdminBootstrapper(UserManager<ApplicationUser> users, ECommerceDbContext context)
{
    public async Task CreateAsync(string email, string password, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        email = email.Trim();
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("A valid administrator email is required.", nameof(email));
        if (password.Length > 128)
            throw new ArgumentException("Password cannot exceed 128 characters.", nameof(password));

        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, AccountRoles.Admin))
                throw new InvalidOperationException("An existing customer cannot be promoted by the bootstrap command.");
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email };
        IdentityValidation.EnsureSuccess(await users.CreateAsync(user, password));
        IdentityValidation.EnsureSuccess(await users.AddToRoleAsync(user, AccountRoles.Admin));
        await transaction.CommitAsync(cancellationToken);
    }
}
