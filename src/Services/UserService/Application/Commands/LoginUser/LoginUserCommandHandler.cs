using MediatR;
using Microsoft.Extensions.Options;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Security;

namespace UserService.Application.Commands.LoginUser;

public class LoginUserCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider,
    IRefreshTokenGenerator refreshTokenGenerator,
    IOptions<JwtOptions> jwtOptions)
    : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    public async Task<LoginUserResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email);

        if (user == null || !user.IsActive() || !passwordHasher.Verify(command.Password, user.PasswordHash))
            throw new ValidationException("Invalid email or password");

        // Каждый логин — новая refresh-сессия. Старые (с других устройств) не трогаем —
        // допускаем несколько параллельных активных токенов на юзера.
        var rawRefreshToken = refreshTokenGenerator.GenerateToken();
        var refreshTokenHash = refreshTokenGenerator.Hash(rawRefreshToken);
        var refreshTokenLifetime = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenExpiryDays);
        var refreshTokenEntity = RefreshToken.Create(user.Id, refreshTokenHash, refreshTokenLifetime);
        await refreshTokenRepository.AddAsync(refreshTokenEntity);
        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtProvider.GenerateToken(user);

        return new LoginUserResult(user.Id, user.Username, user.Email, accessToken, rawRefreshToken);
    }
}