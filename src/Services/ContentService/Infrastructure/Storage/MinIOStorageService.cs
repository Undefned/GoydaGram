using ContentService.Application.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace ContentService.Infrastructure.Storage;

public class MinIOStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName = "videos";

    public MinIOStorageService(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task<string> UploadFileAsync(string path, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        var args = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(path)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(args, cancellationToken);
        return GetFileUrl(path);
    }

    public async Task<Stream> DownloadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(path)
            .WithCallbackStream(async stream =>
            {
                await stream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
            });

        await _minioClient.GetObjectAsync(args, cancellationToken);
        return memoryStream;
    }

    public async Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(path);

        await _minioClient.RemoveObjectAsync(args, cancellationToken);
    }

    public string GetFileUrl(string path)
    {
        return $"/api/videos/stream/{path}";
    }
}