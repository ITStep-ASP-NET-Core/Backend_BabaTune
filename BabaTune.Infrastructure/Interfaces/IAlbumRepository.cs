using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IAlbumRepository : IGenericRepository<Album, Guid>
	{
		Task<PagedResult<Album>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<Album>> SearchAsync ( string query, int pageNumber, int pageSize );
	}
}
