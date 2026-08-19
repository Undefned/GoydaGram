using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContentService.Application.Commands.BlockVideo;
using ContentService.Application.Commands.DeleteAnyVideo;
using ContentService.Application.Commands.UnblockVideo;
using ContentService.Application.Commands.GetAllVideos;
using ContentService.Application.Queries.GetUserVideos;

namespace ContentService.Controllers.Admin;

[ApiController]
[Route("admin/api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMediator mediator, ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Получить все видео (для модерации)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllVideos(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        _logger.LogInformation("Admin: Get all videos, status={Status}, limit={Limit}, offset={Offset}", 
            status, limit, offset);
        
        var query = new GetAllVideosQuery(limit, offset, status);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Получить видео пользователя (для модерации)
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserVideos(
        Guid userId,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        _logger.LogInformation("Admin: Get videos for user {UserId}", userId);
        
        var query = new GetUserVideosQuery(userId, limit, offset);
        var result = await _mediator.Send(query);
        return Ok(new
        {
            data = result,
            pagination = new { limit, offset }
        });
    }

    /// <summary>
    /// Заблокировать видео
    /// </summary>
    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> BlockVideo(Guid id, [FromBody] BlockVideoRequest request)
    {
        _logger.LogInformation("Admin: Block video {VideoId}, reason: {Reason}", id, request.Reason);
        
        var command = new BlockVideoCommand(id, request.Reason);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Разблокировать видео
    /// </summary>
    [HttpPost("{id:guid}/unblock")]
    public async Task<IActionResult> UnblockVideo(Guid id)
    {
        _logger.LogInformation("Admin: Unblock video {VideoId}", id);
        
        var command = new UnblockVideoCommand(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Удалить видео (любое)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAnyVideo(Guid videoId, Guid userId)
    {
        _logger.LogInformation("Admin: Delete video {VideoId}", userId);
        
        var command = new DeleteAnyVideoCommand(videoId, userId);
        var result = await _mediator.Send(command);
        
        if (!result.Success)
        {
            return result.Message.Contains("not found") 
                ? NotFound(result) 
                : BadRequest(result);
        }
        
        return Ok(result);
    }
}

public class BlockVideoRequest
{
    public string Reason { get; set; } = string.Empty;
}