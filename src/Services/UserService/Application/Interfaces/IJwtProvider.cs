using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

public interface IJwtProvider
{
    string GenerateToken(User user);
}