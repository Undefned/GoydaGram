using System.Diagnostics;
using System.Text;
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

        // Получаем длительность видео до загрузки
        var duration = await GetVideoDurationAsync(command.VideoStream, cancellationToken);
        
        // Перематываем поток для загрузки
        command.VideoStream.Position = 0;

        var url = await storageService.UploadFileAsync(
            videoPath, command.VideoStream, "video/mp4", cancellationToken);

        // Перематываем поток для генерации превью
        command.VideoStream.Position = 0;

        // Генерируем превью из реального кадра видео
        var previewUrl = await thumbnailService.GenerateThumbnailAsync(
            command.VideoStream, previewPath, cancellationToken);

        var video = Video.Create(
            command.UserId, command.Title, command.Description, duration, videoPath, url);
        video.MarkAsReady(previewUrl);

        var tags = new List<Tag>();
        foreach (var tagName in command.Tags.Distinct())
        {
            var tag = await videoRepository.GetOrCreateTagAsync(tagName);
            tags.Add(tag);
        }
        video.AddTags(tags);

        await videoRepository.AddAsync(video);
        await videoRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(new VideoUploadedEvent(
            video.Id, video.UserId, video.Title, tags.Select(t => t.Name).ToList(), video.CreatedAt));

        await processingQueue.EnqueueAsync(video.Id);

        return new UploadVideoResult(
            video.Id,
            video.Url,
            video.PreviewUrl,
            video.Status.ToString()
        );
    }

    private async Task<int> GetVideoDurationAsync(Stream videoStream, CancellationToken cancellationToken)
    {
        var originalPosition = videoStream.Position;
        
        try
        {
            videoStream.Position = 0;

            var tempFile = Path.GetTempFileName();
            try
            {
                await using (var fileStream = File.Create(tempFile))
                {
                    await videoStream.CopyToAsync(fileStream, cancellationToken);
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0 && double.TryParse(output.Trim(), out var seconds))
                {
                    return (int)Math.Ceiling(seconds);
                }

                return 0;
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
        finally
        {
            videoStream.Position = originalPosition;
        }
    }
}