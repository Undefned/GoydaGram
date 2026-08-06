using MediatR;
using UserService.Application.Events;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands.Subscribe;

public class SubscribeCommandHandler(
    IUserRepository userRepository,
    IEventPublisher eventPublisher)
    : IRequestHandler<SubscribeCommand, SubscribeResult>
{
    public async Task<SubscribeResult> Handle(SubscribeCommand command, CancellationToken cancellationToken)
    {
        var follower = await userRepository.GetByIdAsync(command.FollowerId);
        if (follower == null || !follower.IsActive())
            throw new NotFoundException("User", command.FollowerId);

        var followee = await userRepository.GetByIdAsync(command.FolloweeId);
        if (followee == null || !followee.IsActive())
            throw new NotFoundException("User", command.FolloweeId);

        var alreadySubscribed = await userRepository.SubscriptionExistsAsync(follower.Id, followee.Id);
        if (alreadySubscribed)
            throw new ValidationException("Already subscribed");

        follower.RegisterSubscriptionTo(followee);

        await userRepository.AddSubscriptionAsync(follower.Id, followee.Id);
        await userRepository.UpdateAsync(follower);
        await userRepository.UpdateAsync(followee);
        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(new UserSubscribedEvent(
            follower.Id,
            followee.Id
        ));

        return new SubscribeResult(true);
    }
}