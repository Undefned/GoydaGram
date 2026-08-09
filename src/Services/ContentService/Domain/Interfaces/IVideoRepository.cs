using ContentService.Domain.Entities;

namespace ContentService.Domain.Interfaces;

public interface IVideoRepository : IRepository<Video>
{
    Task<Video?> GetByIdWithTagsAsync(Guid id);
    Task<List<Video>> GetByUserAsync(Guid userId, int limit = 30);
    Task<List<Video>> GetBatchAsync(List<Guid> ids);
    Task<List<Video>> GetTrendingAsync(int limit = 30);
    Task<Tag?> GetTagByNameAsync(string name);
    Task<Tag> GetOrCreateTagAsync(string name);
    Task<List<Tag>> GetTagsByVideoAsync(Guid videoId);
}



