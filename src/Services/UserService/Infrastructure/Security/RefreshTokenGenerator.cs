using System.Security.Cryptography;
using System.Text;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Security;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string GenerateToken()
    {
        // 64 байта случайных данных — с большим запасом против brute force,
        // base64url без паддинга, чтобы токен был безопасен в URL/заголовках.
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
