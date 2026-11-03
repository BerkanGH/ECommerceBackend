using ECommerce.Api.Contracts;
using ECommerce.Application.Identity;
using ECommerce.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AccountsController(
    ISender sender,
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signInManager,
    IOptionsMonitor<BearerTokenOptions> tokenOptions,
    TimeProvider timeProvider) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("register")]
    public async Task<ActionResult<AccountDto>> Register(RegisterAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await sender.Send(new RegisterAccountCommand(request.Email, request.Password), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, account);
    }

    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("login")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginAccountRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded || await users.GetTwoFactorEnabledAsync(user))
            return Unauthorized();

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        return SignIn(principal, IdentityConstants.BearerScheme);
    }

    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(RefreshAccountRequest request)
    {
        var protector = tokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var ticket = protector.Unprotect(request.RefreshToken);
        if (ticket?.Properties.ExpiresUtc is not { } expiresAt || expiresAt <= timeProvider.GetUtcNow())
            return Unauthorized();

        var user = await signInManager.ValidateSecurityStampAsync(ticket.Principal);
        if (user is null || !await signInManager.CanSignInAsync(user) || await users.IsLockedOutAsync(user))
            return Unauthorized();

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        return SignIn(principal, IdentityConstants.BearerScheme);
    }

    [Authorize]
    [HttpGet("me")]
    public Task<AccountDto> Me(CancellationToken cancellationToken) =>
        sender.Send(new GetCurrentAccountQuery(), cancellationToken);

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeSessionsCommand(), cancellationToken);
        return NoContent();
    }
}
