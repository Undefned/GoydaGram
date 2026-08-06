using MediatR;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands.LoginUser;

public class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider)
    : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email);
        if (user == null || !user.IsActive())
            throw new NotFoundException("User", command.Email);

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            throw new ValidationException("Invalid password");

        var token = jwtProvider.GenerateToken(user);
        return new LoginUserResult(user.Id, user.Username, user.Email, token);
    }
}