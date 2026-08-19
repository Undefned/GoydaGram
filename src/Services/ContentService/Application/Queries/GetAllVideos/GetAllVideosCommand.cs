using MediatR;
using ContentService.Application.DTOs;

namespace ContentService.Application.Commands.GetAllVideos;

public record GetAllVideosQuery(
    int Limit = 50,
    int Offset = 0,
    string? Status = null
) : IRequest<AdminVideosResult>;

