using MediatR;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands.PromoteToAdmin;

public class PromoteToAdminCommandHandler(
    IUserRepository userRepository)
    : IRequestHandler<PromoteToAdminCommand, PromoteToAdminResult>
{
    public async Task<PromoteToAdminResult> Handle(PromoteToAdminCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId);
        if (user == null || !user.IsActive())
            throw new NotFoundException("User", command.UserId);

        if (user.Role == "Admin")
            throw new ValidationException("User is already an admin");

        user.PromoteToAdmin();
        await userRepository.UpdateAsync(user);
        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new PromoteToAdminResult(true);
    }
}