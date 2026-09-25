using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class PlaylistRepository : GenericRepository<Playlist, Guid>, IPlaylistRepository
	{
		public PlaylistRepository ( ApplicationContext context ) : base(context) { }

		public async Task<Playlist?> GetWithItemsAsync ( Guid id )
		{
			return await _dbSet
				.Include(p => p.Items)
				.ThenInclude(i => i.Song)
				.ThenInclude(s => s.User)
				.FirstOrDefaultAsync(p => p.Id == id);
		}

		public async Task<PagedResult<Playlist>> GetByUserAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(p => p.UserId == userId)
				.OrderByDescending(p => p.CreatedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<Playlist>> GetCustomByUserAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(p => p.UserId == userId && p.Type == PlaylistType.Custom)
				.OrderByDescending(p => p.CreatedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<Playlist?> GetLikedPlaylistAsync ( Guid userId )
		{
			return await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId && p.Type == PlaylistType.Liked);
		}

		public async Task<PagedResult<PlaylistItem>> GetItemsPagedAsync ( Guid playlistId, int pageNumber, int pageSize )
		{
			var query = _context.PlaylistItems
				.AsNoTracking()
				.Where(i => i.PlaylistId == playlistId)
				.Include(i => i.Song)
				.ThenInclude(s => s.User)
				.OrderBy(i => i.Number);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task AddSongAsync ( Guid playlistId, Guid songId )
		{
			var exists = await _context.PlaylistItems
				.AnyAsync(i => i.PlaylistId == playlistId && i.SongId == songId);

			if (exists)
				return;

			var count = await _context.PlaylistItems
				.CountAsync(i => i.PlaylistId == playlistId);

			await _context.PlaylistItems.AddAsync(new PlaylistItem
			{
				Id = Guid.NewGuid(),
				PlaylistId = playlistId,
				SongId = songId,
				Number = count + 1,
				AddedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			});
		}

		public async Task RemoveSongAsync ( Guid playlistId, Guid songId )
		{
			var item = await _context.PlaylistItems
				.FirstOrDefaultAsync(i => i.PlaylistId == playlistId && i.SongId == songId);

			if (item is null)
				return;

			_context.PlaylistItems.Remove(item);

			var following = await _context.PlaylistItems
				.Where(i => i.PlaylistId == playlistId && i.Number > item.Number)
				.ToListAsync();

			foreach (var followingItem in following)
			{
				followingItem.Number--;
				followingItem.UpdatedAt = DateTime.UtcNow;
			}
		}

		public async Task<bool> ContainsSongAsync ( Guid playlistId, Guid songId )
		{
			return await _context.PlaylistItems
				.AnyAsync(i => i.PlaylistId == playlistId && i.SongId == songId);
		}

		public async Task<HashSet<Guid>> GetLikedSongIdsAsync ( Guid userId, ICollection<Guid> songIds )
		{
			if(songIds is null || songIds.Count == 0)
				return [];

			var likedPlaylist = await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId && p.Type == PlaylistType.Liked);
			if(likedPlaylist is null)
				return [];

			var matchedIds = await _context.PlaylistItems
				.Where(i => i.PlaylistId == likedPlaylist.Id && songIds.Contains(i.SongId))
				.Select(i => i.SongId)
				.ToListAsync();

			return matchedIds.ToHashSet();
		}

		public async Task<int> GetTotalDurationAsync ( Guid playlistId )
		{
			return await _context.PlaylistItems
				.Where(i => i.PlaylistId == playlistId)
				.Select(i => i.Song.Duration)
				.SumAsync();
		}

		public async Task<int> GetSongsCountAsync ( Guid playlistId )
		{
			return await _context.PlaylistItems
				.CountAsync(i => i.PlaylistId == playlistId);
		}

		public async Task MoveSongAsync ( Guid playlistId, Guid songId, int newPosition )
		{
			var items = await _context.PlaylistItems
				.Where(i => i.PlaylistId == playlistId)
				.OrderBy(i => i.Number)
				.ToListAsync();

			var target = items.FirstOrDefault(i => i.SongId == songId);
			if (target is null)
				return;

			var clampedPosition = Math.Clamp(newPosition, 1, items.Count);
			var oldPosition = target.Number;

			if (clampedPosition == oldPosition)
				return;

			if (clampedPosition > oldPosition)
			{
				foreach (var item in items.Where(i => i.Number > oldPosition && i.Number <= clampedPosition))
				{
					item.Number--;
					item.UpdatedAt = DateTime.UtcNow;
				}
			}
			else
			{
				foreach (var item in items.Where(i => i.Number >= clampedPosition && i.Number < oldPosition))
				{
					item.Number++;
					item.UpdatedAt = DateTime.UtcNow;
				}
			}

			target.Number = clampedPosition;
			target.UpdatedAt = DateTime.UtcNow;
		}
	}
}
