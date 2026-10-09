using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IRoomRepository : IGenericRepository<Room, Guid>
	{
		Task<Room?> GetWithDetailsAsync ( Guid id );
		Task<PagedResult<Room>> GetPagedAsync ( RoomType? type, int pageNumber, int pageSize );
		Task<PagedResult<Room>> GetByOwnerAsync ( Guid ownerId, RoomType? type, int pageNumber, int pageSize );
		Task<PagedResult<Room>> GetByCurrentSongAsync ( Guid songId, RoomType? type, int pageNumber, int pageSize );
		Task AddSongAsync ( Guid roomId, Guid songId );
		Task AddSongsAsync ( Guid roomId, ICollection<Guid> songIds );
		Task<QueueItem?> DequeueAsync ( Guid roomId );
		Task<bool> AddMemberAsync ( Guid roomId, Guid userId );
		Task<bool> RemoveMemberAsync ( Guid roomId, Guid userId );
		Task<PagedResult<Room>> GetPublicRankedAsync ( int pageNumber, int pageSize );
		Task<PagedResult<Room>> GetByFriendsAsync ( Guid userId, int pageNumber, int pageSize );
		Task<Guid?> GetRandomMemberIdAsync ( Guid roomId, Guid excludeUserId );
	}
}
