using BabaTune.Application.DTO.Playlists;
using BabaTune.Application.Interfaces;
using BabaTune.WebApi.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/playlists")]
public class PlaylistsController : BaseApiController
{
	private const long MaxImageSize = 5L * 1024 * 1024;

	private readonly IPlaylistService _playlistService;
	private readonly ISongService _songService;

	public PlaylistsController ( IPlaylistService playlistService, ISongService songService )
	{
		_playlistService = playlistService;
		_songService = songService;
	}

	[HttpGet("liked")]
	public async Task<IActionResult> GetLiked ( )
		=> OkOrNotFound(await _playlistService.GetLikedAsync(CurrentUserId));

	[HttpGet("my")]
	public async Task<IActionResult> GetMy ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _playlistService.GetByUserAsync(CurrentUserId, page, size));
	}

	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetById ( Guid id )
		=> OkOrNotFound(await _playlistService.GetByIdAsync(id));

	[HttpGet("{id:guid}/songs")]
	[AllowAnonymous]
	public async Task<IActionResult> GetSongs ( Guid id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _songService.GetByPlaylistAsync(id, page, size, CurrentUserIdOrNull));
	}

	[HttpPost]
	[Consumes("multipart/form-data")]
	[RequestSizeLimit(MaxImageSize)]
	public async Task<IActionResult> Create ( [FromForm] CreatePlaylistDto dto )
	{
		if (ValidateImage(dto.ImageFile) is { } error)
			return error;

		return ToActionResult(await _playlistService.CreateAsync(dto, CurrentUserId), created: true);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> UpdateInfo ( Guid id, [FromBody] UpdatePlaylistDto dto )
		=> ToActionResult(await _playlistService.UpdateInfoAsync(id, CurrentUserId, dto));

	[HttpPut("{id:guid}/image")]
	[RequestSizeLimit(MaxImageSize)]
	public async Task<IActionResult> UpdateImage ( Guid id, IFormFile imageFile )
	{
		if (ValidateImage(imageFile, required: true) is { } error)
			return error;

		return ToActionResult(await _playlistService.UpdateImageAsync(id, CurrentUserId, imageFile));
	}

	[HttpDelete("{id:guid}/image")]
	public async Task<IActionResult> ResetImage ( Guid id )
		=> ToActionResult(await _playlistService.UpdateImageAsync(id, CurrentUserId, null));

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete ( Guid id )
		=> ToActionResult(await _playlistService.DeleteAsync(id, CurrentUserId));

	[HttpPost("{id:guid}/songs/{songId:guid}")]
	public async Task<IActionResult> AddSong ( Guid id, Guid songId )
		=> ToActionResult(await _playlistService.AddSongAsync(id, CurrentUserId, songId));

	[HttpDelete("{id:guid}/songs/{songId:guid}")]
	public async Task<IActionResult> RemoveSong ( Guid id, Guid songId )
		=> ToActionResult(await _playlistService.RemoveSongAsync(id, CurrentUserId, songId));

	[HttpPut("{id:guid}/songs/{songId:guid}/position")]
	public async Task<IActionResult> MoveSong ( Guid id, Guid songId, [FromBody] MoveSongRequest request )
		=> ToActionResult(await _playlistService.MoveSongAsync(id, CurrentUserId, songId, request.NewPosition));
}
