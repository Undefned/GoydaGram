using MediatR;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands.Unsubscribe;

public class UnsubscribeCommandHandler(
    IUserRepository userRepository)
    : IRequestHandler<UnsubscribeCommand, UnsubscribeResult>
{
    public async Task<UnsubscribeResult> Handle(UnsubscribeCommand command, CancellationToken cancellationToken)
    {
        var follower = await userRepository.GetByIdAsync(command.FollowerId);
        if (follower == null || !follower.IsActive())
            throw new NotFoundException("User", command.FollowerId);

        var followee = await userRepository.GetByIdAsync(command.FolloweeId);
        if (followee == null || !followee.IsActive())
            throw new NotFoundException("User", command.FolloweeId);

        var removed = await userRepository.RemoveSubscriptionAsync(follower.Id, followee.Id);
        if (!removed)
            throw new NotFoundException("Subscription", $"{follower.Id}->{followee.Id}");

        follower.RegisterUnsubscriptionFrom(followee);

        await userRepository.UpdateAsync(follower);
        await userRepository.UpdateAsync(followee);
        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new UnsubscribeResult(true);
    }
}