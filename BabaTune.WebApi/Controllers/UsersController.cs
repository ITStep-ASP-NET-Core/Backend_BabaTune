using BabaTune.Application.DTO.Users;
using BabaTune.Application.Interfaces;
using BabaTune.WebApi.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/users")]
public class UsersController : BaseApiController
{
	private readonly IUserService _userService;

	public UsersController ( IUserService userService )
	{
		_userService = userService;
	}

	[HttpGet("me")]
	public async Task<IActionResult> GetMe ( )
		=> OkOrNotFound(await _userService.GetProfileAsync(CurrentUserId));

	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetById ( Guid id )
		=> OkOrNotFound(await _userService.GetProfileAsync(id));

	[HttpGet("{id:guid}/short")]
	[AllowAnonymous]
	public async Task<IActionResult> GetShort ( Guid id )
		=> OkOrNotFound(await _userService.GetShortAsync(id));

	[HttpGet("search")]
	[AllowAnonymous]
	public async Task<IActionResult> Search ( [FromQuery] string query, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _userService.SearchAsync(query, page, size));
	}

	[HttpPut("me")]
	public async Task<IActionResult> UpdateInfo ( [FromBody] UpdateUserDto dto )
		=> ToActionResult(await _userService.UpdateInfoAsync(CurrentUserId, dto));

	[HttpPut("me/password")]
	public async Task<IActionResult> UpdatePassword ( [FromBody] UpdatePasswordRequest request )
		=> ToActionResult(await _userService.UpdatePasswordAsync(CurrentUserId, request.CurrentPassword, request.NewPassword));

	[HttpPut("me/avatar")]
	[RequestSizeLimit(5 * 1024 * 1024)]
	public async Task<IActionResult> UpdateAvatar ( IFormFile avatarFile )
	{
		if (ValidateImage(avatarFile, required: true) is { } error)
			return error;

		return ToActionResult(await _userService.UpdateAvatarAsync(CurrentUserId, avatarFile));
	}

	[HttpDelete("me/avatar")]
	public async Task<IActionResult> DeleteAvatar ( )
		=> ToActionResult(await _userService.UpdateAvatarAsync(CurrentUserId, null));
}
