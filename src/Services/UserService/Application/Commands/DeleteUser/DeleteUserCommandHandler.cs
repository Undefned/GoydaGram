using MediatR;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands.DeleteUser;

public class DeleteUserCommandHandler(
    IUserRepository userRepository)
    : IRequestHandler<DeleteUserCommand, DeleteUserResult>
{
    public async Task<DeleteUserResult> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId);
        if (user == null)
            return new DeleteUserResult(false, "User not found");

        // Нельзя удалить самого себя (через API админа)
        // Проверка делается в контроллере
        if (user.Role == "Admin")
            return new DeleteUserResult(false, "Cannot delete admin user");

        await userRepository.DeleteAsync(command.UserId);
        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteUserResult(true, "User deleted successfully");
    }
}