using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IPlaylistRepository : IGenericRepository<Playlist, Guid>
	{
		Task<Playlist?> GetWithItemsAsync ( Guid id );
		Task<PagedResult<Playlist>> GetByUserAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<Playlist>> GetCustomByUserAsync ( Guid userId, int pageNumber, int pageSize );
		Task<Playlist?> GetLikedPlaylistAsync ( Guid userId );
		Task<PagedResult<PlaylistItem>> GetItemsPagedAsync ( Guid playlistId, int pageNumber, int pageSize );
		Task AddSongAsync ( Guid playlistId, Guid songId );
		Task RemoveSongAsync ( Guid playlistId, Guid songId );
		Task<bool> ContainsSongAsync ( Guid playlistId, Guid songId );
		Task<HashSet<Guid>> GetLikedSongIdsAsync ( Guid userId, ICollection<Guid> songIds );
		Task<int> GetTotalDurationAsync ( Guid playlistId );
		Task<int> GetSongsCountAsync ( Guid playlistId );
		Task MoveSongAsync ( Guid playlistId, Guid songId, int newPosition );
	}
}
