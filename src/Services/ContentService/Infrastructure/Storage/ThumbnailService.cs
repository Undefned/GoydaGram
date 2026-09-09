using System.Diagnostics;
using ContentService.Application.Interfaces;

namespace ContentService.Infrastructure.Storage;

public class ThumbnailService : IThumbnailService
{
    private readonly ILogger<ThumbnailService> _logger;
    private readonly IStorageService _storageService;

    public ThumbnailService(ILogger<ThumbnailService> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    public async Task<string> GenerateThumbnailAsync(Stream videoStream, string outputPath, CancellationToken cancellationToken = default)
    {
        var tempVideoFile = Path.GetTempFileName();
        var tempThumbFile = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
        
        try
        {
            // 1. Сохраняем видео во временный файл
            videoStream.Position = 0;
            await using (var fileStream = File.Create(tempVideoFile))
            {
                await videoStream.CopyToAsync(fileStream, cancellationToken);
            }

            // 2. Извлекаем кадр из середины видео (или первый кадр)
            // Сначала получаем длительность видео
            var duration = await GetVideoDurationAsync(tempVideoFile, cancellationToken);
            
            // Берем кадр на 10% от начала видео (или 1 секунду, если видео короткое)
            var seekTime = Math.Min(1, duration * 0.1);
            
            // 3. Генерируем превью через FFmpeg
            var ffmpegArgs = $"-y -i \"{tempVideoFile}\" -ss {seekTime:F2} -vframes 1 -vf \"scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2\" -q:v 2 \"{tempThumbFile}\"";
            
            var exitCode = await RunFfmpegAsync(ffmpegArgs, cancellationToken);
            
            if (exitCode != 0 || !File.Exists(tempThumbFile))
            {
                _logger.LogWarning("Failed to generate thumbnail for video, using default placeholder");
                return "/images/default-preview.jpg";
            }

            // 4. Загружаем превью в MinIO
            await using var thumbStream = File.OpenRead(tempThumbFile);
            var contentType = "image/jpeg";
            
            var url = await _storageService.UploadFileAsync(outputPath, thumbStream, contentType, cancellationToken);
            
            _logger.LogInformation("Thumbnail generated and uploaded to {OutputPath}", outputPath);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail, using default placeholder");
            return "/images/default-preview.jpg";
        }
        finally
        {
            // Чистим временные файлы
            try { File.Delete(tempVideoFile); } catch { }
            try { File.Delete(tempThumbFile); } catch { }
        }
    }

    private async Task<double> GetVideoDurationAsync(string videoPath, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && double.TryParse(output.Trim(), out var seconds))
            {
                return seconds;
            }

            return 10; // Если не удалось получить длительность, берем 10 секунд как запас
        }
        catch
        {
            return 10;
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
        var stderr = new System.Text.StringBuilder();
        
        process.ErrorDataReceived += (_, e) => 
        { 
            if (e.Data != null) 
                stderr.AppendLine(e.Data); 
        };

        process.Start();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFmpeg failed (exit {ExitCode}): {Stderr}", process.ExitCode, stderr.ToString());
        }

        return process.ExitCode;
    }
}