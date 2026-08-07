namespace UserService.Application.DTOs;

public record AuthResponse(
    string Token,
    UserDto User
);