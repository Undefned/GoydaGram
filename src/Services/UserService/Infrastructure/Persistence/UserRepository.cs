using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    public IUnitOfWork UnitOfWork { get; }

    public UserRepository(AppDbContext context)
    {
        _context = context;
        UnitOfWork = new UnitOfWork(context);
    }

    public async Task<User?> GetByIdAsync(Guid id)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && u.DeletedAt == null);

    public async Task<User?> GetByUsernameAsync(string username)
        => await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.DeletedAt == null);

    public async Task<List<User>> GetSubscriptionsAsync(Guid userId)
    {
        var followingIds = await _context.Subscriptions
            .Where(s => s.FollowerId == userId)
            .Select(s => s.FolloweeId)
            .ToListAsync();

        return await _context.Users
            .Where(u => followingIds.Contains(u.Id) && u.DeletedAt == null)
            .ToListAsync();
    }

    public async Task<List<Guid>> GetSubscriberIdsAsync(Guid userId)
        => await _context.Subscriptions
            .Where(s => s.FolloweeId == userId)
            .Select(s => s.FollowerId)
            .ToListAsync();

    public async Task<List<User>> GetBatchAsync(List<Guid> userIds)
        => await _context.Users
            .Where(u => userIds.Contains(u.Id) && u.DeletedAt == null)
            .ToListAsync();

    public async Task AddAsync(User entity)
        => await _context.Users.AddAsync(entity);

    public Task UpdateAsync(User entity)
    {
        _context.Users.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            user.SoftDelete();
            _context.Users.Update(user);
        }
    }

    public async Task<bool> SubscriptionExistsAsync(Guid followerId, Guid followeeId)
        => await _context.Subscriptions
            .AnyAsync(s => s.FollowerId == followerId && s.FolloweeId == followeeId);

    public async Task AddSubscriptionAsync(Guid followerId, Guid followeeId)
    {
        var subscription = Subscription.Create(followerId, followeeId);
        await _context.Subscriptions.AddAsync(subscription);
    }

    public async Task<bool> RemoveSubscriptionAsync(Guid followerId, Guid followeeId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.FollowerId == followerId && s.FolloweeId == followeeId);

        if (subscription == null)
            return false;

        _context.Subscriptions.Remove(subscription);
        return true;
    }
}