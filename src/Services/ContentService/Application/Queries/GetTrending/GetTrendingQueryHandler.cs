using MediatR;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Queries.GetTrending;

public class GetTrendingQueryHandler(
    IVideoRepository videoRepository,
    ICacheService cacheService)
    : IRequestHandler<GetTrendingQuery, List<VideoDto>>
{
    public async Task<List<VideoDto>> Handle(GetTrendingQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = "trending:videos";
        
        return await cacheService.GetOrSetAsync<List<VideoDto>>(cacheKey, async () =>
        {
            var videos = await videoRepository.GetTrendingAsync(query.Limit);
            
            return videos.Select(v => new VideoDto(
                v.Id,
                v.UserId,
                v.Title,
                v.Description,
                v.Duration,
                v.Url,
                v.PreviewUrl,
                v.Status.ToString(),
                v.ViewsCount,
                v.LikesCount,
                v.CommentsCount,
                v.CreatedAt,
                v.Tags.Select(t => t.Name).ToList()
            )).ToList();
        }, TimeSpan.FromMinutes(5)) ?? [];
    }
}