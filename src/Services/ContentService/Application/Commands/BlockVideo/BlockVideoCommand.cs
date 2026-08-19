using MediatR;

namespace ContentService.Application.Commands.BlockVideo;

public record BlockVideoCommand(
    Guid VideoId,
    string Reason
) : IRequest<BlockVideoResult>;
