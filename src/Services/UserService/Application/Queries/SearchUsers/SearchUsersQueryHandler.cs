using MediatR;
using UserService.Application.DTOs;
using UserService.Domain.Interfaces;

namespace UserService.Application.Queries.SearchUsers;

public class SearchUsersQueryHandler(
    IUserRepository userRepository)
    : IRequestHandler<SearchUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(SearchUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await userRepository.SearchAsync(query.Query, query.Limit);
        
        return users.Select(u => new UserDto(
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