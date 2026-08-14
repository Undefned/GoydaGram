using MediatR;
using ContentService.Application.DTOs;

namespace ContentService.Application.Queries.GetVideo;

public record GetVideoQuery(Guid VideoId) : IRequest<VideoDto>;