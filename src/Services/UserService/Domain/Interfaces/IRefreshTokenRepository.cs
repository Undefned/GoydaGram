using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);

    // Ищем по хешу, не по user_id — на одного юзера может быть несколько
    // активных токенов (по одному на устройство), различаем их только по значению.
    Task<RefreshToken?> GetByHashAsync(string tokenHash);

    // Для logout-all (отозвать все сессии юзера сразу, например при смене пароля)
    Task RevokeAllForUserAsync(Guid userId);
}
