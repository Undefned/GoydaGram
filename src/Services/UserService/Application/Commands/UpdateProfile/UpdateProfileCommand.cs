using MediatR;

namespace UserService.Application.Commands;

public record UpdateProfileCommand(Guid UserId, string? Username, string? AvatarUrl, string? Bio)
    : IRequest<UpdateProfileResult>;