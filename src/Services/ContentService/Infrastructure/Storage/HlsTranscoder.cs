using System.Diagnostics;
using ContentService.Application.Interfaces;

namespace ContentService.Infrastructure.Storage;

public class HlsTranscoder : IHlsTranscoder
{
    private readonly ILogger<HlsTranscoder> _logger;
    private readonly IStorageService _storageService;
    private readonly string _bucketName = "videos";

    public HlsTranscoder(ILogger<HlsTranscoder> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    public async Task<string> ConvertToHlsAsync(string inputPath, string outputDir, string videoId, CancellationToken cancellationToken = default)
    {
        // Создаём временную директорию
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Скачиваем видео из MinIO
            _logger.LogInformation("Downloading video from MinIO: {InputPath}", inputPath);
            using var videoStream = await _storageService.DownloadFileAsync(inputPath, cancellationToken);
            
            var inputFile = Path.Combine(tempDir, "input.mp4");
            using (var fileStream = File.Create(inputFile))
            {
                await videoStream.CopyToAsync(fileStream, cancellationToken);
            }

            // FFmpeg команда для HLS
            // Создаём 3 варианта качества: 1080p, 720p, 480p
            var hlsDir = Path.Combine(tempDir, "hls");
            Directory.CreateDirectory(hlsDir);

            var ffmpegArgs = $@"
                -i ""{inputFile}""
                -filter_complex ""[0:v]split=3[v1][v2][v3]""
                -map [v1] -map 0:a? -c:v h264 -b:v 2500k -maxrate 3000k -bufsize 5000k -vf scale=-2:1080 -c:a aac -b:a 128k -hls_time 6 -hls_playlist_type vod -hls_segment_filename ""{hlsDir}/1080p_%03d.ts"" -f hls ""{hlsDir}/1080p.m3u8""
                -map [v2] -map 0:a? -c:v h264 -b:v 1500k -maxrate 2000k -bufsize 3000k -vf scale=-2:720 -c:a aac -b:a 96k -hls_time 6 -hls_playlist_type vod -hls_segment_filename ""{hlsDir}/720p_%03d.ts"" -f hls ""{hlsDir}/720p.m3u8""
                -map [v3] -map 0:a? -c:v h264 -b:v 800k -maxrate 1000k -bufsize 1500k -vf scale=-2:480 -c:a aac -b:a 64k -hls_time 6 -hls_playlist_type vod -hls_segment_filename ""{hlsDir}/480p_%03d.ts"" -f hls ""{hlsDir}/480p.m3u8""
            ";

            _logger.LogInformation("Running FFmpeg for HLS conversion...");
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"FFmpeg failed: {error}");
            }

            // Создаём мастер-плейлист (ссылается на все качества)
            var masterPlaylist = $@"
#EXTM3U
#EXT-X-VERSION:3
#EXT-X-STREAM-INF:BANDWIDTH=2800000,RESOLUTION=1920x1080,CODECS=""avc1.42c01e,mp4a.40.2""
{hlsDir}/1080p.m3u8
#EXT-X-STREAM-INF:BANDWIDTH=1700000,RESOLUTION=1280x720,CODECS=""avc1.42c01e,mp4a.40.2""
{hlsDir}/720p.m3u8
#EXT-X-STREAM-INF:BANDWIDTH=900000,RESOLUTION=854x480,CODECS=""avc1.42c01e,mp4a.40.2""
{hlsDir}/480p.m3u8
";

            var masterPath = Path.Combine(hlsDir, "master.m3u8");
            await File.WriteAllTextAsync(masterPath, masterPlaylist, cancellationToken);

            // Загружаем все файлы в MinIO
            _logger.LogInformation("Uploading HLS files to MinIO...");
            
            var basePath = $"hls/{videoId}";
            var files = Directory.GetFiles(hlsDir, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(hlsDir, file);
                var objectPath = $"{basePath}/{relativePath.Replace('\\', '/')}";
                
                using var fs = File.OpenRead(file);
                var contentType = Path.GetExtension(file) switch
                {
                    ".m3u8" => "application/vnd.apple.mpegurl",
                    ".ts" => "video/MP2T",
                    _ => "application/octet-stream"
                };

                await _storageService.UploadFileAsync(objectPath, fs, contentType, cancellationToken);
            }

            // Возвращаем URL мастер-плейлиста
            return $"/api/stream/hls/{videoId}/master.m3u8";
        }
        finally
        {
            // Чистим временные файлы
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch { }
        }
    }
}