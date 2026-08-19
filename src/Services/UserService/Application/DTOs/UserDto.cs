namespace UserService.Application.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? AvatarUrl,
    string? Bio,
    bool IsVerified,
    int FollowersCount,
    int FollowingCount,
    DateTime CreatedAt,
    string Role
);