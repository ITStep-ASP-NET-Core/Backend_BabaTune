using BabaTune.Application.DTO.Genre;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static BabaTune.Application.Common.Constants;

namespace BabaTune.WebApi.Controllers;

[Route("api/genres")]
public class GenresController : BaseApiController
{
	private readonly IGenreService _genreService;

	public GenresController ( IGenreService genreService )
	{
		_genreService = genreService;
	}

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetAll ( )
		=> Ok(await _genreService.GetAllAsync());

	[HttpGet("{id:int}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetById ( int id )
		=> OkOrNotFound(await _genreService.GetByIdAsync(id));

	[HttpGet("search")]
	[AllowAnonymous]
	public async Task<IActionResult> Search ( [FromQuery] string query, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _genreService.SearchAsync(query, page, size));
	}

	[HttpPost]
	[Authorize(Policy = "AdminOnly")]
	[Consumes("multipart/form-data")]
	[RequestSizeLimit(Limits.MaxImageSize)]
	public async Task<IActionResult> Create ( [FromForm] CreateGenreDto dto )
		=> ToActionResult(await _genreService.CreateAsync(dto), created: true);

	[HttpPut("{id:int}")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> UpdateInfo ( int id, [FromBody] UpdateGenreDto dto )
		=> ToActionResult(await _genreService.UpdateInfoAsync(id, dto));

	[HttpPut("{id:int}/image")]
	[Authorize(Policy = "AdminOnly")]
	[RequestSizeLimit(Limits.MaxImageSize)]
	public async Task<IActionResult> UpdateImage ( int id, [ImageFile] IFormFile imageFile )
		=> ToActionResult(await _genreService.UpdateImageAsync(id, imageFile));

	[HttpDelete("{id:int}/image")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> ResetImage ( int id )
		=> ToActionResult(await _genreService.UpdateImageAsync(id, null));

	[HttpDelete("{id:int}")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> Delete ( int id )
		=> ToActionResult(await _genreService.DeleteAsync(id));
}