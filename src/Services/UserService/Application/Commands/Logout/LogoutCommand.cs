using MediatR;

namespace UserService.Application.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<LogoutResult>;

public record LogoutResult(bool Success);
