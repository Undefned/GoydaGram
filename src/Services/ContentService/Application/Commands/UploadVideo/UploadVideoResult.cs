namespace ContentService.Application.Commands.UploadVideo;
public record UploadVideoResult(
    Guid VideoId,
    string Url,
    string PreviewUrl,
    string Status
);