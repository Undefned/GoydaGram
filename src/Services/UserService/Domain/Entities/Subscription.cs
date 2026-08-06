namespace UserService.Domain.Entities;

public class Subscription
{
    public Guid Id { get; private set; }
    public Guid FollowerId { get; private set; }
    public Guid FolloweeId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Subscription() { }

    public static Subscription Create(Guid followerId, Guid followeeId)
    {
        return new Subscription
        {
            Id = Guid.NewGuid(),
            FollowerId = followerId,
            FolloweeId = followeeId,
            CreatedAt = DateTime.UtcNow
        };
    }
}