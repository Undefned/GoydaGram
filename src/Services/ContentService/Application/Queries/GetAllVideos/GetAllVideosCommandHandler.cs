using MediatR;
using ContentService.Application.DTOs;
using ContentService.Domain.Enums;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Commands.GetAllVideos;

public class GetAllVideosQueryHandler(
    IVideoRepository videoRepository)
    : IRequestHandler<GetAllVideosQuery, AdminVideosResult>
{
    public async Task<AdminVideosResult> Handle(GetAllVideosQuery query, CancellationToken cancellationToken)
    {
        VideoStatus? statusFilter = query.Status?.ToLower() switch
        {
            "ready" => VideoStatus.Ready,
            "processing" => VideoStatus.Processing,
            "failed" => VideoStatus.Failed,
            "blocked" => VideoStatus.Blocked,
            _ => null
        };

        var (videos, total) = await videoRepository.GetAllForAdminAsync(query.Limit, query.Offset, statusFilter);

        var dtos = videos.Select(v => new VideoDto(
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

        return new AdminVideosResult(dtos, total, query.Offset, query.Limit);
    }
}