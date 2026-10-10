using BabaTune.Application.DTO.Category;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabaTune.WebApi.Controllers;

[Route("api/categories")]
public class CategoriesController : BaseApiController
{
	private const long MaxImageSize = 5L * 1024 * 1024;

	private readonly ICategoryService _categoryService;

	public CategoriesController ( ICategoryService categoryService )
	{
		_categoryService = categoryService;
	}

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetAll ( )
		=> Ok(await _categoryService.GetAllAsync());

	[HttpGet("{id:int}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetById ( int id )
		=> OkOrNotFound(await _categoryService.GetByIdAsync(id));

	[HttpGet("search")]
	[AllowAnonymous]
	public async Task<IActionResult> Search ( [FromQuery] string query, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20 )
	{
		var (page, size) = Paging(pageNumber, pageSize);
		return Ok(await _categoryService.SearchAsync(query, page, size));
	}

	[HttpPost]
	[Authorize(Policy = "AdminOnly")]
	[Consumes("multipart/form-data")]
	[RequestSizeLimit(MaxImageSize)]
	public async Task<IActionResult> Create ( [FromForm] CreateCategoryDto dto )
		=> ToActionResult(await _categoryService.CreateAsync(dto), created: true);

	[HttpPut("{id:int}")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> UpdateInfo ( int id, [FromBody] UpdateCategoryDto dto )
		=> ToActionResult(await _categoryService.UpdateInfoAsync(id, dto));

	[HttpPut("{id:int}/image")]
	[Authorize(Policy = "AdminOnly")]
	[RequestSizeLimit(MaxImageSize)]
	public async Task<IActionResult> UpdateImage ( int id, [ImageFile] IFormFile imageFile )
		=> ToActionResult(await _categoryService.UpdateImageAsync(id, imageFile));

	[HttpDelete("{id:int}/image")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> ResetImage ( int id )
		=> ToActionResult(await _categoryService.UpdateImageAsync(id, null));

	[HttpDelete("{id:int}")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> Delete ( int id )
		=> ToActionResult(await _categoryService.DeleteAsync(id));
}