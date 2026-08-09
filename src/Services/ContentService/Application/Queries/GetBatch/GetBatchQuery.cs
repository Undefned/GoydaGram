using MediatR;
using ContentService.Application.DTOs;

namespace ContentService.Application.Queries.GetBatch;

public record GetBatchQuery(List<Guid> VideoIds) : IRequest<List<VideoDto>>;