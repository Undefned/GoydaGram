using MediatR;

namespace UserService.Application.Commands.DemoteToUser;

public record DemoteToUserCommand(Guid UserId) : IRequest<DemoteToUserResult>;
public record DemoteToUserResult(bool Success);