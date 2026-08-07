using MediatR;
using UserService.Application.Interfaces;
using UserService.Domain.Interfaces;

namespace UserService.Application.Commands.Logout;

public class LogoutCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenGenerator refreshTokenGenerator)
    : IRequestHandler<LogoutCommand, LogoutResult>
{
    public async Task<LogoutResult> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var hash = refreshTokenGenerator.Hash(command.RefreshToken);
        var token = await refreshTokenRepository.GetByHashAsync(hash);

        // Токен не найден / уже отозван — с точки зрения клиента результат один и тот же:
        // "ты разлогинен". Не бросаем ошибку, чтобы logout был идемпотентным.
        if (token != null && token.IsActive())
        {
            token.Revoke();
            await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new LogoutResult(true);
    }
}
