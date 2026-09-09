namespace UserService.Application.DTOs;

public record UpdateProfileRequest(string? Username, string? AvatarUrl, string? Bio);