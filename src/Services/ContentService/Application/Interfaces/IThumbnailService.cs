namespace ContentService.Application.Interfaces;

public interface IThumbnailService
{
    Task<string> GenerateThumbnailAsync(Stream videoStream, string outputPath, CancellationToken cancellationToken = default);
}