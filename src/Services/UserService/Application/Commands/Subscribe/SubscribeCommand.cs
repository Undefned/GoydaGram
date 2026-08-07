using MediatR;

namespace UserService.Application.Commands.Subscribe;

public record SubscribeCommand(
    Guid FollowerId,
    Guid FolloweeId
) : IRequest<SubscribeResult>;

public record SubscribeResult(bool Success);