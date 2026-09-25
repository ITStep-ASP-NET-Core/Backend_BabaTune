using BabaTune.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/categories")]
[AllowAnonymous]
public class CategoriesController : BaseApiController
{
	private readonly ICategoryService _categoryService;

	public CategoriesController ( ICategoryService categoryService )
	{
		_categoryService = categoryService;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll ( )
		=> Ok(await _categoryService.GetAllAsync());

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById ( int id )
		=> OkOrNotFound(await _categoryService.GetByIdAsync(id));

	[HttpGet("search")]
	public async Task<IActionResult> Search ( [FromQuery] string query, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _categoryService.SearchAsync(query, page, size));
	}
}