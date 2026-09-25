using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface ISubscribeRepository : IGenericRepository<Subscribe, Guid>
	{
		Task<Subscribe?> GetAsync ( Guid subscriberId, Guid subscribedToId );
		Task<bool> ExistsAsync ( Guid subscriberId, Guid subscribedToId );
		Task<PagedResult<Subscribe>> GetSubscriptionsAsync ( Guid subscriberId, int pageNumber, int pageSize );
		Task<int> GetSubscriptionsCountAsync ( Guid subscriberId );
		Task<PagedResult<Subscribe>> GetSubscribersAsync ( Guid subscribedToId, int pageNumber, int pageSize );
		Task<int> GetSubscribersCountAsync ( Guid subscribedToId );
	}
}
