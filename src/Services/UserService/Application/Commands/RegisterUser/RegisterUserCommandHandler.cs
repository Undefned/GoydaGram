using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserService.Application.Events;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands.RegisterUser;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    IEventPublisher eventPublisher)
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // Быстрая проверка — покрывает подавляющее большинство случаев,
        // даёт понятное сообщение об ошибке без похода в try/catch.
        var existingEmail = await userRepository.GetByEmailAsync(command.Email);
        if (existingEmail != null)
            throw new ValidationException("Email already registered");

        var existingUsername = await userRepository.GetByUsernameAsync(command.Username);
        if (existingUsername != null)
            throw new ValidationException("Username already taken");

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(command.Username, command.Email, passwordHash);

        await userRepository.AddAsync(user);

        try
        {
            await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Гонка: два параллельных запроса прошли проверку выше одновременно,
            // и только один из INSERT-ов реально прошёл — второй ловит unique_violation.
            throw new ConflictException("Email or username already registered");
        }

        var token = jwtProvider.GenerateToken(user);

        await eventPublisher.PublishAsync(new UserRegisteredEvent(
            user.Id,
            user.Username,
            user.Email
        ));

        return new RegisterUserResult(user.Id, user.Username, user.Email, token);
    }
}