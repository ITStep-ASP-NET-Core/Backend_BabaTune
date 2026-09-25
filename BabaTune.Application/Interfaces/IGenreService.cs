using BabaTune.Application.Common;
using BabaTune.Application.DTO.Genre;
using BabaTune.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Interfaces
{
	public interface IGenreService
	{
		Task<List<GenreDto>> GetAllAsync ( );
		Task<GenreDto?> GetByIdAsync ( int id );
		Task<PagedResult<GenreDto>> SearchAsync ( string query, int pageNumber, int pageSize );
		Task<Result> CreateAsync ( CreateGenreDto dto );
		Task<Result> UpdateInfoAsync ( int id, UpdateGenreDto dto );
		Task<Result> UpdateImageAsync ( int id, IFormFile? imageFile );
		Task<Result> DeleteAsync ( int id );
	}
}
