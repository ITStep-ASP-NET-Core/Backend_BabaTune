using BabaTune.Application.Common;
using BabaTune.Application.DTO.Notices;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Interfaces
{
	public interface INoticeService
	{
		Task<PagedResult<NoticeDto>> GetByRecipientAsync ( Guid recipientId, int pageNumber, int pageSize );
		Task<int> GetUnreadCountAsync ( Guid recipientId );
		Task<Result> MarkAsReadAsync ( Guid noticeId, Guid recipientId );
		Task CreateAsync ( NoticeType type, Guid? senderId, Guid recipientId, string title, string? text, string? url = null, string? imageUrl = null );
	}
}
