using MediatR;

namespace UserService.Application.Commands.LoginUser;

public record LoginUserCommand(
    string Email,
    string Password
) : IRequest<LoginUserResult>;

public record LoginUserResult(
    Guid UserId,
    string Username,
    string Email,
    string AccessToken,
    string RefreshToken
);