using MediatR;

namespace ContentService.Application.Commands.UnblockVideo;

public record UnblockVideoCommand(Guid VideoId) : IRequest<UnblockVideoResult>;

