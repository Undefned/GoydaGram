using ContentService.Application.Interfaces;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace ContentService.Infrastructure.Storage;

public class MinIOStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName = "videos";
    private readonly ILogger<MinIOStorageService> _logger;

    public MinIOStorageService(IMinioClient minioClient, ILogger<MinIOStorageService> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(string path, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(args, cancellationToken);
            _logger.LogInformation("Uploaded file to MinIO: {Path}", path);
            return GetFileUrl(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to MinIO: {Path}", path);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading file from MinIO: {Path}", path);

            // ✅ Правильный способ для MinIO SDK
            var memoryStream = new MemoryStream();
            
            var args = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithCallbackStream(async (stream) =>
                {
                    await stream.CopyToAsync(memoryStream, cancellationToken);
                });

            await _minioClient.GetObjectAsync(args, cancellationToken);
            
            // ✅ Сбрасываем позицию для чтения
            memoryStream.Position = 0;
            
            _logger.LogInformation("Successfully downloaded file: {Path}, Size: {Size} bytes", 
                path, memoryStream.Length);
            
            return memoryStream;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO error downloading file: {Path}", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from MinIO: {Path}", path);
            throw;
        }
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path);

            await _minioClient.RemoveObjectAsync(args, cancellationToken);
            _logger.LogInformation("Deleted file from MinIO: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from MinIO: {Path}", path);
            throw;
        }
    }

    public string GetFileUrl(string path)
    {
        return $"/api/videos/stream/{path}";
    }
}