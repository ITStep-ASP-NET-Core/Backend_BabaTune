using BabaTune.Application.Common;
using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Common;

namespace BabaTune.Application.Interfaces
{
	public interface ISubscribeService
	{
		Task<PagedResult<UserSummaryDto>> GetSubscriptionsAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<UserSummaryDto>> GetSubscribersAsync ( Guid userId, int pageNumber, int pageSize );
		Task<Result> SubscribeAsync ( Guid subscriberId, Guid subscribedToId );
		Task<Result> UnsubscribeAsync ( Guid subscriberId, Guid subscribedToId );
	}
}
