using System.Diagnostics;
using System.Text;
using ContentService.Application.Interfaces;

namespace ContentService.Infrastructure.Storage;

public class FfmpegHlsTranscodingService : IHlsTranscodingService
{
    private readonly IStorageService _storageService;
    private readonly ILogger<FfmpegHlsTranscodingService> _logger;
    private const int SegmentDurationSeconds = 6;

    public FfmpegHlsTranscodingService(IStorageService storageService, ILogger<FfmpegHlsTranscodingService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<string> TranscodeToHlsAsync(Guid videoId, Guid userId, string sourceObjectPath, CancellationToken cancellationToken = default)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "hls-" + videoId);
        Directory.CreateDirectory(workDir);

        try
        {
            var inputPath = Path.Combine(workDir, "source.mp4");
            const string playlistFileName = "playlist.m3u8";
            var playlistPath = Path.Combine(workDir, playlistFileName);

            // 1. ffmpeg работает с файлами на диске, не с сырым Stream из MinIO-клиента —
            // сначала выкачиваем исходник во временный файл.
            await using (var sourceStream = await _storageService.DownloadFileAsync(sourceObjectPath, cancellationToken))
            await using (var fileStream = File.Create(inputPath))
            {
                await sourceStream.CopyToAsync(fileStream, cancellationToken);
            }

            // 2. Один битрейт, сегменты по 6 сек — "базовый" HLS без адаптивных
            // плейлистов под разные качества (это отдельная, более объёмная фича).
            var ffmpegArgs =
                $"-y -i \"{inputPath}\" " +
                "-codec:v libx264 -preset veryfast -codec:a aac " +
                $"-start_number 0 -hls_time {SegmentDurationSeconds} -hls_list_size 0 " +
                $"-hls_segment_filename \"{Path.Combine(workDir, "segment_%03d.ts")}\" " +
                $"-f hls \"{playlistPath}\"";

            var exitCode = await RunFfmpegAsync(ffmpegArgs, cancellationToken);
            if (exitCode != 0)
                throw new InvalidOperationException($"ffmpeg exited with code {exitCode} while processing video {videoId}");

            if (!File.Exists(playlistPath))
                throw new InvalidOperationException($"ffmpeg finished but playlist was not produced for video {videoId}");

            // 3. Заливаем плейлист и все сегменты обратно в MinIO рядом с оригиналом.
            var destPrefix = $"hls/{userId}/{videoId}";
            string? playlistObjectPath = null;

            foreach (var filePath in Directory.EnumerateFiles(workDir))
            {
                var fileName = Path.GetFileName(filePath);
                var objectPath = $"{destPrefix}/{fileName}";
                var contentType = fileName.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
                    ? "application/vnd.apple.mpegurl"
                    : "video/mp2t";

                await using var uploadStream = File.OpenRead(filePath);
                await _storageService.UploadFileAsync(objectPath, uploadStream, contentType, cancellationToken);

                if (fileName == playlistFileName)
                    playlistObjectPath = objectPath;
            }

            return playlistObjectPath
                ?? throw new InvalidOperationException("Playlist was generated but not found among uploaded files");
        }
        finally
        {
            // Чистим временные файлы независимо от результата — иначе диск контейнера
            // будет копить исходники и сегменты после каждой обработки.
            try { Directory.Delete(workDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private async Task<int> RunFfmpegAsync(string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var stderr = new StringBuilder();
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            _logger.LogError("ffmpeg failed (exit {ExitCode}): {Stderr}", process.ExitCode, stderr.ToString());

        return process.ExitCode;
    }
}
