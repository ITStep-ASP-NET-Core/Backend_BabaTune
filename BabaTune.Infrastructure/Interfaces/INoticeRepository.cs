using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface INoticeRepository : IGenericRepository<Notice, Guid>
	{
		Task<int> GetUnreadCountAsync ( Guid recipientId );
		Task MarkAsReadAsync ( Guid noticeId );
		Task<PagedResult<NoticeFeedRow>> GetFeedAsync ( Guid recipientId, bool unreadOnly, int pageNumber, int pageSize );
		Task<ICollection<Notice>> GetDetailedByIdsAsync ( ICollection<Guid> ids );
		Task<Dictionary<Guid, string>> GetChatTitlesAsync ( Guid recipientId, ICollection<Guid> chatIds );
		Task MarkChatAsReadAsync ( Guid recipientId, Guid chatId );
		Task AddRangeAsync ( IEnumerable<Notice> notices );
		Task<bool> IsImageUsedAsync ( string imageUrl, Guid exceptId );
	}
}
