using MediatR;
using UserService.Application.DTOs;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Queries.GetSubscriptions;

public class GetSubscriptionsQueryHandler(
    IUserRepository userRepository)
    : IRequestHandler<GetSubscriptionsQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetSubscriptionsQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId);
        if (user == null || !user.IsActive())
            throw new NotFoundException("User", query.UserId);

        var subscriptions = await userRepository.GetSubscriptionsAsync(query.UserId);
        
        return subscriptions.Select(u => new UserDto(
            u.Id,
            u.Username,
            u.Email,
            u.AvatarUrl,
            u.Bio,
            u.IsVerified,
            u.FollowersCount,
            u.FollowingCount,
            u.CreatedAt,
            u.Role
        )).ToList();
    }
}