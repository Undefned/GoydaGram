using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Queries.GetUser;

public record GetUserQuery(Guid UserId) : IRequest<UserDto>;