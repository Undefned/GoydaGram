using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<List<User>> GetSubscriptionsAsync(Guid userId);
    Task<List<Guid>> GetSubscriberIdsAsync(Guid userId);
    Task<List<User>> GetBatchAsync(List<Guid> userIds);
    Task<bool> SubscriptionExistsAsync(Guid followerId, Guid followeeId);
    Task AddSubscriptionAsync(Guid followerId, Guid followeeId);
    Task<bool> RemoveSubscriptionAsync(Guid followerId, Guid followeeId);
}