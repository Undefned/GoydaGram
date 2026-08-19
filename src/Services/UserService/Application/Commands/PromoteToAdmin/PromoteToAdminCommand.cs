using MediatR;

namespace UserService.Application.Commands.PromoteToAdmin;

public record PromoteToAdminCommand(Guid UserId) : IRequest<PromoteToAdminResult>;
public record PromoteToAdminResult(bool Success);