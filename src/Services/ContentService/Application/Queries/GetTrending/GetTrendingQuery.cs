using MediatR;
using ContentService.Application.DTOs;

namespace ContentService.Application.Queries.GetTrending;

public record GetTrendingQuery(int Limit = 30) : IRequest<List<VideoDto>>;