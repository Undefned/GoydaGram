using MediatR;

namespace ContentService.Application.Commands.UploadVideo;

public record UploadVideoCommand(
    Guid UserId,
    string Title,
    string Description,
    List<string> Tags,
    Stream VideoStream,
    string FileName,
    long FileSize
) : IRequest<UploadVideoResult>;