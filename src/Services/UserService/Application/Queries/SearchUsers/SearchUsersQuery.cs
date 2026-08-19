using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Queries.SearchUsers;

public record SearchUsersQuery(
    string Query,
    int Limit = 30
) : IRequest<List<UserDto>>;