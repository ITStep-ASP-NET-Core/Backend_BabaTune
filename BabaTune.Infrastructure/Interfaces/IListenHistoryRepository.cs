using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IListenHistoryRepository : IGenericRepository<ListenHistory, Guid>
	{
		Task<PagedResult<ListenHistory>> GetByUserAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<Guid>> GetTopSongIdsAsync ( DateTime from, int pageNumber, int pageSize );
		Task<ListenHistory?> GetEntryAsync ( Guid userId, Guid songId );
	}
}
