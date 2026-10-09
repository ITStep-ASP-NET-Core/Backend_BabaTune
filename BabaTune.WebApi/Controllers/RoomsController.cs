using BabaTune.Application.DTO.Rooms;
using BabaTune.Application.Interfaces;
using BabaTune.WebApi.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/rooms")]
public class RoomsController : BaseApiController
{
	private readonly IRoomService _roomService;

	public RoomsController ( IRoomService roomService )
	{
		_roomService = roomService;
	}

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetPublic ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _roomService.GetPublicAsync(page, size, CurrentUserIdOrNull));
	}

	[HttpGet("friends")]
	public async Task<IActionResult> GetFriendsRooms ( [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _roomService.GetFriendsRoomsAsync(CurrentUserId, page, size));
	}

	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetById ( Guid id )
		=> OkOrNotFound(await _roomService.GetByIdAsync(id, CurrentUserIdOrNull));

	[HttpGet("owner/{userId:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetByOwner ( Guid userId )
		=> OkOrNotFound(await _roomService.GetByOwnerAsync(userId, CurrentUserIdOrNull));

	[HttpGet("song/{songId:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetByCurrentSong ( Guid songId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _roomService.GetByCurrentSongAsync(songId, page, size, CurrentUserIdOrNull));
	}

	[HttpPost]
	public async Task<IActionResult> Create ( [FromBody] CreateRoomDto dto )
	{
		var result = await _roomService.CreateAsync(dto, CurrentUserId);

		return result.Success ? StatusCode(StatusCodes.Status201Created, result.Data) : Failure(result.Error);
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete ( Guid id )
		=> ToActionResult(await _roomService.DeleteAsync(id, CurrentUserId, IsAdmin));

	[HttpPost("{id:guid}/join")]
	public async Task<IActionResult> Join ( Guid id )
		=> ToActionResult(await _roomService.JoinAsync(id, CurrentUserId));

	[HttpPost("{id:guid}/leave")]
	public async Task<IActionResult> Leave ( Guid id )
		=> ToActionResult(await _roomService.LeaveAsync(id, CurrentUserId));

	[HttpPut("{id:guid}/owner")]
	public async Task<IActionResult> TransferOwnership ( Guid id, [FromBody] TransferOwnershipRequest request )
		=> ToActionResult(await _roomService.TransferOwnershipAsync(id, CurrentUserId, request.NewOwnerId));

	[HttpPost("{id:guid}/queue/songs/{songId:guid}")]
	public async Task<IActionResult> AddSong ( Guid id, Guid songId )
		=> ToActionResult(await _roomService.AddSongAsync(id, CurrentUserId, songId));

	[HttpPost("{id:guid}/queue/playlists/{playlistId:guid}")]
	public async Task<IActionResult> AddPlaylist ( Guid id, Guid playlistId )
		=> ToActionResult(await _roomService.AddPlaylistAsync(id, CurrentUserId, playlistId));

	[HttpPost("{id:guid}/queue/albums/{albumId:guid}")]
	public async Task<IActionResult> AddAlbum ( Guid id, Guid albumId )
		=> ToActionResult(await _roomService.AddAlbumAsync(id, CurrentUserId, albumId));

	[HttpPut("{id:guid}/playback")]
	public async Task<IActionResult> UpdatePlayback ( Guid id, [FromBody] UpdatePlaybackDto dto )
		=> ToActionResult(await _roomService.UpdatePlaybackAsync(id, CurrentUserId, dto));

	[HttpPost("{id:guid}/skip")]
	public async Task<IActionResult> Skip ( Guid id )
		=> ToActionResult(await _roomService.SkipAsync(id, CurrentUserId));
}
