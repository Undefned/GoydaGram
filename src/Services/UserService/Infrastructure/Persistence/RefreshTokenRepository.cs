using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Infrastructure.Persistence;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token)
        => await _context.RefreshTokens.AddAsync(token);

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
        => await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
            token.Revoke();
    }
}
