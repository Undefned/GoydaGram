using MediatR;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Queries.GetUserVideos;

public class GetUserVideosQueryHandler(
    IVideoRepository videoRepository,
    ICacheService cacheService)
    : IRequestHandler<GetUserVideosQuery, List<VideoDto>>
{
    public async Task<List<VideoDto>> Handle(GetUserVideosQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"user_videos:{query.UserId}:{query.Limit}:{query.Offset}";
        
        return await cacheService.GetOrSetAsync<List<VideoDto>>(cacheKey, async () =>
        {
            var videos = await videoRepository.GetByUserAsync(query.UserId, query.Limit, query.Offset);
            
            return videos.Select(v => new VideoDto(
                v.Id,
                v.UserId,
                v.Title,
                v.Description,
                v.Duration,
                v.Url,
                v.HlsPlaylistUrl ?? string.Empty,
                v.PreviewUrl,
                v.Status.ToString(),
                v.ViewsCount,
                v.LikesCount,
                v.CommentsCount,
                v.CreatedAt,
                v.Tags.Select(t => t.Name).ToList()
            )).ToList();
        }, TimeSpan.FromMinutes(5)) ?? new List<VideoDto>();
    }
}