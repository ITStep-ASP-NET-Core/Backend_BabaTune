using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/friends")]
public class FriendshipsController : BaseApiController
{
	private readonly IFriendshipService _friendshipService;

	public FriendshipsController ( IFriendshipService friendshipService )
	{
		_friendshipService = friendshipService;
	}

	[HttpGet]
	public async Task<IActionResult> GetFriends ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _friendshipService.GetFriendsAsync(CurrentUserId, page, size));
	}

	[HttpGet("requests")]
	public async Task<IActionResult> GetIncoming ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _friendshipService.GetIncomingAsync(CurrentUserId, page, size));
	}

	[HttpGet("blocked")]
	public async Task<IActionResult> GetBlocked ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _friendshipService.GetBlockedAsync(CurrentUserId, page, size));
	}

	[HttpPost("requests/{userId:guid}")]
	public async Task<IActionResult> SendRequest ( Guid userId )
		=> ToActionResult(await _friendshipService.SendRequestAsync(CurrentUserId, userId), created: true);

	[HttpPost("{id:guid}/accept")]
	public async Task<IActionResult> Accept ( Guid id )
		=> ToActionResult(await _friendshipService.AcceptAsync(id, CurrentUserId));

	[HttpPost("block/{userId:guid}")]
	public async Task<IActionResult> Block ( Guid userId )
		=> ToActionResult(await _friendshipService.BlockAsync(CurrentUserId, userId));

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Remove ( Guid id )
		=> ToActionResult(await _friendshipService.RemoveAsync(id, CurrentUserId));
}
