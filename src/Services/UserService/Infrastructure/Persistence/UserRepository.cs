using Microsoft.EntityFrameworkCore;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cache;
    public IUnitOfWork UnitOfWork { get; }

    private const string USER_CACHE_KEY = "user:{0}";
    private const string USER_BY_EMAIL_CACHE_KEY = "user:email:{0}";
    private const string USER_BY_USERNAME_CACHE_KEY = "user:username:{0}";
    private const string SUBSCRIPTIONS_CACHE_KEY = "user:{0}:subscriptions";
    private const string SUBSCRIPTION_EXISTS_CACHE_KEY = "user:{0}:subscribed:{1}";

    public UserRepository(AppDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
        UnitOfWork = new UnitOfWork(context);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var cacheKey = string.Format(USER_CACHE_KEY, id);
        return await _cache.GetOrSetAsync<User?>(cacheKey, async () =>
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var cacheKey = string.Format(USER_BY_EMAIL_CACHE_KEY, normalizedEmail);
        return await _cache.GetOrSetAsync<User?>(cacheKey, async () =>
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.DeletedAt == null);
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var cacheKey = string.Format(USER_BY_USERNAME_CACHE_KEY, username);
        return await _cache.GetOrSetAsync<User?>(cacheKey, async () =>
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.DeletedAt == null);
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<List<User>> GetSubscriptionsAsync(Guid userId)
    {
        var cacheKey = string.Format(SUBSCRIPTIONS_CACHE_KEY, userId);


        var result = await _cache.GetOrSetAsync<List<User>>(cacheKey, async () =>
        {
            var followingIds = await _context.Subscriptions
                .Where(s => s.FollowerId == userId)
                .Select(s => s.FolloweeId)
                .ToListAsync();

            return await _context.Users
                .Where(u => followingIds.Contains(u.Id) && u.DeletedAt == null)
                .ToListAsync();
        }, TimeSpan.FromMinutes(5));
        
        return result ?? new List<User>();
    }

    public async Task<List<Guid>> GetSubscriberIdsAsync(Guid userId)
        => await _context.Subscriptions
            .Where(s => s.FolloweeId == userId)
            .Select(s => s.FollowerId)
            .ToListAsync();

    public async Task<List<User>> GetBatchAsync(List<Guid> userIds)
    {
        // Для batch запроса берем из БД напрямую (или можно по одному из кеша, но это много запросов)
        return await _context.Users
            .Where(u => userIds.Contains(u.Id) && u.DeletedAt == null)
            .ToListAsync();
    }

    public async Task<bool> SubscriptionExistsAsync(Guid followerId, Guid followeeId)
    {
        var cacheKey = string.Format(SUBSCRIPTION_EXISTS_CACHE_KEY, followerId, followeeId);
        var cached = await _cache.GetAsync<bool?>(cacheKey);
        if (cached.HasValue)
            return cached.Value;

        var exists = await _context.Subscriptions
            .AnyAsync(s => s.FollowerId == followerId && s.FolloweeId == followeeId);
        
        await _cache.SetAsync(cacheKey, exists, TimeSpan.FromMinutes(5));
        return exists;
    }

    public async Task AddSubscriptionAsync(Guid followerId, Guid followeeId)
    {
        var subscription = Subscription.Create(followerId, followeeId);
        await _context.Subscriptions.AddAsync(subscription);
        
        // Инвалидируем кеш
        await InvalidateSubscriptionCacheAsync(followerId, followeeId);
    }

    public async Task<bool> RemoveSubscriptionAsync(Guid followerId, Guid followeeId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.FollowerId == followerId && s.FolloweeId == followeeId);

        if (subscription == null)
            return false;

        _context.Subscriptions.Remove(subscription);
        
        // Инвалидируем кеш
        await InvalidateSubscriptionCacheAsync(followerId, followeeId);
        
        return true;
    }

    public async Task AddAsync(User entity)
        => await _context.Users.AddAsync(entity);

    public Task UpdateAsync(User entity)
    {
        _context.Users.Update(entity);
        
        // Инвалидируем кеш после обновления
        var id = entity.Id;
        _ = Task.Run(async () =>
        {
            await _cache.RemoveAsync(string.Format(USER_CACHE_KEY, id));
            await _cache.RemoveAsync(string.Format(USER_BY_EMAIL_CACHE_KEY, entity.Email));
            await _cache.RemoveAsync(string.Format(USER_BY_USERNAME_CACHE_KEY, entity.Username));
        });
        
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            user.SoftDelete();
            _context.Users.Update(user);
            
            // Инвалидируем кеш
            await _cache.RemoveAsync(string.Format(USER_CACHE_KEY, id));
            await _cache.RemoveAsync(string.Format(USER_BY_EMAIL_CACHE_KEY, user.Email));
            await _cache.RemoveAsync(string.Format(USER_BY_USERNAME_CACHE_KEY, user.Username));
        }
    }

    public async Task<(List<User> Users, int Total)> GetAllAsync(int limit, int offset)
    {
        var query = _context.Users
            .Where(u => u.DeletedAt == null)
            .OrderBy(u => u.CreatedAt);

        var total = await query.CountAsync();
        var users = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (users, total);
    }

    public async Task<List<User>> SearchAsync(string query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<User>();

        return await _context.Users
            .Where(u => u.DeletedAt == null &&
                (u.Username.Contains(query) ||
                u.Email.Contains(query)))
            .OrderBy(u => u.Username)
            .Take(limit)
            .ToListAsync();
    }

    private async Task InvalidateSubscriptionCacheAsync(Guid followerId, Guid followeeId)
    {
        await _cache.RemoveAsync(string.Format(SUBSCRIPTIONS_CACHE_KEY, followerId));
        await _cache.RemoveAsync(string.Format(SUBSCRIPTION_EXISTS_CACHE_KEY, followerId, followeeId));
    }
}