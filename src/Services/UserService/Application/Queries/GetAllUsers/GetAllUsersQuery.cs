using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Queries.GetAllUsers;

public record GetAllUsersQuery(
    int Limit = 50,
    int Offset = 0
) : IRequest<PaginatedUsersResult>;