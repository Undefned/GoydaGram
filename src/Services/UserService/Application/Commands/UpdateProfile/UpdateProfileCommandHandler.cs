using MediatR;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands;

public class UpdateProfileCommandHandler(IUserRepository userRepository)
    : IRequestHandler<UpdateProfileCommand, UpdateProfileResult>
{
    public async Task<UpdateProfileResult> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId);
        if (user is null)
            return new UpdateProfileResult(false, "User not found");

        if (!string.IsNullOrWhiteSpace(command.Username) && command.Username != user.Username)
        {
            var existing = await userRepository.GetByUsernameAsync(command.Username);
            if (existing is not null && existing.Id != user.Id)
                return new UpdateProfileResult(false, "Username is already taken");
        }

        user.UpdateProfile(command.Username, command.AvatarUrl, command.Bio);
        await userRepository.UpdateAsync(user);
        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateProfileResult(true, "Profile updated successfully");
    }
}