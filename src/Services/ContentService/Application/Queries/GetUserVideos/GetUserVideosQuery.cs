using MediatR;
using ContentService.Application.DTOs;

namespace ContentService.Application.Queries.GetUserVideos;

public record GetUserVideosQuery(
    Guid UserId,
    int Limit = 30,
    int Offset = 0
) : IRequest<List<VideoDto>>;