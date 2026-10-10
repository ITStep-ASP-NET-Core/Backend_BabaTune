using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static BabaTune.Application.Common.Constants;

namespace BabaTune.WebApi.Controllers;

[Route("api/songs")]
public class SongsController : BaseApiController
{
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
	[RequestSizeLimit(Limits.MaxAudioSize + Limits.MaxImageSize)]
	[RequestFormLimits(MultipartBodyLengthLimit = Limits.MaxAudioSize + Limits.MaxImageSize)]
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
	[RequestSizeLimit(Limits.MaxImageSize)]
	public async Task<IActionResult> UpdateImage ( Guid id, [ImageFile] IFormFile imageFile )
	{
		if (ValidateImage(imageFile, required: true) is { } error)
			return error;

		return ToActionResult(await _songService.UpdateImageAsync(id, CurrentUserId, imageFile));
	}

	[HttpDelete("{id:guid}/image")]
	public async Task<IActionResult> ResetImage ( Guid id )
		=> ToActionResult(await _songService.UpdateImageAsync(id, CurrentUserId, null));

	[HttpPut("{id:guid}/audio")]
	[RequestSizeLimit(Limits.MaxAudioSize)]
	[RequestFormLimits(MultipartBodyLengthLimit = Limits.MaxAudioSize)]
	public async Task<IActionResult> UpdateAudio ( Guid id, [AudioFile] IFormFile audioFile )
	{
		if (ValidateAudio(audioFile, required: true) is { } error)
			return error;

		return ToActionResult(await _songService.UpdateAudioAsync(id, CurrentUserId, audioFile));
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete ( Guid id )
		=> ToActionResult(await _songService.DeleteAsync(id, CurrentUserId, IsAdmin));
}
