using System.Text.Json;
using StackExchange.Redis;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;

    // Короткий TTL для "не найдено" — если запись появится вскоре после промаха
    // (например, только что зарегистрировались), не хотим долго отдавать устаревший "не найдено".
    private static readonly TimeSpan NullCacheTtl = TimeSpan.FromSeconds(30);
    private const string NullMarker = "\u0000NULL\u0000";

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty || value == NullMarker)
                return default;

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET error for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET error for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis DELETE error for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis EXISTS error for key: {Key}", key);
            return false;
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        // Читаем напрямую (не через GetAsync), чтобы отличить "промах кеша" от "закешированный null".
        try
        {
            var raw = await _db.StringGetAsync(key);
            if (!raw.IsNullOrEmpty)
            {
                if (raw == NullMarker)
                    return default; // Закешированное "не существует" — не идём в БД.

                return JsonSerializer.Deserialize<T>(raw!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET error for key: {Key}", key);
        }

        var result = await factory();

        try
        {
            if (result != null)
            {
                var json = JsonSerializer.Serialize(result);
                await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(10));
            }
            else
            {
                // Кешируем сам факт отсутствия — короткий TTL, чтобы не бить по БД
                // на каждый повторный запрос несуществующего email/username (например,
                // при переборе логинов), но и не залипнуть надолго на устаревшем "нет".
                await _db.StringSetAsync(key, NullMarker, NullCacheTtl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET error for key: {Key}", key);
        }

        return result;
    }
}