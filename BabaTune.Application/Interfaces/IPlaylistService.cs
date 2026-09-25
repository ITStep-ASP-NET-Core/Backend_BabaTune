using BabaTune.Application.Common;
using BabaTune.Application.DTO.Playlists;
using BabaTune.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Interfaces
{
	public interface IPlaylistService
	{
		Task<PlaylistDto?> GetByIdAsync ( Guid playlistId );
		Task<PlaylistDto?> GetLikedAsync ( Guid userId );
		Task<PagedResult<PlaylistSidebarDto>> GetByUserAsync ( Guid userId, int pageNumber, int pageSize );
		Task<Result> CreateAsync ( CreatePlaylistDto playlistDto, Guid userId );
		Task<Result> UpdateInfoAsync ( Guid playlistId, Guid userId, UpdatePlaylistDto playlistDto );
		Task<Result> UpdateImageAsync ( Guid playlistId, Guid userId, IFormFile? imageFile );
		Task<Result> DeleteAsync ( Guid playlistId, Guid userId );
		Task<Result> AddSongAsync ( Guid playlistId, Guid userId, Guid songId );
		Task<Result> RemoveSongAsync ( Guid playlistId, Guid userId, Guid songId );
		Task<Result> MoveSongAsync ( Guid playlistId, Guid userId, Guid songId, int newPosition );
	}
}
