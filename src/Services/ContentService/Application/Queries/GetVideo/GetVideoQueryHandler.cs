using MediatR;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Exceptions;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Queries.GetVideo;

public class GetVideoQueryHandler(
    IVideoRepository videoRepository,
    ICacheService cacheService)
    : IRequestHandler<GetVideoQuery, VideoDto>
{
    public async Task<VideoDto> Handle(GetVideoQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"video:{query.VideoId}";
        
        return await cacheService.GetOrSetAsync<VideoDto>(cacheKey, async () =>
        {
            var video = await videoRepository.GetByIdWithTagsAsync(query.VideoId);
            if (video == null)
                throw new NotFoundException("Video", query.VideoId);

            return new VideoDto(
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
        }, TimeSpan.FromMinutes(5)) ?? null!;
    }
}