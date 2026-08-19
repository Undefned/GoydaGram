using MediatR;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands.DemoteToUser;

public class DemoteToUserCommandHandler(
    IUserRepository userRepository)
    : IRequestHandler<DemoteToUserCommand, DemoteToUserResult>
{
    public async Task<DemoteToUserResult> Handle(DemoteToUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId);
        if (user == null || !user.IsActive())
            throw new NotFoundException("User", command.UserId);

        if (user.Role == "User")
            throw new ValidationException("User is already a regular user");

        user.DemoteToUser();
        await userRepository.UpdateAsync(user);
        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new DemoteToUserResult(true);
    }
}