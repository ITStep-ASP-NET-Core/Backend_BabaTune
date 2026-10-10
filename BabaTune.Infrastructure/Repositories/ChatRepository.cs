using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class ChatRepository : GenericRepository<Chat, Guid>, IChatRepository
	{
		public ChatRepository ( ApplicationContext context ) : base(context) { }

		public async Task<PagedResult<Message>> GetMessagesAsync ( Guid chatId, int pageNumber, int pageSize )
		{
			var query = _context.Set<Message>().AsNoTracking()
				.Where(m => m.ChatId == chatId)
				.OrderByDescending(m => m.CreatedAt)
				.ThenByDescending(m => m.Id);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<Message>> GetMessagesWithUsersAsync ( Guid chatId, int pageNumber, int pageSize )
		{
			var query = _context.Set<Message>().AsNoTracking()
				.Where(m => m.ChatId == chatId)
				.Include(m => m.User)
				.OrderByDescending(m => m.CreatedAt)
				.ThenByDescending(m => m.Id);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task AddMessageAsync ( Message message )
		{
			await _context.Set<Message>().AddAsync(message);
		}

		public async Task<bool> IsParticipantAsync ( Guid chatId, Guid userId )
		{
			var inFriendship = await _context.Set<Friendship>().AnyAsync(f =>
				f.ChatId == chatId
				&& f.Status == FriendshipStatus.Accepted
				&& (f.SenderId == userId || f.RecipientId == userId));

			if (inFriendship)
				return true;

			return await _context.Set<Room>().AnyAsync(r =>
				r.ChatId == chatId && r.Members.Any(m => m.Id == userId));
		}

		public async Task<Guid?> GetPeerIdAsync ( Guid chatId, Guid userId )
		{
			var pair = await _context.Set<Friendship>().AsNoTracking()
				.Where(f => f.ChatId == chatId
					&& f.Status == FriendshipStatus.Accepted
					&& (f.SenderId == userId || f.RecipientId == userId))
				.Select(f => new { f.SenderId, f.RecipientId })
				.FirstOrDefaultAsync();

			if (pair is null)
				return null;

			return pair.SenderId == userId ? pair.RecipientId : pair.SenderId;
		}
		public async Task<HashSet<Guid>> GetParticipantIdsAsync ( Guid chatId )
		{
			var ids = new HashSet<Guid>();

			var friendship = await _context.Set<Friendship>().AsNoTracking()
				.Where(f => f.ChatId == chatId)
				.Select(f => new { f.SenderId, f.RecipientId })
				.FirstOrDefaultAsync();

			if(friendship is not null)
			{
				ids.Add(friendship.SenderId);
				ids.Add(friendship.RecipientId);
			}

			var room = await _context.Set<Room>().AsNoTracking()
				.Where(r => r.ChatId == chatId)
				.Select(r => new { r.OwnerId, r.Members })
				.FirstOrDefaultAsync();

			if(room is not null)
			{
				ids.Add(room.OwnerId);
				ids.UnionWith(room.Members.Select(m => m.Id));
			}

			return ids;
		}
	}
}
