using MediatR;

namespace UserService.Application.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId) : IRequest<DeleteUserResult>;
public record DeleteUserResult(bool Success, string Message);