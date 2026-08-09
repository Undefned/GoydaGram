using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContentService.Application.Interfaces;

namespace ContentService.Controllers;

[ApiController]
[Route("api/videos/stream")]
public class StreamController : ControllerBase
{
    private readonly IStorageService _storageService;

    public StreamController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet("{**path}")]
    [AllowAnonymous]
    public async Task<IActionResult> Stream(string path)
    {
        try
        {
            var stream = await _storageService.DownloadFileAsync(path);
            var contentType = ResolveContentType(path);

            // enableRangeProcessing: true — без этого браузер/плеер не может перематывать
            // видео (запросы с заголовком Range игнорировались бы, весь файл гнался с начала).
            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch
        {
            return NotFound();
        }
    }

    private static string ResolveContentType(string path)
    {
        if (path.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
            return "application/vnd.apple.mpegurl";
        if (path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
            return "video/mp2t";
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            return "image/jpeg";

        return "video/mp4";
    }
}