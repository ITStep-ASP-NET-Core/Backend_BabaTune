using BabaTune.Application.Common;
using BabaTune.Application.DTO.Albums;
using BabaTune.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Interfaces
{
	public interface IAlbumService
	{
		Task<AlbumDto?> GetByIdAsync ( Guid albumId );
		Task<PagedResult<AlbumCardDto>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<AlbumCardDto>> SearchAsync ( string query, int pageNumber, int pageSize );
		Task<Result> CreateAsync ( CreateAlbumDto albumDto, Guid userId );
		Task<Result> UpdateInfoAsync ( Guid albumId, Guid userId, UpdateAlbumDto albumDto );
		Task<Result> UpdateImageAsync ( Guid albumId, Guid userId, IFormFile? imageFile );
		Task<Result> DeleteAsync ( Guid albumId, Guid userId );
	}
}
