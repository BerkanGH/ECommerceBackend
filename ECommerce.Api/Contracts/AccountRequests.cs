using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.Contracts;

public record RegisterAccountRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(128, MinimumLength = 10)] string Password);

public record LoginAccountRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(128)] string Password);

public record RefreshAccountRequest([Required, StringLength(16384)] string RefreshToken);

public record ChangePasswordRequest(
    [Required, StringLength(128)] string CurrentPassword,
    [Required, StringLength(128, MinimumLength = 10)] string NewPassword);
