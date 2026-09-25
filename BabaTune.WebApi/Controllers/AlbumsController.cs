using BabaTune.Application.DTO.Albums;
using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/albums")]
public class AlbumsController : BaseApiController
{
	private const long MaxImageSize = 5L * 1024 * 1024;

	private readonly IAlbumService _albumService;
	private readonly ISongService _songService;

	public AlbumsController ( IAlbumService albumService, ISongService songService )
	{
		_albumService = albumService;
		_songService = songService;
	}

	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetById ( Guid id )
		=> OkOrNotFound(await _albumService.GetByIdAsync(id));

	[HttpGet("{id:guid}/songs")]
	[AllowAnonymous]
	public async Task<IActionResult> GetSongs ( Guid id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _songService.GetByAlbumAsync(id, page, size, CurrentUserIdOrNull));
	}

	[HttpGet("author/{userId:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetByAuthor ( Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _albumService.GetByAuthorAsync(userId, page, size));
	}

	[HttpGet("search")]
	[AllowAnonymous]
	public async Task<IActionResult> Search ( [FromQuery] string query, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _albumService.SearchAsync(query, page, size));
	}

	[HttpPost]
	[Consumes("multipart/form-data")]
	[RequestSizeLimit(MaxImageSize)]
	public async Task<IActionResult> Create ( [FromForm] CreateAlbumDto dto )
	{
		if (ValidateImage(dto.ImageFile) is { } error)
			return error;

		return ToActionResult(await _albumService.CreateAsync(dto, CurrentUserId), created: true);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> UpdateInfo ( Guid id, [FromBody] UpdateAlbumDto dto )
		=> ToActionResult(await _albumService.UpdateInfoAsync(id, CurrentUserId, dto));

	[HttpPut("{id:guid}/image")]
	[RequestSizeLimit(MaxImageSize)]
	public async Task<IActionResult> UpdateImage ( Guid id, IFormFile imageFile )
	{
		if (ValidateImage(imageFile, required: true) is { } error)
			return error;

		return ToActionResult(await _albumService.UpdateImageAsync(id, CurrentUserId, imageFile));
	}

	[HttpDelete("{id:guid}/image")]
	public async Task<IActionResult> ResetImage ( Guid id )
		=> ToActionResult(await _albumService.UpdateImageAsync(id, CurrentUserId, null));

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete ( Guid id )
		=> ToActionResult(await _albumService.DeleteAsync(id, CurrentUserId));
}
