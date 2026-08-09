using MediatR;

namespace ContentService.Application.Commands.UploadVideo;

public record UploadVideoCommand(
    Guid UserId,
    string Title,
    string Description,
    List<string> Tags,
    Stream VideoStream,
    string FileName
) : IRequest<UploadVideoResult>;

public record UploadVideoResult(
    Guid VideoId,
    string Url,
    string PreviewUrl,
    string Status
);