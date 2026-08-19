using MediatR;
using ContentService.Application.DTOs;

namespace ContentService.Application.Commands.GetAllVideos;

public record AdminVideosResult(
    List<VideoDto> Videos,
    int Total,
    int Offset,
    int Limit
);