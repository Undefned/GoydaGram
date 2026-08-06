using MediatR;

namespace UserService.Application.Commands.RegisterUser;

public record RegisterUserCommand(
    string Username,
    string Email,
    string Password
) : IRequest<RegisterUserResult>;

public record RegisterUserResult(
    Guid UserId,
    string Username,
    string Email,
    string Token
);