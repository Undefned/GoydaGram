using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Commands.Subscribe;
using UserService.Application.Commands.Unsubscribe;
using UserService.Application.Queries.GetSubscriptions;
using UserService.Application.Queries.GetUser;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        _logger.LogInformation("Get user {UserId}", id);
        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}/subscriptions")]
    public async Task<IActionResult> GetSubscriptions(Guid id)
    {
        _logger.LogInformation("Get subscriptions for user {UserId}", id);
        var query = new GetSubscriptionsQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("{id:guid}/subscribe")]
    public async Task<IActionResult> Subscribe(Guid id)
    {
        var followerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _logger.LogInformation("User {Follower} subscribes to {Followee}", followerId, id);
        
        var command = new SubscribeCommand(followerId, id);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}/unsubscribe")]
    public async Task<IActionResult> Unsubscribe(Guid id)
    {
        var followerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _logger.LogInformation("User {Follower} unsubscribes from {Followee}", followerId, id);
        
        var command = new UnsubscribeCommand(followerId, id);
        await _mediator.Send(command);
        return NoContent();
    }
}