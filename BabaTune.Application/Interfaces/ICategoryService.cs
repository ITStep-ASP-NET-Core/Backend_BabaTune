using BabaTune.Application.Common;
using BabaTune.Application.DTO.Category;
using BabaTune.Application.DTO.Common;
using BabaTune.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Interfaces
{
	public interface ICategoryService
	{
		Task<List<CategoryDto>> GetAllAsync ( );
		Task<CategoryDto?> GetByIdAsync ( int id );
		Task<PagedResult<CategoryDto>> SearchAsync ( string query, int pageNumber, int pageSize );
		Task<Result> CreateAsync ( CreateCategoryDto dto );
		Task<Result> UpdateInfoAsync ( int id, UpdateCategoryDto dto );
		Task<Result> UpdateImageAsync ( int id, IFormFile? imageFile );
		Task<Result> DeleteAsync ( int id );
	}
}
