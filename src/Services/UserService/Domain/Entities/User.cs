using UserService.Domain.Exceptions;

namespace UserService.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public bool IsVerified { get; private set; }
    public int FollowersCount { get; private set; }
    public int FollowingCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private User() { }

    public static User Create(string username, string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            IsVerified = false,
            FollowersCount = 0,
            FollowingCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string? username, string? avatarUrl, string? bio)
    {
        if (!string.IsNullOrWhiteSpace(username))
            Username = username;
        if (!string.IsNullOrWhiteSpace(avatarUrl))
            AvatarUrl = avatarUrl;
        if (!string.IsNullOrWhiteSpace(bio))
            Bio = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RegisterSubscriptionTo(User followee)
    {
        if (Id == followee.Id)
            throw new DomainException("Cannot subscribe to yourself");

        followee.IncrementFollowers();
        FollowingCount++;
    }

    public void RegisterUnsubscriptionFrom(User followee)
    {
        followee.DecrementFollowers();
        FollowingCount--;
    }

    public void IncrementFollowers() => FollowersCount++;
    public void DecrementFollowers() => FollowersCount = Math.Max(0, FollowersCount - 1);
    public void Verify() => IsVerified = true;
    public void SoftDelete() => DeletedAt = DateTime.UtcNow;
    public bool IsActive() => DeletedAt == null;
}