using MediatR;
using UserService.Application.DTOs;
using UserService.Domain.Interfaces;

namespace UserService.Application.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(
    IUserRepository userRepository)
    : IRequestHandler<GetAllUsersQuery, PaginatedUsersResult>
{
    public async Task<PaginatedUsersResult> Handle(GetAllUsersQuery query, CancellationToken cancellationToken)
    {
        var (users, total) = await userRepository.GetAllAsync(query.Limit, query.Offset);
        
        var dtos = users.Select(u => new UserDto(
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

        return new PaginatedUsersResult(dtos, total, query.Offset, query.Limit);
    }
}

public record PaginatedUsersResult(
    List<UserDto> Users,
    int Total,
    int Offset,
    int Limit
);