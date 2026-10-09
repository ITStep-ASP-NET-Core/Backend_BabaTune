using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class RoomRepository : GenericRepository<Room, Guid>, IRoomRepository
	{
		public RoomRepository ( ApplicationContext context ) : base(context) { }

		public async Task<Room?> GetWithDetailsAsync ( Guid id )
		{
			return await _dbSet
				.Include(r => r.Owner)
				.Include(r => r.Members)
				.Include(r => r.CurrentSong)
					.ThenInclude(s => s!.User)
				.Include(r => r.Queue.OrderBy(q => q.AddedAt))
					.ThenInclude(q => q.Song)
					.ThenInclude(s => s!.User)
				.AsSplitQuery()
				.FirstOrDefaultAsync(r => r.Id == id);
		}

		public async Task<PagedResult<Room>> GetPagedAsync ( RoomType? type, int pageNumber, int pageSize )
		{
			return await ToCardsPageAsync(Filter(_dbSet.AsNoTracking(), type), pageNumber, pageSize);
		}

		public async Task<PagedResult<Room>> GetByOwnerAsync ( Guid ownerId, RoomType? type, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking().Where(r => r.OwnerId == ownerId);
			return await ToCardsPageAsync(Filter(query, type), pageNumber, pageSize);
		}

		public async Task<PagedResult<Room>> GetByCurrentSongAsync ( Guid songId, RoomType? type, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking().Where(r => r.CurrentSongId == songId);
			return await ToCardsPageAsync(Filter(query, type), pageNumber, pageSize);
		}

		public async Task AddSongAsync ( Guid roomId, Guid songId )
		{
			await _context.Set<QueueItem>().AddAsync(new QueueItem
			{
				Id = Guid.NewGuid(),
				RoomId = roomId,
				SongId = songId,
				AddedAt = DateTime.UtcNow
			});
		}

		public async Task AddSongsAsync ( Guid roomId, ICollection<Guid> songIds )
		{
			if (songIds is null || songIds.Count == 0)
				return;

			var start = DateTime.UtcNow;
			var offset = 0;

			var items = songIds.Select(songId => new QueueItem
			{
				Id = Guid.NewGuid(),
				RoomId = roomId,
				SongId = songId,
				AddedAt = start.AddTicks(offset++)
			}).ToList();

			await _context.Set<QueueItem>().AddRangeAsync(items);
		}

		public async Task<QueueItem?> DequeueAsync ( Guid roomId )
		{
			var set = _context.Set<QueueItem>();

			var item = await set
				.Where(q => q.RoomId == roomId)
				.OrderBy(q => q.AddedAt)
				.FirstOrDefaultAsync();

			if (item is null)
				return null;

			set.Remove(item);
			return item;
		}

		public async Task<bool> AddMemberAsync ( Guid roomId, Guid userId )
		{
			var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == userId);
			if (user is null)
				return false;

			user.RoomId = roomId;
			return true;
		}

		public async Task<bool> RemoveMemberAsync ( Guid roomId, Guid userId )
		{
			var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == userId && u.RoomId == roomId);
			if (user is null)
				return false;

			user.RoomId = null;
			return true;
		}

		public async Task<PagedResult<Room>> GetPublicRankedAsync ( int pageNumber, int pageSize )
		{
			var scored = _dbSet.AsNoTracking()
				.Where(r => r.Type == RoomType.Public)
				.Select(r => new
				{
					r.Id,
					r.UpdatedAt,
					Score = _context.Set<Subscribe>().Count(s => s.SubscribedToId == r.OwnerId)
						* (r.Owner!.IsChecked ? 1.5 : 1.0)
				});

			var total = await _dbSet.CountAsync(r => r.Type == RoomType.Public);

			var ids = await scored
				.OrderByDescending(x => x.Score)
				.ThenByDescending(x => x.UpdatedAt)
				.ThenBy(x => x.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.Select(x => x.Id)
				.ToListAsync();

			var rooms = await _dbSet.AsNoTracking()
				.Include(r => r.Owner)
				.Include(r => r.CurrentSong)
					.ThenInclude(s => s!.User)
				.Where(r => ids.Contains(r.Id))
				.ToListAsync();

			var byId = rooms.ToDictionary(r => r.Id);

			return new PagedResult<Room>
			{
				Items = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList(),
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = total
			};
		}

		public async Task<PagedResult<Room>> GetByFriendsAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(r => _context.Set<Friendship>().Any(f =>
					f.Status == FriendshipStatus.Accepted
					&& ((f.SenderId == userId && f.RecipientId == r.OwnerId)
						|| (f.RecipientId == userId && f.SenderId == r.OwnerId))));

			return await ToCardsPageAsync(query, pageNumber, pageSize);
		}

		public async Task<Guid?> GetRandomMemberIdAsync ( Guid roomId, Guid excludeUserId )
		{
			return await _context.Set<User>().AsNoTracking()
				.Where(u => u.RoomId == roomId && u.Id != excludeUserId)
				.OrderBy(u => Guid.NewGuid())
				.Select(u => (Guid?)u.Id)
				.FirstOrDefaultAsync();
		}

		private static IQueryable<Room> Filter ( IQueryable<Room> query, RoomType? type )
			=> type is null ? query : query.Where(r => r.Type == type.Value);

		private static async Task<PagedResult<Room>> ToCardsPageAsync ( IQueryable<Room> query, int pageNumber, int pageSize )
		{
			var ordered = query
				.Include(r => r.Owner)
				.Include(r => r.CurrentSong)
					.ThenInclude(s => s!.User)
				.OrderByDescending(r => r.UpdatedAt)
				.ThenBy(r => r.Id);

			return await ordered.ToPagedResultAsync(pageNumber, pageSize);
		}
	}
}
