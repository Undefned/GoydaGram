using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContentService.Application.Interfaces;

namespace ContentService.Controllers;

[ApiController]
[Route("api/videos/stream")]
public class StreamController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly ILogger<StreamController> _logger;

    public StreamController(IStorageService storageService, ILogger<StreamController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    [HttpGet("{**path}")]
    [AllowAnonymous]
    public async Task<IActionResult> Stream(string path)
    {
        try
        {
            _logger.LogInformation("Streaming file: {Path}", path);
            
            var stream = await _storageService.DownloadFileAsync(path);
            
            if (stream == null || stream.Length == 0)
            {
                _logger.LogWarning("File not found or empty: {Path}", path);
                return NotFound();
            }

            var contentType = ResolveContentType(path);
            
            // ✅ Устанавливаем заголовки для HLS
            Response.Headers.Add("Accept-Ranges", "bytes");
            Response.Headers.Add("Cache-Control", "public, max-age=86400");
            
            // ✅ Возвращаем файл с поддержкой Range
            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream file: {Path}", path);
            return NotFound();
        }
    }

    [HttpGet("preview/{**path}")]
    [AllowAnonymous]
    public async Task<IActionResult> Preview(string path)
    {
        try
        {
            _logger.LogInformation("Streaming preview: {Path}", path);
            
            var stream = await _storageService.DownloadFileAsync($"previews/{path}");
            
            if (stream == null || stream.Length == 0)
            {
                return NotFound();
            }
            
            return File(stream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream preview: {Path}", path);
            return NotFound();
        }
    }

    private static string ResolveContentType(string path)
    {
        if (path.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
            return "application/vnd.apple.mpegurl";
        if (path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
            return "video/mp2t";
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
            path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            return "image/jpeg";
        if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            return "video/mp4";

        return "application/octet-stream";
    }
}