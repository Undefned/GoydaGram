namespace ContentService.Application.Interfaces;

public interface IHlsTranscodingService
{
    // Возвращает путь (ключ в MinIO) до сгенерированного master-плейлиста.
    Task<string> TranscodeToHlsAsync(Guid videoId, Guid userId, string sourceObjectPath, CancellationToken cancellationToken = default);
}