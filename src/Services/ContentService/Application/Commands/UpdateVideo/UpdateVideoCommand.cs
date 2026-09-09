using MediatR;

namespace ContentService.Application.Commands.UpdateVideo;

public record UpdateVideoCommand(Guid VideoId, Guid RequestingUserId, string? Title, string? Description, List<string>? Tags)
    : IRequest<UpdateVideoResult>;

public record UpdateVideoResult(bool Success, string Message);