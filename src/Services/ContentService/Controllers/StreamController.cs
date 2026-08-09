using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContentService.Application.Interfaces;

namespace ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
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
            var stream = await _storageService.DownloadFileAsync($"videos/{path}");
            return File(stream, "video/mp4");
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("preview/{**path}")]
    [AllowAnonymous]
    public async Task<IActionResult> Preview(string path)
    {
        try
        {
            var stream = await _storageService.DownloadFileAsync($"previews/{path}");
            return File(stream, "image/jpeg");
        }
        catch
        {
            return NotFound();
        }
    }
}