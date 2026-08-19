using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Commands.DeleteUser;
using UserService.Application.Commands.DemoteToUser;
using UserService.Application.Commands.PromoteToAdmin;
using UserService.Application.Queries.GetAllUsers;
using UserService.Application.Queries.SearchUsers;



namespace UserService.Controllers.Admin;

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

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        _logger.LogInformation("Admin: Get all users, limit={Limit}, offset={Offset}", limit, offset);
        var query = new GetAllUsersQuery(limit, offset);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("users/{userId:guid}/promote")]
    public async Task<IActionResult> PromoteToAdmin(Guid userId)
    {
        _logger.LogInformation("Admin: Promote user {UserId} to admin", userId);
        var command = new PromoteToAdminCommand(userId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("users/{userId:guid}/demote")]
    public async Task<IActionResult> DemoteToUser(Guid userId)
    {
        _logger.LogInformation("Admin: Demote user {UserId} to user", userId);
        var command = new DemoteToUserCommand(userId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("users/search")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string q,
        [FromQuery] int limit = 30)
    {
        _logger.LogInformation("Admin: Search users by query '{Query}'", q);
        var query = new SearchUsersQuery(q, limit);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        _logger.LogInformation("Admin: Delete user {UserId}", userId);
        var command = new DeleteUserCommand(userId);
        await _mediator.Send(command);
        return NoContent();
    }
}