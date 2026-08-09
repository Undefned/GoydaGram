namespace ContentService.Application.Interfaces;

public interface IHlsTranscoder
{
    Task<string> ConvertToHlsAsync(string inputPath, string outputDir, string videoId, CancellationToken cancellationToken = default);
}