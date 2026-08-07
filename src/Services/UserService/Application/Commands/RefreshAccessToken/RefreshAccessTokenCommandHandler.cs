using MediatR;
using Microsoft.Extensions.Options;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Security;
using DomainRefreshToken = UserService.Domain.Entities.RefreshToken;

namespace UserService.Application.Commands.RefreshAccessToken;

public class RefreshAccessTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtProvider jwtProvider,
    IRefreshTokenGenerator refreshTokenGenerator,
    IOptions<JwtOptions> jwtOptions)
    : IRequestHandler<RefreshAccessTokenCommand, RefreshAccessTokenResult>
{
    public async Task<RefreshAccessTokenResult> Handle(RefreshAccessTokenCommand command, CancellationToken cancellationToken)
    {
        var incomingHash = refreshTokenGenerator.Hash(command.RefreshToken);
        var existingToken = await refreshTokenRepository.GetByHashAsync(incomingHash);

        // Один и тот же общий ответ на "не существует", "истёк" и "уже отозван" —
        // не даём атакующему понять, какой из трёх случаев произошёл.
        if (existingToken == null || !existingToken.IsActive())
            throw new ValidationException("Invalid or expired refresh token");

        var user = await userRepository.GetByIdAsync(existingToken.UserId);
        if (user == null || !user.IsActive())
            throw new ValidationException("Invalid or expired refresh token");

        // Ротация: старый токен отзываем сразу, чтобы его нельзя было переиспользовать повторно
        // (single-use refresh token — если кто-то перехватил старое значение, оно уже мертво).
        existingToken.Revoke();

        var rawRefreshToken = refreshTokenGenerator.GenerateToken();
        var refreshTokenHash = refreshTokenGenerator.Hash(rawRefreshToken);
        var refreshTokenLifetime = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenExpiryDays);
        var newTokenEntity = DomainRefreshToken.Create(user.Id, refreshTokenHash, refreshTokenLifetime);
        await refreshTokenRepository.AddAsync(newTokenEntity);

        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtProvider.GenerateToken(user);

        return new RefreshAccessTokenResult(accessToken, rawRefreshToken);
    }
}
