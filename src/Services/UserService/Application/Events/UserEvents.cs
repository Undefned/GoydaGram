namespace UserService.Application.Events;

public record UserRegisteredEvent(Guid UserId, string Username, string Email);
public record UserSubscribedEvent(Guid FollowerId, Guid FolloweeId);