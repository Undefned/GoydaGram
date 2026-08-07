using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Queries.GetSubscriptions;

public record GetSubscriptionsQuery(Guid UserId) : IRequest<List<UserDto>>;