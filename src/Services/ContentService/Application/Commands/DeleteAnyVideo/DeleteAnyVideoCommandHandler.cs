using MediatR;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Domain.Exceptions;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Commands.DeleteAnyVideo;

public class DeleteAnyVideoCommandHandler : IRequestHandler<DeleteAnyVideoCommand, DeleteAnyVideoResult>
{
    private readonly IVideoRepository _videoRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<DeleteAnyVideoCommandHandler> _logger;

    public DeleteAnyVideoCommandHandler(
        IVideoRepository videoRepository,
        IStorageService storageService,
        ILogger<DeleteAnyVideoCommandHandler> logger)
    {
        _videoRepository = videoRepository;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<DeleteAnyVideoResult> Handle(DeleteAnyVideoCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Находим видео
            var video = await _videoRepository.GetByIdAsync(command.VideoId);
            
            if (video == null)
            {
                return new DeleteAnyVideoResult(false, "Video not found");
            }

            // 2. Удаляем файлы из хранилища
            // Удаляем оригинальное видео
            if (!string.IsNullOrEmpty(video.Url))
            {
                var originalPath = ExtractPathFromUrl(video.Url);
                await _storageService.DeleteFileAsync(originalPath, cancellationToken);
            }

            // Удаляем HLS файлы (если есть)
            if (!string.IsNullOrEmpty(video.HlsPlaylistUrl))
            {
                var hlsPath = ExtractPathFromUrl(video.HlsPlaylistUrl);
                // Удаляем всю папку с HLS
                await _storageService.DeleteFileAsync(Path.GetDirectoryName(hlsPath)!, cancellationToken);
            }

            // Удаляем превью (если есть)
            if (!string.IsNullOrEmpty(video.PreviewUrl))
            {
                var previewPath = ExtractPathFromUrl(video.PreviewUrl);
                await _storageService.DeleteFileAsync(previewPath, cancellationToken);
            }

            // 3. Удаляем из базы данных
            await _videoRepository.DeleteAsync(video.Id);
            await _videoRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Video {VideoId} deleted successfully by user {UserId}", command.VideoId, command.UserId);

            return new DeleteAnyVideoResult(true, "Video deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video {VideoId}", command.VideoId);
            return new DeleteAnyVideoResult(false, $"Error deleting video: {ex.Message}");
        }
    }

    private string ExtractPathFromUrl(string url)
    {
        // Пример: /api/videos/123/video.mp4 -> videos/123/video.mp4
        // Зависит от того, как вы храните пути
        var uri = new Uri(url, UriKind.RelativeOrAbsolute);
        return uri.ToString().TrimStart('/').Replace("api/", "");
    }
}