using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using UserService.Application.Events;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Security;

namespace UserService.Application.Commands.RegisterUser;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    IRefreshTokenGenerator refreshTokenGenerator,
    IOptions<JwtOptions> jwtOptions,
    IEventPublisher eventPublisher)
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var existingEmail = await userRepository.GetByEmailAsync(command.Email);
        if (existingEmail != null)
            throw new ValidationException("Email already registered");

        var existingUsername = await userRepository.GetByUsernameAsync(command.Username);
        if (existingUsername != null)
            throw new ValidationException("Username already taken");

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(command.Username, command.Email, passwordHash);

        await userRepository.AddAsync(user);

        // Сразу выдаём refresh-токен вместе с регистрацией — юзер залогинен с первого запроса.
        var rawRefreshToken = refreshTokenGenerator.GenerateToken();
        var refreshTokenHash = refreshTokenGenerator.Hash(rawRefreshToken);
        var refreshTokenLifetime = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenExpiryDays);
        var refreshTokenEntity = RefreshToken.Create(user.Id, refreshTokenHash, refreshTokenLifetime);
        await refreshTokenRepository.AddAsync(refreshTokenEntity);

        try
        {
            await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new ConflictException("Email or username already registered");
        }

        var accessToken = jwtProvider.GenerateToken(user);

        await eventPublisher.PublishAsync(new UserRegisteredEvent(
            user.Id,
            user.Username,
            user.Email
        ));

        return new RegisterUserResult(user.Id, user.Username, user.Email, accessToken, rawRefreshToken);
    }
}