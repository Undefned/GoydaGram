using Microsoft.EntityFrameworkCore;
using ContentService.Domain.Entities;
using ContentService.Domain.Interfaces;

namespace ContentService.Infrastructure.Persistence;

public class VideoRepository : IVideoRepository
{
    private readonly AppDbContext _context;
    public IUnitOfWork UnitOfWork { get; }

    public VideoRepository(AppDbContext context)
    {
        _context = context;
        UnitOfWork = new UnitOfWork(context);
    }

    public async Task<Video?> GetByIdAsync(Guid id)
        => await _context.Videos
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<Video?> GetByIdWithTagsAsync(Guid id)
        => await _context.Videos
            .Include(v => v.Tags)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<List<Video>> GetByUserAsync(Guid userId, int limit = 30)
        => await _context.Videos
            .Where(v => v.UserId == userId && v.Status == Domain.Enums.VideoStatus.Ready)
            .OrderByDescending(v => v.CreatedAt)
            .Take(limit)
            .Include(v => v.Tags)
            .ToListAsync();

    public async Task<List<Video>> GetBatchAsync(List<Guid> ids)
        => await _context.Videos
            .Where(v => ids.Contains(v.Id) && v.Status == Domain.Enums.VideoStatus.Ready)
            .Include(v => v.Tags)
            .ToListAsync();

    public async Task<List<Video>> GetTrendingAsync(int limit = 30)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        return await _context.Videos
            .Where(v => v.Status == Domain.Enums.VideoStatus.Ready && v.CreatedAt >= sevenDaysAgo)
            .OrderByDescending(v => v.ViewsCount + v.LikesCount * 2 + v.CommentsCount * 3)
            .Take(limit)
            .Include(v => v.Tags)
            .ToListAsync();
    }

    public async Task<Tag?> GetTagByNameAsync(string name)
        => await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == name.ToLowerInvariant());

    public async Task<Tag> GetOrCreateTagAsync(string name)
    {
        var normalizedName = name.ToLowerInvariant();
        var tag = await GetTagByNameAsync(normalizedName);
        
        if (tag == null)
        {
            tag = Tag.Create(normalizedName);
            await _context.Tags.AddAsync(tag);
        }
        
        return tag;
    }

    public async Task<List<Tag>> GetTagsByVideoAsync(Guid videoId)
        => await _context.VideoTags
            .Where(vt => vt.VideoId == videoId)
            .Select(vt => vt.Tag)
            .ToListAsync();

    public async Task AddAsync(Video entity)
        => await _context.Videos.AddAsync(entity);

    public Task UpdateAsync(Video entity)
    {
        _context.Videos.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var video = await GetByIdAsync(id);
        if (video != null)
            _context.Videos.Remove(video);
    }
}