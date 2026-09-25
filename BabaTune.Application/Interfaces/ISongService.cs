using BabaTune.Application.Common;
using BabaTune.Application.DTO.Songs;
using BabaTune.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Interfaces
{
	public interface ISongService
	{
		Task<PagedResult<SongDto>> GetTopAsync ( TopPeriod period, int pageNumber, int pageSize, Guid? currentUserId );
		Task<SongDto?> GetByIdAsync ( Guid songId, Guid? currentUserId );
		Task<PagedResult<SongDto>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize, Guid? currentUserId );
		Task<PagedResult<SongDto>> GetByFiltersAsync ( SongFilterDto filter, int pageNumber, int pageSize, Guid? currentUserId );
		Task<PagedResult<SongDto>> GetByPlaylistAsync ( Guid playlistId, int pageNumber, int pageSize, Guid? currentUserId );
		Task<PagedResult<SongDto>> GetByAlbumAsync ( Guid albumId, int pageNumber, int pageSize, Guid? currentUserId );
		Task<Result> CreateAsync ( CreateSongDto songDto, Guid userId );
		Task<Result> UpdateInfoAsync ( Guid songId, Guid userId, UpdateSongDto songDto );
		Task<Result> UpdateImageAsync ( Guid songId, Guid userId, IFormFile? imageFile );
		Task<Result> UpdateAudioAsync ( Guid songId, Guid userId, IFormFile audioFile );
		Task<Result> DeleteAsync ( Guid songId, Guid userId );
	}
}
