using ContentService.Application.Interfaces;
using Minio;

namespace ContentService.Infrastructure.Storage;

public class ThumbnailService : IThumbnailService
{
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(ILogger<ThumbnailService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateThumbnailAsync(Stream videoStream, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Thumbnail generated (placeholder) at {OutputPath}", outputPath);
        
        return "/images/default-preview.jpg";
    }
}