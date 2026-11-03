using System.Security.Claims;
using ECommerce.Application.Abstractions;

namespace ECommerce.Api.Identity;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) == true;
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId : null;
}
