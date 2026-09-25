using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface ISongRepository : IGenericRepository<Song, Guid>
	{
		Task<ICollection<Song>> GetByIdsAsync ( ICollection<Guid> ids );
		Task<Song?> GetWithAllAsync ( Guid id );
		Task<PagedResult<Song>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize );
		Task<int> GetCountByAuthorAsync ( Guid userId );
		Task<PagedResult<Song>> GetByAlbumAsync ( Guid albumId, int pageNumber, int pageSize );
		Task<PagedResult<Song>> GetByFiltersAsync ( string? searchQuery, Guid? authorId, Guid? albumId, ICollection<int>? categoryIds, ICollection<int>? genreIds, int pageNumber, int pageSize );
	}
}
