using BabaTune.Application.Common;
using BabaTune.Application.DTO.Notices;
using BabaTune.Domain.Common;

namespace BabaTune.Application.Interfaces
{
	public interface INoticeService
	{
		Task<PagedResult<NoticeDto>> GetAllAsync ( Guid recipientId, int pageNumber, int pageSize );
		Task<PagedResult<NoticeDto>> GetUnreadAsync ( Guid recipientId, int pageNumber, int pageSize );
		Task<int> GetUnreadCountAsync ( Guid recipientId );
		Task<Result> MarkAsReadAsync ( Guid noticeId, Guid recipientId );
		Task MarkChatAsReadAsync ( Guid chatId, Guid recipientId );
		Task NotifyFriendRequestAsync ( Guid senderId, Guid recipientId, Guid friendshipId );
		Task NotifyMessageAsync ( Guid chatId, IEnumerable<Guid> recipientIds );
		Task NotifyNewSongAsync ( Guid authorId, Guid songId );
		Task NotifyNewAlbumAsync ( Guid authorId, Guid albumId );
		Task NotifyNewRoomAsync ( Guid ownerId, Guid roomId );
		Task NotifyNewSubscriptionAsync ( Guid recipientId, Guid subscribeId );
		Task<Result> CreateServerAsync ( CreateServerNoticeDto dto );
		Task<Result> UpdateServerAsync ( Guid id, UpdateServerNoticeDto dto );
		Task<Result> DeleteServerAsync ( Guid id );
	}
}