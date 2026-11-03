using ECommerce.Application.Abstractions;
using MediatR;

namespace ECommerce.Application.Identity;

public record RegisterAccountCommand(string Email, string Password) : IRequest<AccountDto>;
public record GetCurrentAccountQuery : IRequest<AccountDto>;
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
public record RevokeSessionsCommand : IRequest;

public sealed class RegisterAccountHandler(IAccountService accounts) : IRequestHandler<RegisterAccountCommand, AccountDto>
{
    public Task<AccountDto> Handle(RegisterAccountCommand request, CancellationToken cancellationToken) =>
        accounts.RegisterAsync(request.Email, request.Password, cancellationToken);
}

public sealed class GetCurrentAccountHandler(IAccountService accounts, ICurrentUser currentUser)
    : IRequestHandler<GetCurrentAccountQuery, AccountDto>
{
    public Task<AccountDto> Handle(GetCurrentAccountQuery request, CancellationToken cancellationToken) =>
        accounts.GetAsync(currentUser.UserId ?? throw new UnauthorizedAccessException(), cancellationToken);
}

public sealed class ChangePasswordHandler(IAccountService accounts, ICurrentUser currentUser)
    : IRequestHandler<ChangePasswordCommand>
{
    public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken) =>
        accounts.ChangePasswordAsync(currentUser.UserId ?? throw new UnauthorizedAccessException(),
            request.CurrentPassword, request.NewPassword, cancellationToken);
}

public sealed class RevokeSessionsHandler(IAccountService accounts, ICurrentUser currentUser)
    : IRequestHandler<RevokeSessionsCommand>
{
    public Task Handle(RevokeSessionsCommand request, CancellationToken cancellationToken) =>
        accounts.RevokeSessionsAsync(currentUser.UserId ?? throw new UnauthorizedAccessException(), cancellationToken);
}
