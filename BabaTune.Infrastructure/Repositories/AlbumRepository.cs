using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class AlbumRepository : GenericRepository<Album, Guid>, IAlbumRepository
	{
		public AlbumRepository ( ApplicationContext context ) : base(context) { }

		public async Task<PagedResult<Album>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(a => a.UserId == userId)
				.Include(a => a.User)
				.OrderByDescending(a => a.CreatedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<Album>> SearchAsync ( string query, int pageNumber, int pageSize )
		{
			var q = _dbSet.AsNoTracking()
				.Where(a => a.Name.ToLower().Contains(query.ToLower()))
				.Include(a => a.User)
				.OrderByDescending(a => a.CreatedAt);

			return await q.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<int> GetSongsCountAsync ( Guid albumId )
		{
			return await _context.Songs.CountAsync(s => s.AlbumId == albumId);
		}

		public async Task<bool> ContainsSongAsync ( Guid albumId, Guid songId )
		{
			return await _context.Songs.AnyAsync(s => s.Id == songId && s.AlbumId == albumId);
		}

		public async Task<bool> AnySongInAlbumAsync ( ICollection<Guid> songIds )
		{
			if (songIds is null || songIds.Count == 0)
				return false;

			return await _context.Songs.AnyAsync(s => songIds.Contains(s.Id) && s.AlbumId != null);
		}

		public async Task<int> CountOwnedSongsAsync ( Guid userId, ICollection<Guid> songIds )
		{
			if (songIds is null || songIds.Count == 0)
				return 0;

			return await _context.Songs.CountAsync(s => s.UserId == userId && songIds.Contains(s.Id));
		}

		public async Task AddSongsAsync ( Guid albumId, ICollection<Guid> songIds )
		{
			if (songIds is null || songIds.Count == 0)
				return;

			var songs = await _context.Songs.Where(s => songIds.Contains(s.Id)).ToListAsync();
			var now = DateTime.UtcNow;

			foreach (var song in songs)
			{
				song.AlbumId = albumId;
				song.UpdatedAt = now;
			}
		}

		public async Task RemoveSongAsync ( Guid albumId, Guid songId )
		{
			var song = await _context.Songs.FirstOrDefaultAsync(s => s.Id == songId && s.AlbumId == albumId);
			if (song is null)
				return;

			song.AlbumId = null;
			song.UpdatedAt = DateTime.UtcNow;
		}
	}
}
