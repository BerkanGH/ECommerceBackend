using ECommerce.Application.Identity;
using ECommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity;

internal static class IdentityValidation
{
    public static void EnsureSuccess(IdentityResult result)
    {
        if (result.Succeeded)
            return;
        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            throw new DomainRuleException("An account with this email already exists.");

        var errors = result.Errors.GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
        throw new AccountValidationException(errors);
    }
}
