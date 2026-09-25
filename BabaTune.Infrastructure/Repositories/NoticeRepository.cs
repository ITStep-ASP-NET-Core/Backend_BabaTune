using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class NoticeRepository : GenericRepository<Notice, Guid>, INoticeRepository
	{
		public NoticeRepository ( ApplicationContext context ) : base(context) { }

		public async Task<PagedResult<Notice>> GetByRecipientAsync ( Guid recipientId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(n => n.RecipientId == recipientId)
				.OrderByDescending(n => n.CreatedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<int> GetUnreadCountAsync ( Guid recipientId )
		{
			return await _dbSet.CountAsync(n => n.RecipientId == recipientId && !n.IsRead);
		}

		public async Task MarkAsReadAsync ( Guid noticeId )
		{
			var notice = await _dbSet.FirstOrDefaultAsync(n => n.Id == noticeId);
			if (notice is null)
				return;

			notice.IsRead = true;
		}
	}
}
