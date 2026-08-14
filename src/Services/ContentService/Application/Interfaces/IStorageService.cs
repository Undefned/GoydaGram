namespace ContentService.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(string path, Stream stream, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    string GetFileUrl(string path);
}