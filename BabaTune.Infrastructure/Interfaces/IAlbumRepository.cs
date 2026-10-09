using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IAlbumRepository : IGenericRepository<Album, Guid>
	{
		Task<PagedResult<Album>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<Album>> SearchAsync ( string query, int pageNumber, int pageSize );
		Task<int> GetSongsCountAsync ( Guid albumId );
		Task<bool> ContainsSongAsync ( Guid albumId, Guid songId );
		Task<bool> AnySongInAlbumAsync ( ICollection<Guid> songIds );
		Task<int> CountOwnedSongsAsync ( Guid userId, ICollection<Guid> songIds );
		Task AddSongsAsync ( Guid albumId, ICollection<Guid> songIds );
		Task RemoveSongAsync ( Guid albumId, Guid songId );
	}
}
