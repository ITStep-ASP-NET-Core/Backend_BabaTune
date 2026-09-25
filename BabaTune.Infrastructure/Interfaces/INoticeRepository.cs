using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface INoticeRepository : IGenericRepository<Notice, Guid>
	{
		Task<PagedResult<Notice>> GetByRecipientAsync ( Guid recipientId, int pageNumber, int pageSize );
		Task<int> GetUnreadCountAsync ( Guid recipientId );
		Task MarkAsReadAsync ( Guid noticeId );
	}
}
