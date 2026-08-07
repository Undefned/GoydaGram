namespace UserService.Application.Interfaces;

public interface IRefreshTokenGenerator
{
    // Возвращает СЫРОЙ токен — этот отдаём клиенту, в БД он никогда не попадает.
    string GenerateToken();

    // Хеш для сравнения/хранения в БД (быстрый SHA-256, не bcrypt —
    // токен уже имеет полную энтропию, в отличие от пароля пользователя).
    string Hash(string token);
}
