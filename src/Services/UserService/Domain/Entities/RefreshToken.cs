namespace UserService.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    // Хранится ХЕШ токена, не сам токен — если утащат дамп БД, активные
    // сессии всё равно нельзя будет использовать напрямую.
    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime)
        };
    }

    public bool IsActive() => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

    public void Revoke() => RevokedAt = DateTime.UtcNow;
}
