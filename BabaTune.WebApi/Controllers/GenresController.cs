using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/genres")]
[AllowAnonymous]
public class GenresController : BaseApiController
{
	private readonly IGenreService _genreService;

	public GenresController ( IGenreService genreService )
	{
		_genreService = genreService;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll ( )
		=> Ok(await _genreService.GetAllAsync());

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById ( int id )
		=> OkOrNotFound(await _genreService.GetByIdAsync(id));

	[HttpGet("search")]
	public async Task<IActionResult> Search ( [FromQuery] string query, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _genreService.SearchAsync(query, page, size));
	}
}