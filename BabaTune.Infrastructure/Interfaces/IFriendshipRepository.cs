using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IFriendshipRepository : IGenericRepository<Friendship, Guid>
	{
		Task<Friendship?> GetByUsersAsync ( Guid userId, Guid otherUserId );
		Task<bool> ExistsAsync ( Guid userId, Guid otherUserId );
		Task<PagedResult<Friendship>> GetByStatusWithUsersAsync ( Guid userId, FriendshipStatus status, int pageNumber, int pageSize );
		Task<PagedResult<Friendship>> GetIncomingWithUsersAsync ( Guid recipientId, int pageNumber, int pageSize );
		Task<PagedResult<Friendship>> GetBlockedWithUsersAsync ( Guid blockerId, int pageNumber, int pageSize );
	}
}
