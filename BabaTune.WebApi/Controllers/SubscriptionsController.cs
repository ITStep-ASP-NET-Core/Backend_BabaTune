using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/users/{userId:guid}")]
public class SubscriptionsController : BaseApiController
{
	private readonly ISubscribeService _subscribeService;

	public SubscriptionsController ( ISubscribeService subscribeService )
	{
		_subscribeService = subscribeService;
	}

	[HttpGet("subscriptions")]
	[AllowAnonymous]
	public async Task<IActionResult> GetSubscriptions ( Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _subscribeService.GetSubscriptionsAsync(userId, page, size));
	}

	[HttpGet("subscribers")]
	[AllowAnonymous]
	public async Task<IActionResult> GetSubscribers ( Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _subscribeService.GetSubscribersAsync(userId, page, size));
	}

	[HttpPost("subscribe")]
	public async Task<IActionResult> Subscribe ( Guid userId )
		=> ToActionResult(await _subscribeService.SubscribeAsync(CurrentUserId, userId));

	[HttpDelete("subscribe")]
	public async Task<IActionResult> Unsubscribe ( Guid userId )
		=> ToActionResult(await _subscribeService.UnsubscribeAsync(CurrentUserId, userId));
}
