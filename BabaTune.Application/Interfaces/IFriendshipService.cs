using BabaTune.Application.Common;
using BabaTune.Application.DTO.Friendships;
using BabaTune.Domain.Common;

namespace BabaTune.Application.Interfaces
{
	public interface IFriendshipService
	{
		Task<PagedResult<FriendshipDto>> GetFriendsAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<FriendshipDto>> GetIncomingAsync ( Guid userId, int pageNumber, int pageSize );
		Task<PagedResult<FriendshipDto>> GetBlockedAsync ( Guid userId, int pageNumber, int pageSize );
		Task<Result> SendRequestAsync ( Guid senderId, Guid recipientId );
		Task<Result> AcceptAsync ( Guid friendshipId, Guid userId );
		Task<Result> BlockAsync ( Guid userId, Guid targetId );
		Task<Result> RemoveAsync ( Guid friendshipId, Guid userId );
	}
}
