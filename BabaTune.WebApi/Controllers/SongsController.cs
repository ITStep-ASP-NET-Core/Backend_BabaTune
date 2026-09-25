using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/songs")]
public class SongsController : BaseApiController
{
	private const long MaxAudioSize = 50L * 1024 * 1024;
	private const long MaxImageSize = 5L * 1024 * 1024;

	private readonly ISongService _songService;

	public SongsController ( ISongService songService )
	{
		_songService = songService;
	}

	[HttpGet("filters")]
	[AllowAnonymous]
	public async Task<IActionResult> GetByFilters ( [FromQuery] SongFilterDto filter, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _songService.GetByFiltersAsync(filter, page, size, CurrentUserIdOrNull));
	}

	[HttpGet("top")]
	[AllowAnonymous]
	public async Task<IActionResult> GetTop ( [FromQuery] TopPeriod period = TopPeriod.Week, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		if (!Enum.IsDefined(period))
			return Problem(detail: "Invalid period.", statusCode: StatusCodes.Status400BadRequest);

		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _songService.GetTopAsync(period, page, size, CurrentUserIdOrNull));
	}

	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetById ( Guid id )
		=> OkOrNotFound(await _songService.GetByIdAsync(id, CurrentUserIdOrNull));

	[HttpGet("author/{userId:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetByAuthor ( Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _songService.GetByAuthorAsync(userId, page, size, CurrentUserIdOrNull));
	}

	[HttpPost]
	[Consumes("multipart/form-data")]
	[RequestSizeLimit(MaxAudioSize + MaxImageSize)]
	[RequestFormLimits(MultipartBodyLengthLimit = MaxAudioSize + MaxImageSize)]
	public async Task<IActionResult> Create ( [FromForm] CreateSongDto dto )
	{
		if (ValidateAudio(dto.AudioFile, required: true) is { } audioError)
			return audioError;

		if (ValidateImage(dto.ImageFile) is { } imageError)
			return imageError;

		return ToActionResult(await _songService.CreateAsync(dto, CurrentUserId), created: true);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> UpdateInfo ( Guid id, [FromBody] UpdateSongDto dto )
		=> ToActionResult(await _songService.UpdateInfoAsync(id, CurrentUserId, dto));

	[HttpPut("{id:guid}/image")]
	[RequestSizeLimit(MaxImageSize)]
	public async Task<IActionResult> UpdateImage ( Guid id, IFormFile imageFile )
	{
		if (ValidateImage(imageFile, required: true) is { } error)
			return error;

		return ToActionResult(await _songService.UpdateImageAsync(id, CurrentUserId, imageFile));
	}

	[HttpDelete("{id:guid}/image")]
	public async Task<IActionResult> ResetImage ( Guid id )
		=> ToActionResult(await _songService.UpdateImageAsync(id, CurrentUserId, null));

	[HttpPut("{id:guid}/audio")]
	[RequestSizeLimit(MaxAudioSize)]
	[RequestFormLimits(MultipartBodyLengthLimit = MaxAudioSize)]
	public async Task<IActionResult> UpdateAudio ( Guid id, IFormFile audioFile )
	{
		if (ValidateAudio(audioFile, required: true) is { } error)
			return error;

		return ToActionResult(await _songService.UpdateAudioAsync(id, CurrentUserId, audioFile));
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete ( Guid id )
		=> ToActionResult(await _songService.DeleteAsync(id, CurrentUserId));
}
