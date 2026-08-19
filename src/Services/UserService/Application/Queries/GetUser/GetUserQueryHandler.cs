using MediatR;
using UserService.Application.DTOs;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Queries.GetUser;

public class GetUserQueryHandler(
    IUserRepository userRepository)
    : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId);
        if (user == null || !user.IsActive())
            throw new NotFoundException("User", query.UserId);

        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.AvatarUrl,
            user.Bio,
            user.IsVerified,
            user.FollowersCount,
            user.FollowingCount,
            user.CreatedAt,
            user.Role
        );
    }
}