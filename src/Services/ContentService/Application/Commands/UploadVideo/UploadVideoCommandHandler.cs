using MediatR;
using ContentService.Application.Events;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Commands.UploadVideo;

public class UploadVideoCommandHandler(
    IVideoRepository videoRepository,
    IStorageService storageService,
    IThumbnailService thumbnailService,
    IVideoProcessingQueue processingQueue,
    IEventPublisher eventPublisher) : IRequestHandler<UploadVideoCommand, UploadVideoResult>
{
    public async Task<UploadVideoResult> Handle(UploadVideoCommand command, CancellationToken cancellationToken)
    {
        var videoId = Guid.NewGuid();
        var extension = Path.GetExtension(command.FileName);
        var videoPath = $"videos/{command.UserId}/{videoId}{extension}";
        var previewPath = $"previews/{command.UserId}/{videoId}.jpg";

        var url = await storageService.UploadFileAsync(
            videoPath, command.VideoStream, "video/mp4", cancellationToken);

        // Плейсхолдер-превью — быстро и синхронно, чтобы видео сразу появилось в ленте
        // с картинкой, не дожидаясь тяжёлой HLS-транскодизации.
        var previewUrl = await thumbnailService.GenerateThumbnailAsync(
            command.VideoStream, previewPath, cancellationToken);

        var video = Video.Create(
            command.UserId, command.Title, command.Description, 0, videoPath, url);
        video.MarkAsReady(previewUrl); // "Ready" = доступен прогрессивный MP4; HLS доедет отдельно

        var tags = new List<Tag>();
        foreach (var tagName in command.Tags.Distinct())
        {
            var tag = await videoRepository.GetOrCreateTagAsync(tagName);
            tags.Add(tag);
        }
        video.AddTags(tags);

        await videoRepository.AddAsync(video);
        await videoRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        // Search/Feed узнают о новом видео сразу, не дожидаясь окончания HLS-транскодинга.
        await eventPublisher.PublishAsync(new VideoUploadedEvent(
            video.Id, video.UserId, video.Title, tags.Select(t => t.Name).ToList(), video.CreatedAt));

        // HLS — тяжёлая CPU-задача (запуск ffmpeg), уходит в фон, не блокирует HTTP-ответ.
        await processingQueue.EnqueueAsync(video.Id);

        return new UploadVideoResult(
            video.Id,
            video.Url,
            video.PreviewUrl,
            video.Status.ToString()
        );
    }
}