using MediatR;

namespace UserService.Application.Commands.Unsubscribe;

public record UnsubscribeCommand(
    Guid FollowerId,
    Guid FolloweeId
) : IRequest<UnsubscribeResult>;

public record UnsubscribeResult(bool Success);