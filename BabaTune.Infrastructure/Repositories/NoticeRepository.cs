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

		public async Task<PagedResult<NoticeFeedRow>> GetFeedAsync ( Guid recipientId, bool unreadOnly, int pageNumber, int pageSize )
		{
			var others = _dbSet.AsNoTracking()
				.Where(n => n.RecipientId == recipientId && !(n is MessageNotice) && (!unreadOnly || !n.IsRead))
				.Select(n => new NoticeFeedRow { Id = n.Id, CreatedAt = n.CreatedAt, Count = 1 });

			var messages = _context.Set<MessageNotice>().AsNoTracking()
				.Where(n => n.RecipientId == recipientId && !n.IsRead)
				.GroupBy(n => n.ChatId)
				.Select(g => new NoticeFeedRow
				{
					Id = g.OrderByDescending(x => x.CreatedAt).Select(x => x.Id).First(),
					CreatedAt = g.Max(x => x.CreatedAt),
					Count = g.Count()
				});

			var query = others.Concat(messages)
				.OrderByDescending(r => r.CreatedAt)
				.ThenBy(r => r.Id);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<ICollection<Notice>> GetDetailedByIdsAsync ( ICollection<Guid> ids )
		{
			var result = new List<Notice>();

			result.AddRange(await _context.Set<FriendRequestNotice>().AsNoTracking()
				.Include(n => n.Sender).Where(n => ids.Contains(n.Id)).ToListAsync());

			result.AddRange(await _context.Set<MessageNotice>().AsNoTracking()
				.Where(n => ids.Contains(n.Id)).ToListAsync());

			result.AddRange(await _context.Set<NewSongNotice>().AsNoTracking()
				.Include(n => n.Author).Include(n => n.Song).Where(n => ids.Contains(n.Id)).ToListAsync());

			result.AddRange(await _context.Set<NewAlbumNotice>().AsNoTracking()
				.Include(n => n.Author).Include(n => n.Album).Where(n => ids.Contains(n.Id)).ToListAsync());

			result.AddRange(await _context.Set<NewRoomNotice>().AsNoTracking()
				.Include(n => n.Owner).Include(n => n.Room).Where(n => ids.Contains(n.Id)).ToListAsync());

			result.AddRange(await _context.Set<NewSubscriptionNotice>().AsNoTracking()
				.Include(n => n.Subscribe).ThenInclude(s => s.Subscriber)
				.Where(n => ids.Contains(n.Id)).ToListAsync());

			result.AddRange(await _context.Set<ServerNotice>().AsNoTracking()
				.Where(n => ids.Contains(n.Id)).ToListAsync());

			return result;
		}

		public async Task<Dictionary<Guid, string>> GetChatTitlesAsync ( Guid recipientId, ICollection<Guid> chatIds )
		{
			var friends = await _context.Set<Friendship>().AsNoTracking()
				.Where(f => f.ChatId != null && chatIds.Contains(f.ChatId.Value))
				.Select(f => new { f.ChatId, Name = f.SenderId == recipientId ? f.Recipient.Name : f.Sender.Name })
				.ToListAsync();

			var rooms = await _context.Set<Room>().AsNoTracking()
				.Where(r => chatIds.Contains(r.ChatId))
				.Select(r => new { r.ChatId, r.Owner.Name })
				.ToListAsync();

			var titles = new Dictionary<Guid, string>();
			foreach(var f in friends)
				titles[f.ChatId!.Value] = f.Name;
			foreach(var r in rooms)
				titles[r.ChatId] = r.Name;

			return titles;
		}

		public async Task<int> GetUnreadCountAsync ( Guid recipientId )
		{
			var others = await _dbSet.CountAsync(n => n.RecipientId == recipientId && !n.IsRead && !(n is MessageNotice));

			var chats = await _context.Set<MessageNotice>()
				.Where(n => n.RecipientId == recipientId && !n.IsRead)
				.Select(n => n.ChatId)
				.Distinct()
				.CountAsync();

			return others + chats;
		}

		public async Task MarkAsReadAsync ( Guid noticeId )
		{
			var notice = await _dbSet.FirstOrDefaultAsync(n => n.Id == noticeId);
			if(notice is null)
				return;

			notice.IsRead = true;
			notice.UpdatedAt = DateTime.UtcNow;
		}

		public async Task MarkChatAsReadAsync ( Guid recipientId, Guid chatId )
		{
			var items = await _context.Set<MessageNotice>()
				.Where(n => n.RecipientId == recipientId && n.ChatId == chatId && !n.IsRead)
				.ToListAsync();

			foreach(var item in items)
			{
				item.IsRead = true;
				item.UpdatedAt = DateTime.UtcNow;
			}
		}

		public async Task AddRangeAsync ( IEnumerable<Notice> notices )
		{
			await _dbSet.AddRangeAsync(notices);
		}

		public async Task<bool> IsImageUsedAsync ( string imageUrl, Guid exceptId )
		{
			return await _context.Set<ServerNotice>().AnyAsync(n => n.ImageUrl == imageUrl && n.Id != exceptId);
		}
	}
}