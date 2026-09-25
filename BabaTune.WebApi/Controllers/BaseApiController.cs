using System.Security.Claims;
using BabaTune.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
	private const int MaxPageSize = 100;

	protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

	protected Guid? CurrentUserIdOrNull =>
		Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

	protected static (int Page, int Size) Paging ( int pageNumber, int pageSize )
		=> (Math.Max(pageNumber, 1), Math.Clamp(pageSize, 1, MaxPageSize));

	protected IActionResult OkOrNotFound<T> ( T? value ) where T : class
		=> value is null ? NotFound() : Ok(value);

	protected IActionResult ToActionResult ( Result result, bool created = false )
	{
		if (!result.Success)
			return Failure(result.Error);

		return created ? StatusCode(StatusCodes.Status201Created) : NoContent();
	}

	protected IActionResult Failure ( string? error )
	{
		var message = error ?? "Request failed.";

		var status = message switch
		{
			_ when message.Contains("not found", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
			_ when message.Contains("not the ", StringComparison.OrdinalIgnoreCase)
				|| message.Contains("does not belong", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status403Forbidden,
			_ when message.Contains("already", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
			_ => StatusCodes.Status400BadRequest
		};

		return Problem(detail: message, statusCode: status);
	}

	protected IActionResult? ValidateImage ( IFormFile? file, bool required = false )
		=> ValidateFile(file, "image/", required);

	protected IActionResult? ValidateAudio ( IFormFile? file, bool required = false )
		=> ValidateFile(file, "audio/", required);

	private IActionResult? ValidateFile ( IFormFile? file, string contentTypePrefix, bool required )
	{
		if (file is null)
			return required ? Problem(detail: "File is required.", statusCode: StatusCodes.Status400BadRequest) : null;

		if (file.Length == 0 || !file.ContentType.StartsWith(contentTypePrefix, StringComparison.OrdinalIgnoreCase))
			return Problem(detail: $"File must be of type {contentTypePrefix}*.", statusCode: StatusCodes.Status400BadRequest);

		return null;
	}
}
