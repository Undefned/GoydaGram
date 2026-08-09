using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ContentService.Application.Queries.GetVideo;
using ContentService.Application.Queries.GetBatch;
using ContentService.Application.Queries.GetTrending;
using ContentService.Application.Commands.UploadVideo;
using ContentService.Application.Commands.DeleteVideo;
using ContentService.Application.Queries.GetUserVideos;

namespace ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VideosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VideosController> _logger;

    public VideosController(IMediator mediator, ILogger<VideosController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetVideo(Guid id)
    {
        _logger.LogInformation("Get video {VideoId}", id);
        var query = new GetVideoQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> GetBatch([FromBody] GetBatchQuery query)
    {
        _logger.LogInformation("Get batch of {Count} videos", query.VideoIds.Count);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int limit = 30)
    {
        _logger.LogInformation("Get trending videos, limit: {Limit}", limit);
        var query = new GetTrendingQuery(limit);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _logger.LogInformation("Upload video by user {UserId}: {FileName}", userId, request.File.FileName);

        using var stream = request.File.OpenReadStream();
        var command = new UploadVideoCommand(
            userId,
            request.Title,
            request.Description,
            request.Tags?.Split(',').Select(t => t.Trim()).ToList() ?? new(),
            stream,
            request.File.FileName,
            request.File.Length
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Получаем ID пользователя из токена/контекста
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new DeleteVideoCommand(id, Guid.Parse(userId));
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return result.Message.Contains("not found") 
                ? NotFound(result) 
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserVideos([FromQuery] int limit = 30, [FromQuery] int offset = 0)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        _logger.LogInformation("Getting videos for user {UserId}, limit: {Limit}, offset: {Offset}", 
            userId, limit, offset);
        
        var query = new GetUserVideosQuery(userId, limit, offset);
        var result = await _mediator.Send(query);
        
        return Ok(new
        {
            data = result,
            pagination = new
            {
                limit,
                offset,
                total = result.Count // Можно добавить total count если нужно
            }
        });
    }
    
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserVideosById(Guid userId, [FromQuery] int limit = 30, [FromQuery] int offset = 0)
    {
        _logger.LogInformation("Getting videos for user {UserId}", userId);
        
        var query = new GetUserVideosQuery(userId, limit, offset);
        var result = await _mediator.Send(query);
        
        return Ok(new
        {
            data = result,
            pagination = new { limit, offset }
        });
    }
}
