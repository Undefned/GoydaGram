using ContentService.Application.Events;
using ContentService.Application.Interfaces;
using ContentService.Domain.Interfaces;

namespace ContentService.Infrastructure.BackgroundJobs;

public class VideoProcessingBackgroundService : BackgroundService
{
    private readonly IVideoProcessingQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VideoProcessingBackgroundService> _logger;

    public VideoProcessingBackgroundService(
        IVideoProcessingQueue queue,
        IServiceProvider serviceProvider,
        ILogger<VideoProcessingBackgroundService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var videoId in _queue.DequeueAllAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
            var hlsService = scope.ServiceProvider.GetRequiredService<IHlsTranscodingService>();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var video = await repository.GetByIdAsync(videoId);
            if (video == null)
            {
                _logger.LogWarning("Video {VideoId} not found, skipping HLS processing", videoId);
                continue;
            }

            try
            {
                _logger.LogInformation("Starting HLS transcoding for video {VideoId}", videoId);

                var playlistObjectPath = await hlsService.TranscodeToHlsAsync(
                    video.Id, video.UserId, video.StorageKey, stoppingToken);

                var playlistUrl = storageService.GetFileUrl(playlistObjectPath);
                video.SetHlsPlaylist(playlistUrl);

                await repository.UpdateAsync(video);
                await repository.UnitOfWork.SaveChangesAsync(stoppingToken);

                await eventPublisher.PublishAsync(new VideoProcessedEvent(video.Id, video.UserId, playlistUrl));

                _logger.LogInformation("HLS transcoding finished for video {VideoId}", videoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HLS transcoding failed for video {VideoId}", videoId);
                video.MarkAsFailed();
                await repository.UpdateAsync(video);
                await repository.UnitOfWork.SaveChangesAsync(stoppingToken);
            }
        }
    }
}