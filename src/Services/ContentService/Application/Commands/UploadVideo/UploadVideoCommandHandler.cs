using MediatR;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using ContentService.Domain.Exceptions;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Commands.UploadVideo;

public class UploadVideoCommandHandler(
    IVideoRepository videoRepository,
    IStorageService storageService,
    IThumbnailService thumbnailService) : IRequestHandler<UploadVideoCommand, UploadVideoResult>
{
    public async Task<UploadVideoResult> Handle(UploadVideoCommand command, CancellationToken cancellationToken)
    {
        // 1. Generate IDs
        var videoId = Guid.NewGuid();
        var extension = Path.GetExtension(command.FileName);
        var videoPath = $"videos/{command.UserId}/{videoId}{extension}";
        var previewPath = $"previews/{command.UserId}/{videoId}.jpg";

        // 2. Upload video to MinIO
        var url = await storageService.UploadFileAsync(
            videoPath,
            command.VideoStream,
            "video/mp4",
            cancellationToken);

        // 3. Generate preview (first frame)
        var previewUrl = await thumbnailService.GenerateThumbnailAsync(
            command.VideoStream,
            previewPath,
            cancellationToken);

        // 4. Create video entity
        var video = Video.Create(
            command.UserId,
            command.Title,
            command.Description,
            0, // Duration - we'll calculate later (optional)
            url);

        video.MarkAsReady(previewUrl);

        // 5. Add tags
        var tags = new List<Tag>();
        foreach (var tagName in command.Tags.Distinct())
        {
            var tag = await videoRepository.GetOrCreateTagAsync(tagName);
            tags.Add(tag);
        }
        video.AddTags(tags);

        // 6. Save to DB
        await videoRepository.AddAsync(video);
        await videoRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadVideoResult(
            video.Id,
            video.Url,
            video.PreviewUrl,
            video.Status.ToString()
        );
    }
}