using MediatR;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Queries.GetBatch;

public class GetBatchQueryHandler(
    IVideoRepository videoRepository,
    ICacheService cacheService)
    : IRequestHandler<GetBatchQuery, List<VideoDto>>
{
    public async Task<List<VideoDto>> Handle(GetBatchQuery query, CancellationToken cancellationToken)
    {
        if (!query.VideoIds.Any())
            return new List<VideoDto>();

        // Try to get from cache first
        var result = new List<VideoDto>();
        var uncachedIds = new List<Guid>();

        foreach (var id in query.VideoIds)
        {
            var cached = await cacheService.GetAsync<VideoDto>($"video:{id}");
            if (cached != null)
                result.Add(cached);
            else
                uncachedIds.Add(id);
        }

        // Get uncached from DB
        if (uncachedIds.Any())
        {
            var videos = await videoRepository.GetBatchAsync(uncachedIds);
            foreach (var video in videos)
            {
                var dto = new VideoDto(
                    video.Id,
                    video.UserId,
                    video.Title,
                    video.Description,
                    video.Duration,
                    video.Url,
                    video.PreviewUrl,
                    video.Status.ToString(),
                    video.ViewsCount,
                    video.LikesCount,
                    video.CommentsCount,
                    video.CreatedAt,
                    video.Tags.Select(t => t.Name).ToList()
                );
                
                result.Add(dto);
                await cacheService.SetAsync($"video:{video.Id}", dto, TimeSpan.FromMinutes(5));
            }
        }

        return result;
    }
}